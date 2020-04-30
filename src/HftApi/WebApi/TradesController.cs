using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Services;
using Lykke.MatchingEngine.Connector.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Authorize]
    [Route("api/trades")]
    public class TradesController : ControllerBase
    {
        private readonly HistoryHttpClient _historyClient;

        public TradesController(
            HistoryHttpClient historyClient)
        {
            _historyClient = historyClient;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Trade>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrades(
            [FromQuery]string asetPairId = null,
            [FromQuery]OrderAction? side = null,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100,
            [FromQuery]DateTime? from = null,
            [FromQuery]DateTime? to = null
            )
        {
            //TODO: validation
            var trades = await _historyClient.GetTradersAsync(User.GetWalletId(), asetPairId, offset, take, side, from, to);
            return Ok(ResponseModel<IReadOnlyCollection<Trade>>.Ok(trades));
        }

        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Trade>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> OrderTrades(string orderId)
        {
            var trades = await _historyClient.GetOrderTradesAsync(User.GetWalletId(), orderId);
            return Ok(ResponseModel<IReadOnlyCollection<Trade>>.Ok(trades));
        }
    }
}
