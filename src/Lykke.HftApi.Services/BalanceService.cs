using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common;
using HftApi.Common.Domain.MyNoSqlEntities;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using MyNoSqlServer.Abstractions;

namespace Lykke.HftApi.Services
{
    public class BalanceService : IBalanceService
    {
        private readonly IAssetsService _assetsService;
        private readonly BalanceHttpClient _balanceClient;
        private readonly IMyNoSqlServerDataReader<BalanceEntity> _balancesReader;
        private readonly IMapper _mapper;

        public BalanceService(
            IAssetsService assetsService,
            BalanceHttpClient balanceClient,
            IMyNoSqlServerDataReader<BalanceEntity> balancesReader,
            IMapper mapper
            )
        {
            _assetsService = assetsService;
            _balanceClient = balanceClient;
            _balancesReader = balancesReader;
            _mapper = mapper;
        }

        public async Task<IReadOnlyCollection<Balance>> GetBalancesAsync(string walletId)
        {
            var result = new List<Balance>();
            var balanceEntities = _balancesReader.Get(walletId);

            if (balanceEntities.Any())
            {
                result.AddRange(_mapper.Map<IReadOnlyCollection<Balance>>(balanceEntities));
            }
            else
            {
                var balances = await _balanceClient.GetBalanceAsync(walletId);
                result.AddRange(balances);
            }

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
