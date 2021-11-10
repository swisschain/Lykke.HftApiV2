namespace Lykke.HftApi.Domain.Entities.DepositWallets
{
    public class DepositWallet
    {
        public string AssetId { get; set; }
        public string Symbol { get; set; }
        public string Address { get; set; }
        public string BaseAddress { get; set; }
        public string AddressExtension { get; set; }
        public DepositWalletState State { set; get; }
    }
}
