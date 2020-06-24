namespace HftApi.WebApi.Models
{
    public class PriceModel
    {
        public string AssetPairId { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public long Timestamp { get; set; }
    }
}
