using System;
using MyNoSqlServer.Abstractions;

namespace HftApi.Common.Domain.MyNoSqlEntities
{
    public class BalanceEntity : IMyNoSqlEntity
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

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime? Expires { get; set; }
    }
}
