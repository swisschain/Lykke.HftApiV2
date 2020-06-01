using System;
using System.Collections.Generic;
using MyNoSqlServer.Abstractions;

namespace Lykke.HftApi.Domain.Entities
{
    public class OrderbookEntity : IMyNoSqlEntity
    {

        public OrderbookEntity()
        {
        }

        public OrderbookEntity(string assetPairId)
        {
            PartitionKey = GetPk();
            RowKey = assetPairId;
            TimeStamp = DateTime.UtcNow;
            AssetPairId = assetPairId;
        }
        public string AssetPairId { get; set; }
        public List<VolumePrice> Bids { get; set; } = new List<VolumePrice>();
        public List<VolumePrice> Asks { get; set; } = new List<VolumePrice>();
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
        public static string GetPk() => "Orderbook";
    }
}
