using System;
using System.Collections.Generic;

namespace Lykke.HftApi.Domain.Entities
{
    public class Orderbook
    {
        public string AssetPairId { get; set; }
        public DateTime Timestamp { get; set; }
        public IReadOnlyCollection<VolumePrice> Bids { get; set; }
        public IReadOnlyCollection<VolumePrice> Asks { get; set; }
    }
}
