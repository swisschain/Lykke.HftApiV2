using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Entities.DepositWallets;
using Swisschain.Sirius.Api.ApiContract.Account;

namespace Lykke.HftApi.Domain.Services
{
    public interface ISiriusWalletsService
    {
        Task CreateWalletAsync(string clientId, string walletId);
        Task<DepositWallet> GetWalletAddressAsync(string clientId, string walletId, long siriusAssetId);
    }
}
