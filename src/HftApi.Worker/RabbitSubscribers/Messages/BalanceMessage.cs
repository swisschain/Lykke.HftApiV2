using System;
using System.Collections.Generic;

namespace HftApi.Worker.RabbitSubscribers.Messages
{
    public class BalanceMessage
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ClientBalanceMessage> Balances { get; set; }
    }

    public class ClientBalanceMessage
    {
        public string Id { get; set; }
        public string Asset { get; set; }
        public decimal OldBalance { get; set; }
        public decimal NewBalance { get; set; }
        public decimal? OldReserved { get; set; }
        public decimal? NewReserved { get; set; }
    }
}
