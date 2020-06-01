namespace HftApi.Common.Configuration
{
    public class RabbitMqConfig
    {
        public string ConnectionString { get; set; }
        public string MeConnectionString { get; set; }
        public string ExchangeName { get; set; }
        public string OrderbooksExchangeName { get; set; }
        public string BalancesExchangeName { get; set; }
    }
}
