using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Services;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace HftApi.RabbitSubscribers
{
    [UsedImplicitly]
    public class TradesSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly TradesStreamService _tradeStream;
        private readonly IMapper _mapper;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<ExecutionEvent> _subscriber;

        public TradesSubscriber(
            string connectionString,
            string exchangeName,
            TradesStreamService tradeStream,
            IMapper mapper,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _tradeStream = tradeStream;
            _mapper = mapper;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"hft-{nameof(TradesSubscriber)}-{Environment.MachineName}")
                .UseRoutingKey(((int) Lykke.MatchingEngine.Connector.Models.Events.Common.MessageType.Order).ToString())
                .MakeDurable();

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
            var trades = new List<TradeEntity>();

            foreach (var order in message.Orders)
            {
                var orderEntity = _mapper.Map<OrderEntity>(order);

                if (order.Trades == null)
                    continue;

                foreach (var trade in order.Trades)
                {
                    var tradeEntity = _mapper.Map<TradeEntity>(trade);
                    tradeEntity.AssetPairId = orderEntity.AssetPairId;
                    tradeEntity.OrderId = orderEntity.Id;
                    tradeEntity.PartitionKey = orderEntity.WalletId;
                    tradeEntity.WalletId = orderEntity.WalletId;
                    trades.Add(tradeEntity);
                }
            }

            var tradesByWallet = trades.GroupBy(x => x.WalletId);

            foreach (var walletTrades in tradesByWallet)
            {
                var tradeUpdate = new TradeUpdate();

                tradeUpdate.Trades.AddRange(_mapper.Map<List<Lykke.HftApi.ApiContract.Trade>>(walletTrades.ToList()));
                _tradeStream.WriteToStream(tradeUpdate, walletTrades.Key);
            }
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
