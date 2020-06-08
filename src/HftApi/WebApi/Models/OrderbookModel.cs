using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HftApi.WebApi.Models
{
    public class OrderbookModel
    {
        public string AssetPairId { get; set; }
        public DateTime Timestamp { get; set; }
        public IReadOnlyCollection<VolumePriceModel> Bids { get; set; }
        public IReadOnlyCollection<VolumePriceModel> Asks { get; set; }
    }

    public class VolumePriceModel
    {
        [JsonProperty("v")]
        public decimal Volume { get; set; }
        [JsonProperty("p")]
        public decimal Price { get; set; }

        public VolumePriceModel(decimal volume, decimal price)
        {
            Volume = volume;
            Price = price;
        }
    }
}
