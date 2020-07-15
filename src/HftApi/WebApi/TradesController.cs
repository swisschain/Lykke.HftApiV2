using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Exceptions;
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
        private readonly ValidationService _validationService;
        private readonly HistoryHttpClient _historyClient;
        private readonly IMapper _mapper;

        public TradesController(
            ValidationService validationService,
            HistoryHttpClient historyClient,
            IMapper mapper
            )
        {
            _validationService = validationService;
            _historyClient = historyClient;
            _mapper = mapper;
        }

        /// <summary>
        /// Get trade history
        /// </summary>
        /// <remarks>Gets the trading history of an account. Also, with the use of parameters, it can returns a single order.</remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TradeModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrades(
            [FromQuery]string assetPairId = null,
            [FromQuery]OrderAction? side = null,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100,
            [FromQuery]double? from = null,
            [FromQuery]double? to = null
            )
        {
            var result = await _validationService.ValidateOrdersRequestAsync(assetPairId, offset, take);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message)
                    .AddField(result.FieldName);

            DateTime? fromDate = from == null ? (DateTime?) null : DateTime.UnixEpoch.AddMilliseconds(from.Value);
            DateTime? toDate = to == null ? (DateTime?) null : DateTime.UnixEpoch.AddMilliseconds(to.Value);

            var trades = await _historyClient.GetTradersAsync(User.GetWalletId(), assetPairId, offset, take, side, fromDate, toDate);

            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades)));
        }

        /// <summary>
        /// Get order trades
        /// </summary>
        /// <remarks>Get trades for specific order.</remarks>
        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TradeModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> OrderTrades(string orderId)
        {
            var trades = await _historyClient.GetOrderTradesAsync(User.GetWalletId(), orderId);
            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades)));
        }
    }
}
