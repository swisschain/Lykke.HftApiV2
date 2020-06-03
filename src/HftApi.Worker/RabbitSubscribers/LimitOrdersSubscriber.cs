using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using Common;
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
    public class LimitOrdersSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IMyNoSqlServerDataWriter<LimitOrderEntity> _writer;
        private readonly IMapper _mapper;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<ExecutionEvent> _subscriber;

        public LimitOrdersSubscriber(
            string connectionString,
            string exchangeName,
            IMyNoSqlServerDataWriter<LimitOrderEntity> writer,
            IMapper mapper,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _writer = writer;
            _mapper = mapper;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"hft-{nameof(LimitOrdersSubscriber)}")
                .MakeDurable()
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
            var orders = _mapper.Map<List<LimitOrderEntity>>(message.Orders);

            foreach (var order in orders)
            {
                foreach (var trade in order.Trades)
                {
                    trade.AssetPairId = order.AssetPairId;
                    trade.OrderId = order.Id;
                }
            }

            await _writer.BulkInsertOrReplaceAsync(orders);
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
