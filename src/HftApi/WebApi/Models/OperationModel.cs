namespace HftApi.WebApi.Models
{
    public class OperationModel
    {
        public string HistoricalId { set; get; }
        public string AssetId { set; get; }
        public decimal TotalAmount { set; get; }
        public decimal Fee { set; get; }
        public OperationType Type { set; get; }
    }

    public enum OperationType
    {
        Withdrawal,
        Deposit
    }
}
