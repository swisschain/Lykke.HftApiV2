using Lykke.SettingsReader.Attributes;

namespace HftApi.Common.Configuration
{
    public class SagasRabbitMq
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }
    }
}
