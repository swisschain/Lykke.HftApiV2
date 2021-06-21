namespace Lykke.HftApi.Services.AssetsClient
{
    public class AssetResponseModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayId { get; set; }
        public int? DisplayAccuracy { get; set; }
        public int Accuracy { get; set; }
        public int MultiplierPower { set; get; }
        public string AssetAddress { set; get; }
        public string BlockchainIntegrationLayerId { set; get; }
        public BlockchainResponse Blockchain { set; get; }
        public AssetTypeResponse? Type { set; get; }
        public bool IsTradable { set; get; }
        public bool IsTrusted { set; get; }
        public bool KycNeeded { set; get; }
        public bool BlockchainWithdrawal { set; get; }
        public double CashoutMinimalAmount { set; get; }
        public double? LowVolumeAmount { set; get; }
        public string LykkeEntityId { set; get; }
        public long SiriusAssetId { set; get; }
        public BlockchainIntegrationTypeResponse BlockchainIntegrationType { set; get; }
        public bool IsDisabled { set; get; }
        public bool BlockchainDepositEnabled { set; get; }
    }
}
