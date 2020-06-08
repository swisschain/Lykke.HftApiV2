using System;

namespace HftApi.WebApi.Models
{
    public class TickerModel
    {
        public string AssetPairId { get; set; }
        public decimal VolumeBase { get; set; }
        public decimal VolumeQuote { get; set; }
        public decimal PriceChange { get; set; }
        public decimal LastPrice { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
