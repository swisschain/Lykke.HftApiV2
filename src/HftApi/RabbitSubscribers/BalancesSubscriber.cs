using System;
using System.Linq;
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
    public class BalancesSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IMyNoSqlServerDataWriter<BalanceEntity> _writer;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<BalanceMessage> _subscriber;

        public BalancesSubscriber(
            string connectionString,
            string exchangeName,
            IMyNoSqlServerDataWriter<BalanceEntity> writer,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _writer = writer;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"{nameof(BalancesSubscriber)}-{Environment.MachineName}");

            settings.DeadLetterExchangeName = null;

            _subscriber = new RabbitMqSubscriber<BalanceMessage>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings, TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<BalanceMessage>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(BalanceMessage message)
        {
            if (!message.Balances.Any())
                return;

            foreach (var balance in message.Balances)
            {
                var entity = new BalanceEntity(balance.Id, balance.Asset)
                {
                    TimeStamp = message.Timestamp,
                    Balance = balance.NewBalance,
                    Reserved = balance.NewReserved ?? 0
                };

                await _writer.InsertOrReplaceAsync(entity);
            }
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
