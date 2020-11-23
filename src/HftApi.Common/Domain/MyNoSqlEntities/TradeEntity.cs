using System;
using Lykke.HftApi.Domain.Entities;
using MyNoSqlServer.Abstractions;

namespace HftApi.Common.Domain.MyNoSqlEntities
{
    public class TradeEntity : IMyNoSqlDbEntity
    {
        public string Id { get; set; }
        public string WalletId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AssetPairId { get; set; }
        public string OrderId { get; set; }
        public string OppositeOrderId { get; set; }
        public string Role { get; set; }
        public decimal Price { get; set; }
        public decimal BaseVolume { get; set; }
        public decimal QuoteVolume { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public TradeFee Fee { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
    }
}
