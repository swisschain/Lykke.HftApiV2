using System;
using MyNoSqlServer.Abstractions;

namespace HftApi.Common.Domain.MyNoSqlEntities
{
    public class OrderEntity : IMyNoSqlDbEntity
    {
        public string Id { get; set; }
        public string WalletId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTradeTimestamp { get; set; }
        public string Status { get; set; }
        public string AssetPairId { get; set; }
        public string Type { get; set; }
        public string Side { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public decimal FilledVolume => Volume - RemainingVolume;
        public decimal RemainingVolume { get; set; }
        public decimal Cost => Math.Abs(FilledVolume * Price);

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
    }
}
