namespace Lykke.HftApi.Domain.Entities
{
    public class DepositWallet
    {
        public string Address { get; set; }
        public string BaseAddress { get; set; }
        public string AddressExtension { get; set; }
        public DepositWalletState State { set; get; }
    }

    public enum DepositWalletState
    {
        NotFound,
        Creating,
        Active,
        Blocked
    }
}
