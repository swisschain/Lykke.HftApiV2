using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;

namespace Lykke.HftApi.Services
{
    public class BalanceService : IBalanceService
    {
        private readonly IAssetsService _assetsService;
        private readonly BalanceHttpClient _balanceClient;

        public BalanceService(
            IAssetsService assetsService,
            BalanceHttpClient balanceClient
            )
        {
            _assetsService = assetsService;
            _balanceClient = balanceClient;
        }

        public async Task<IReadOnlyCollection<Balance>> GetBalancesAsync(string walletId)
        {
            var balances = await _balanceClient.GetBalanceAsync(walletId);

            var assetIds = balances.Select(x => x.AssetId).Distinct().ToList();

            var assets = (await _assetsService.GetAllAssetsAsync())
                .Where(x => assetIds.Contains(x.AssetId, StringComparer.InvariantCultureIgnoreCase))
                .ToList();

            foreach (var wallet in balances)
            {
                var asset = assets.FirstOrDefault(x => x.AssetId == wallet.AssetId);

                if (asset == null)
                    continue;

                wallet.Available = wallet.Available.TruncateDecimalPlaces(asset.Accuracy);
                wallet.Reserved = wallet.Reserved.TruncateDecimalPlaces(asset.Accuracy);
            }

            return balances;
        }
    }
}
