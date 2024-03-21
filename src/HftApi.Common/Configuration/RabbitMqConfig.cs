﻿namespace HftApi.Common.Configuration
{
    public class RabbitMqConfig
    {
        public RabbitMqConnection HftInternal { get; set; }
        public RabbitMqConnection Orderbooks { get; set; }
        public RabbitMqConnection Balances { get; set; }
        public RabbitMqConnection Orders { get; set; }
        public RabbitMqConnection PublicTrades { get; set; }
        public string ClientAccountFeedConnectionString { get; set; }
    }
}
