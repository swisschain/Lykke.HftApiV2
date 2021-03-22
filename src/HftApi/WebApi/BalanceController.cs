using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Extensions;
using HftApi.WebApi.Models;
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
        private readonly IMapper _mapper;

        public BalanceController(
            IBalanceService balanceService,
            IMapper mapper
            )
        {
            _balanceService = balanceService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get the current balance
        /// </summary>
        /// <remarks>Get the current balance from the API Key account.</remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<BalanceModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBalances()
        {
            var walletId = User.GetWalletId();
            var balances = await _balanceService.GetBalancesAsync(walletId);

            return Ok(ResponseModel<IReadOnlyCollection<BalanceModel>>.Ok(_mapper.Map<IReadOnlyCollection<BalanceModel>>(balances)));
        }
    }
}
