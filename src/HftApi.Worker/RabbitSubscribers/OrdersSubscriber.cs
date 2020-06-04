using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MyNoSqlServer.Abstractions;

namespace HftApi.Worker.RabbitSubscribers
{
    [UsedImplicitly]
    public class OrdersSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IMyNoSqlServerDataWriter<OrderEntity> _orderWriter;
        private readonly IMyNoSqlServerDataWriter<TradeEntity> _tradeWriter;
        private readonly IMapper _mapper;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<ExecutionEvent> _subscriber;

        public OrdersSubscriber(
            string connectionString,
            string exchangeName,
            IMyNoSqlServerDataWriter<OrderEntity> orderWriter,
            IMyNoSqlServerDataWriter<TradeEntity> tradeWriter,
            IMapper mapper,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _orderWriter = orderWriter;
            _tradeWriter = tradeWriter;
            _mapper = mapper;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"hft-{nameof(OrdersSubscriber)}-{Environment.MachineName}")
                .UseRoutingKey(((int) Lykke.MatchingEngine.Connector.Models.Events.Common.MessageType.Order).ToString());

            settings.DeadLetterExchangeName = null;

            _subscriber = new RabbitMqSubscriber<ExecutionEvent>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings, TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<ExecutionEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(ExecutionEvent message)
        {
            var orders = _mapper.Map<List<OrderEntity>>(message.Orders);

            var trades = new List<TradeEntity>();

            foreach (var order in orders)
            {
                foreach (var trade in order.Trades)
                {
                    trade.AssetPairId = order.AssetPairId;
                    trade.OrderId = order.Id;
                    trade.WalletId = order.WalletId;

                    var tradeEntity = _mapper.Map<TradeEntity>(trade);
                    tradeEntity.AssetPairId = order.AssetPairId;
                    tradeEntity.OrderId = order.Id;
                    tradeEntity.WalletId = order.WalletId;

                    trades.Add(tradeEntity);
                }
            }

            await _orderWriter.BulkInsertOrReplaceAsync(orders);
            await _tradeWriter.BulkInsertOrReplaceAsync(trades);
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
