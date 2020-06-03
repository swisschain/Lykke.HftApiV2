using System;
using System.Collections.Generic;

namespace HftApi.Worker.RabbitSubscribers.Messages
{
    public class OrderbookMessage
    {
        public string AssetPair { get; set; }
        public bool IsBuy { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VolumePriceItem> Prices { get; set; } = new List<VolumePriceItem>();
    }

    public class VolumePriceItem
    {
        public string Id { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
    }
}
