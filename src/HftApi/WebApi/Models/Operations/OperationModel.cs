namespace HftApi.WebApi.Models.Operations
{
    public class OperationModel
    {
        public string OperationId { set; get; }
        public string AssetId { set; get; }
        public decimal TotalVolume { set; get; }
        public decimal Fee { set; get; }
        public OperationType Type { set; get; }
        public long Timestamp { set; get; }
    }
}
