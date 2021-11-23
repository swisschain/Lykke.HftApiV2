using System;

namespace Lykke.HftApi.Domain.Entities.OperationsHistory
{
    public class OperationHistoricRecord
    {
        public string OperationId { set; get; }
        public string AssetId { set; get; }
        public decimal TotalVolume { set; get; }
        public decimal Fee { set; get; }
        public OperationType Type { set; get; }
        public DateTime Timestamp { set; get; }
        public string BlockchainHash { set; get; }
    }
}
