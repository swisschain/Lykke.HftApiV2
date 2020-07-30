using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.Worker.RabbitSubscribers.Messages;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.HftApi.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using MyNoSqlServer.Abstractions;

namespace HftApi.Worker.RabbitSubscribers
{
    [UsedImplicitly]
    public class BalancesSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly IMyNoSqlServerDataWriter<BalanceEntity> _writer;
        private readonly BalanceHttpClient _balanceClient;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<BalanceMessage> _subscriber;
        private readonly HashSet<string> _walletIds = new HashSet<string>();

        public BalancesSubscriber(
            string connectionString,
            string exchangeName,
            IMyNoSqlServerDataWriter<BalanceEntity> writer,
            BalanceHttpClient balanceClient,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _writer = writer;
            _balanceClient = balanceClient;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"hft-{nameof(BalancesSubscriber)}")
                .MakeDurable();

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

            var walletIds = message.Balances.Select(x => x.Id).Distinct().ToList();

            await InitBalancesIfNeededAsync(walletIds);

            var entities = message.Balances.Select(balance => new BalanceEntity(balance.Id, balance.Asset)
                {
                    CreatedAt = message.Timestamp,
                    Balance = balance.NewBalance,
                    Reserved = balance.NewReserved ?? 0
                })
                .ToList();

            await _writer.BulkInsertOrReplaceAsync(entities);
        }

        private async Task InitBalancesIfNeededAsync(List<string> walletIds)
        {
            foreach (var walletId in walletIds.Where(x => !_walletIds.Contains(x)))
            {
                var balances = await _balanceClient.GetBalanceAsync(walletId);

                var entities = balances.Select(balance => new BalanceEntity(walletId, balance.AssetId)
                    {
                        CreatedAt = balance.Timestamp,
                        Balance = balance.Available,
                        Reserved = balance.Reserved
                    })
                    .ToList();

                await _writer.BulkInsertOrReplaceAsync(entities);

                _walletIds.Add(walletId);
            }
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
