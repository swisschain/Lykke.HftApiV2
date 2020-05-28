using System.Collections.Generic;
using System.Threading.Tasks;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
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
        private readonly IBalanceService _balanceService;

        public BalanceController(
            IBalanceService balanceService
            )
        {
            _balanceService = balanceService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Balance>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBalances()
        {
            var walletId = User.GetWalletId();
            var balances = await _balanceService.GetBalancesAsync(walletId);

            return Ok(ResponseModel<IReadOnlyCollection<Balance>>.Ok(balances));
        }
    }
}
