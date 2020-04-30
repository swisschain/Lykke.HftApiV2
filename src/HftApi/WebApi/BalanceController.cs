using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Authorize]
    [Route("api/balance")]
    public class BalanceController : ControllerBase
    {
        private readonly IAssetsService _assetsService;
        private readonly BalanceHttpClient _balanceClient;

        public BalanceController(
            IAssetsService assetsService,
            BalanceHttpClient balanceClient
            )
        {
            _assetsService = assetsService;
            _balanceClient = balanceClient;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Balance>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBalances()
        {
            var walletId = User.GetWalletId();
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

            return Ok(ResponseModel<IReadOnlyCollection<Balance>>.Ok(balances));
        }
    }
}
