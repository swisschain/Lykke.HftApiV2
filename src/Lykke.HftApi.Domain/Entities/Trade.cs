using System;

namespace Lykke.HftApi.Domain.Entities
{
    public class Trade
    {
        public string Id { get; set; }
        public string WalletId { get; set; }
        public DateTime Timestamp { get; set; }
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
    }
}
