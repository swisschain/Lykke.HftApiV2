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
            var result = new List<Balance>();

            var balances = await _balanceClient.GetBalanceAsync(walletId);
            result.AddRange(balances);

            await SetBalancesAccuracyAsync(result);

            return result;
        }

        private async Task SetBalancesAccuracyAsync(IReadOnlyList<Balance> balances)
        {
            var assetIds = balances.Select(x => x.AssetId).Distinct().ToList();

            var assets = (await _assetsService.GetAllAssetsAsync())
                .Where(x => assetIds.Contains(x.AssetId, StringComparer.InvariantCultureIgnoreCase))
                .ToList();

            foreach (var balance in balances)
            {
                var asset = assets.FirstOrDefault(x => x.AssetId == balance.AssetId);

                if (asset == null)
                    continue;

                balance.Available = balance.Available.TruncateDecimalPlaces(asset.Accuracy);
                balance.Reserved = balance.Reserved.TruncateDecimalPlaces(asset.Accuracy);
            }
        }
    }
}
