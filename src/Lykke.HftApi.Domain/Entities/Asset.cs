namespace Lykke.HftApi.Domain.Entities
{
    public class Asset
    {
        public string AssetId { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public int Accuracy { get; set; }
        public int MultiplierPower { set; get; }
        public string AssetAddress { set; get; }
        public string BlockchainIntegrationLayerId { set; get; }
        public Blockchain Blockchain { set; get; }
        public AssetType? Type { set; get; }
        public bool IsTradable { set; get; }
        public bool IsTrusted { set; get; }
        public bool KycNeeded { set; get; }
        public bool BlockchainWithdrawal { set; get; }
        public double CashoutMinimalAmount { set; get; }
        public double? LowVolumeAmount { set; get; }
        public string LykkeEntityId { set; get; }
        public long SiriusAssetId { set; get; }
        public BlockchainIntegrationType BlockchainIntegrationType { set; get; }
        public bool BlockchainDepositEnabled { set; get; }
        public bool IsDisabled { set; get; }
    }
    
    public enum BlockchainIntegrationType
    {
        None,
        Bil,
        Sirius,
    }
    
    public enum AssetType
    {
        Erc20Token,
    }
    
    public enum Blockchain
    {
        None,
        Bitcoin,
        Ethereum,
    }
}
