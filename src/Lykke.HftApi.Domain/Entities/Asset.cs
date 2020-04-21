namespace Lykke.HftApi.Domain.Entities
{
    public class Asset
    {
        public string AssetId { get; set; }
        public string BlockchainId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int Accuracy { get; set; }
    }
}
