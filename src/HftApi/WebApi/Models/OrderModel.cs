using System;
using System.Collections.Generic;

namespace HftApi.WebApi.Models
{
    public class OrderModel
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? LastTradeTimestamp { get; set; }
        public string Status { get; set; }
        public string AssetPairId { get; set; }
        public string Type { get; set; }
        public string Side { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal FilledVolume => Volume - RemainingVolume;
        public decimal RemainingVolume { get; set; }
        public decimal Cost => FilledVolume * Price;
        public IReadOnlyCollection<TradeModel> Trades { get; set; } = Array.Empty<TradeModel>();
    }
}
