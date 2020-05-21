using System;
using System.Threading.Tasks;
using Autofac;
using Lykke.Common.Log;
using Lykke.HftApi.Domain.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.HftInternalService.Client.Messages;
using Microsoft.Extensions.Logging;

namespace HftApi.RabbitSubscribers
{
    public class KeyUpdateSubscriber : IStartable, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly ITokenService _tokenService;
        private readonly ILogFactory _logFactory;
        private RabbitMqSubscriber<KeyUpdatedEvent> _subscriber;

        public KeyUpdateSubscriber(
            string connectionString,
            string exchangeName,
            ITokenService tokenService,
            ILoggerFactory loggerFactory,
            ILogFactory logFactory)
        {
            _connectionString = connectionString;
            _exchangeName = exchangeName;
            _tokenService = tokenService;
            _logFactory = logFactory;
        }

        public void Start()
        {
            _tokenService.InitAsync().GetAwaiter().GetResult();

            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, $"{nameof(KeyUpdateSubscriber)}-{Environment.MachineName}");

            settings.DeadLetterExchangeName = null;

            _subscriber = new RabbitMqSubscriber<KeyUpdatedEvent>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings, TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<KeyUpdatedEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private Task ProcessMessageAsync(KeyUpdatedEvent message)
        {
            if (message.IsDeleted)
                _tokenService.Remove(message.Id);
            else
                _tokenService.Add(message.Id);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
