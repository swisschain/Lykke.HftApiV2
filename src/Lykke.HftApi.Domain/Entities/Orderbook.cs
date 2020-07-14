using System;
using System.Collections.Generic;

namespace Lykke.HftApi.Domain.Entities
{
    public class Orderbook
    {
        public string AssetPairId { get; set; }
        public DateTime Timestamp { get; set; }
        public IReadOnlyList<VolumePrice> Bids { get; set; }
        public IReadOnlyList<VolumePrice> Asks { get; set; }
    }
}
