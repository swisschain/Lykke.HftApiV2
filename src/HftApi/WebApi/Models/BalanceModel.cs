namespace HftApi.WebApi.Models
{
    public class BalanceModel
    {
        public string AssetId { get; set; }
        public decimal Available { get; set; }
        public decimal Reserved { get; set; }
        public long Timestamp { get; set; }
    }
}
