using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.Worker.RabbitSubscribers.Messages;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MyNoSqlServer.Abstractions;

namespace HftApi.Worker.RabbitSubscribers
{
    [UsedImplicitly]
    public class OrderbooksSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IMyNoSqlServerDataWriter<OrderbookEntity> _orderbookWriter;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<OrderbookMessage> _subscriber;
        private readonly ILog _log;

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
            _log = logFactory.CreateLog(this);
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"hft-{nameof(OrderbooksSubscriber)}");

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
            OrderbookEntity orderbook = null;
            try
            {
                orderbook = await _orderbookWriter.GetAsync(OrderbookEntity.GetPk(), orderbookMessage.AssetPair);
            }
            catch (Exception ex)
            {
                _log.Warning($"Can't get orderbook from mynosql for assetPair = {orderbookMessage.AssetPair}", ex);
            }

            var entity = orderbook ?? new OrderbookEntity(orderbookMessage.AssetPair)
              {
                  CreatedAt = orderbookMessage.Timestamp
              };

            entity.CreatedAt = orderbookMessage.Timestamp;
            var prices = orderbookMessage.IsBuy ? entity.Bids : entity.Asks;
            prices.Clear();

            foreach (var price in orderbookMessage.Prices)
            {
                prices.Add(new VolumePriceEntity((decimal)price.Volume, (decimal)price.Price));
            }

            await _orderbookWriter.InsertOrReplaceAsync(entity);
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
