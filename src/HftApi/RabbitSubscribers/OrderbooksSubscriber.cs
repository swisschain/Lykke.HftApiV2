using System;
using System.Threading.Tasks;
using Autofac;
using HftApi.RabbitSubscribers.Messages;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.HftApi.Domain.Entities;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MyNoSqlServer.Abstractions;

namespace HftApi.RabbitSubscribers
{
    [UsedImplicitly]
    public class OrderbooksSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IMyNoSqlServerDataWriter<OrderbookEntity> _orderbookWriter;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<OrderbookMessage> _subscriber;

        public OrderbooksSubscriber(
            string connectionString,
            string exchangeName,
            IMyNoSqlServerDataWriter<OrderbookEntity> orderbookWriter,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _orderbookWriter = orderbookWriter;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"{nameof(OrderbooksSubscriber)}-{Environment.MachineName}");

            settings.DeadLetterExchangeName = null;

            _subscriber = new RabbitMqSubscriber<OrderbookMessage>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings, TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<OrderbookMessage>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(OrderbookMessage orderbookMessage)
        {
            var entity = await _orderbookWriter.GetAsync(OrderbookEntity.GetPk(), orderbookMessage.AssetPair)
                          ?? new OrderbookEntity(orderbookMessage.AssetPair)
                          {
                              TimeStamp = orderbookMessage.Timestamp,
                          };

            var prices = orderbookMessage.IsBuy ? entity.Bids : entity.Asks;
            prices.Clear();

            foreach (var price in orderbookMessage.Prices)
            {
                prices.Add(new VolumePrice((decimal)price.Volume, (decimal)price.Price));
            }

            await _orderbookWriter.InsertOrReplaceAsync(entity);
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
