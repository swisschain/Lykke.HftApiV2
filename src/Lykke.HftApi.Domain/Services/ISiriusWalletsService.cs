using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities.Assets;
using Lykke.HftApi.Domain.Entities.DepositWallets;

namespace Lykke.HftApi.Domain.Services
{
    public interface ISiriusWalletsService
    {
        Task<Asset> CheckDepositPreconditionsAsync(string clientId, string assetId = default);
        Task CreateWalletAsync(string clientId, string walletId);
        Task<List<DepositWallet>> GetWalletAddressesAsync(string clientId, string walletId, long? siriusAssetId = null);
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
