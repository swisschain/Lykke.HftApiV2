using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Common.Log;
using Lykke.HftApi.Domain.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.HftInternalService.Client.Messages;

namespace HftApi.RabbitSubscribers
{
    public class KeyUpdateSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly ITokenService _tokenService;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private readonly IBlockedClientsService _blockedClients;
        private RabbitMqSubscriber<KeyUpdatedEvent> _subscriber;

        public KeyUpdateSubscriber(
            string connectionString,
            string exchangeName,
            ITokenService tokenService,
            ILogFactory logFactory,
            IBlockedClientsService blockedClients)
        {
            _log = logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _tokenService = tokenService;
            _logFactory = logFactory;
            _blockedClients = blockedClients;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"{nameof(KeyUpdateSubscriber)}-{Environment.MachineName}");

            settings.DeadLetterExchangeName = null;

            _subscriber = new RabbitMqSubscriber<KeyUpdatedEvent>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_log, settings, TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<KeyUpdatedEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();

            _tokenService.InitAsync().GetAwaiter().GetResult();
        }

        private async Task ProcessMessageAsync(KeyUpdatedEvent message)
        {
            // Race condition with ClientSettingsCashoutBlockUpdated event handling is possible, but it is decided
            // that it's acceptable since these events are not very frequent

            var isClientBlocked = await _blockedClients.IsClientBlocked(message.ClientId);

            _log.Info($"API key deleted: {message.IsDeleted}. Client blocked: {isClientBlocked}", context: new
            {
                ClientId = message.ClientId,
                WalletId = message.WalletId,
                ApiKeyId = message.Id
            });

            if (!message.IsDeleted && !isClientBlocked)
            {
                _tokenService.Add(message.Id);

                _log.Info($"API key has been cached", context: new
                {
                    ClientId = message.ClientId,
                    WalletId = message.WalletId,
                    ApiKeyId = message.Id
                });
            }
            else
            {
                _tokenService.Remove(message.Id);

                _log.Info($"API key has been evicted from the cache", context: new
                {
                    ClientId = message.ClientId,
                    WalletId = message.WalletId,
                    ApiKeyId = message.Id
                });
            }
        }

        public void Dispose()
        {
            _subscriber?.Stop();
            _subscriber?.Dispose();
        }
    }
}
