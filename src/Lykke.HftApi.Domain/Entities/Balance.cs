using System;

namespace Lykke.HftApi.Domain.Entities
{
    public class Balance
    {
        public string AssetId { get; set; }
        public decimal Available { get; set; }
        public decimal Reserved { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
