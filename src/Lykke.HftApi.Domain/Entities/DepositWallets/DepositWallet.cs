namespace Lykke.HftApi.Domain.Entities.DepositWallets
{
    public class DepositWallet
    {
        public string Address { get; set; }
        public string BaseAddress { get; set; }
        public string AddressExtension { get; set; }
        public DepositWalletState State { set; get; }
    }
}
