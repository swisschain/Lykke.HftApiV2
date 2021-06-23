using System;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Entities.Assets;
using Lykke.HftApi.Domain.Entities.DepositWallets;
using Lykke.HftApi.Domain.Entities.Withdrawals;
using Swisschain.Sirius.Api.ApiContract.Account;

namespace Lykke.HftApi.Domain.Services
{
    public interface ISiriusWalletsService
    {
        Task<Asset> CheckDepositPreconditionsAsync(string clientId, string assetId = default);
        Task CreateWalletAsync(string clientId, string walletId);
        Task<DepositWallet> GetWalletAddressAsync(string clientId, string walletId, long siriusAssetId);
        Task<Guid> CreateWithdrawalAsync(
            string requestId,
            string clientId,
            string walletId,
            string assetId,
            decimal volume,
            string destinationAddress,
            string destinationAddressExtension);
    }
}
