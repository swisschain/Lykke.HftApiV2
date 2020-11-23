using System;
using MyNoSqlServer.Abstractions;

namespace HftApi.Common.Domain.MyNoSqlEntities
{
    public class BalanceEntity : IMyNoSqlDbEntity
    {
        public BalanceEntity() {}

        public BalanceEntity(string walletId, string assetId)
        {
            PartitionKey = walletId;
            RowKey = assetId;
            WalletId = walletId;
            AssetId = assetId;
        }

        public string WalletId { get; set; }
        public string AssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal Reserved { get; set; }
        public DateTime CreatedAt { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
    }
}
