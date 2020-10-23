namespace HftApi.WebApi.Models
{
    public class PublicTradeModel
    {
        public string Id { get; set; }
        public string AssetPairId { get; set; }
        public long DateTime { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public TradeSide Side { get; set; }
    }
}
