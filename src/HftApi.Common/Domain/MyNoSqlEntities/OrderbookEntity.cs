using System;
using System.Collections.Generic;
using MyNoSqlServer.Abstractions;

namespace HftApi.Common.Domain.MyNoSqlEntities
{
    public class OrderbookEntity : IMyNoSqlDbEntity
    {
        public OrderbookEntity()
        {
        }

        public OrderbookEntity(string assetPairId)
        {
            PartitionKey = GetPk();
            RowKey = assetPairId;
            CreatedAt = DateTime.UtcNow;
            AssetPairId = assetPairId;
        }
        public string AssetPairId { get; set; }
        public List<VolumePriceEntity> Bids { get; set; } = new List<VolumePriceEntity>();
        public List<VolumePriceEntity> Asks { get; set; } = new List<VolumePriceEntity>();
        public DateTime CreatedAt { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
        public static string GetPk() => "Orderbook";
    }

    public class VolumePriceEntity
    {
        public decimal Volume { get; set; }
        public decimal Price { get; set; }

        public VolumePriceEntity(decimal volume, decimal price)
        {
            Volume = volume;
            Price = price;
        }
    }
}
