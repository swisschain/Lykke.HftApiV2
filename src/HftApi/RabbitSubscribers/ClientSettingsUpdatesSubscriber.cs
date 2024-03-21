using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using HftApi.RabbitSubscribers.Messages;
using Lykke.Common.Log;
using Lykke.HftApi.Domain.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.HftInternalService.Client;

namespace HftApi.RabbitSubscribers
{
    public class ClientSettingsUpdatesSubscriber : IStartable, IDisposable
    {
        private readonly RabbitMqSubscriber<ClientSettingsCashoutBlockUpdated> _subscriber;
        private readonly IHftInternalClient _hftInternalClient;
        private readonly ITokenService _tokenService;
        private readonly ILog _log;

        public ClientSettingsUpdatesSubscriber(ILogFactory logFactory,
            string connectionString,
            IHftInternalClient hftInternalClient,
            ITokenService tokenService)
        {
            _hftInternalClient = hftInternalClient;
            _tokenService = tokenService;
            _log = logFactory.CreateLog(this);

            var subscriptionSettings = RabbitMqSubscriptionSettings
                .ForSubscriber(
                    connectionString,
                    "client-account.client-settings-updated",
                    $"client-account.client-settings-updated.hft-api-v2-{Environment.MachineName}")
                .UseRoutingKey(nameof(ClientSettingsCashoutBlockUpdated));

            subscriptionSettings.DeadLetterExchangeName = null;

            var strategy = new DefaultErrorHandlingStrategy(logFactory, subscriptionSettings);

            _subscriber = new RabbitMqSubscriber<ClientSettingsCashoutBlockUpdated>(logFactory, subscriptionSettings, strategy)
                .SetMessageDeserializer(new JsonMessageDeserializer<ClientSettingsCashoutBlockUpdated>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .Subscribe(HandleMessage);
        }

        public async Task HandleMessage(ClientSettingsCashoutBlockUpdated evt)
        {
            // Race condition with KeyUpdatedEvent event handling is possible, but it is decided
            // that it's acceptable since these events are not very frequent

            _log.Info($"Got client trades blocking update. Trades are blocked: {evt.TradesBlocked}", context: new
            {
                ClientId = evt.ClientId
            });           

            var enabledClientApiKeys = await _hftInternalClient.Keys.GetKeys(evt.ClientId);

            if (evt.TradesBlocked)
            {
                foreach (var key in enabledClientApiKeys)
                {
                    _tokenService.Remove(key.ApiKey);

                    var apiKeyStart = key.ApiKey.Substring(0, 4);

                    _log.Info($"API key has been cached", context: new
                    {
                        ClientId = evt.ClientId,
                        ApiKeyStart = apiKeyStart
                    });
                }
            }
            else
            {
                foreach (var key in enabledClientApiKeys)
                {
                    _tokenService.Add(key.ApiKey);

                    var apiKeyStart = key.ApiKey.Substring(0, 4);

                    _log.Info($"API key has been evicted from the cache", context: new
                    {
                        ClientId = evt.ClientId,
                        ApiKeyStart = apiKeyStart
                    });
                }                
            }
        }

        public void Start()
        {
            _subscriber.Start();
        }

        public void Dispose()
        {
            _subscriber?.Stop();
            _subscriber?.Dispose();
        }
    }
}
