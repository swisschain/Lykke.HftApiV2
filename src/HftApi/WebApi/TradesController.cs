using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
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
        private readonly IAssetsService _assetsService;
        private readonly HistoryHttpClient _historyClient;
        private readonly IMapper _mapper;
        private const int MaxPageSize = 500;

        public TradesController(
            IAssetsService assetsService,
            HistoryHttpClient historyClient,
            IMapper mapper
            )
        {
            _assetsService = assetsService;
            _historyClient = historyClient;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TradeModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrades(
            [FromQuery]string assetPairId = null,
            [FromQuery]OrderAction? side = null,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100,
            [FromQuery]DateTime? from = null,
            [FromQuery]DateTime? to = null
            )
        {
            if (!string.IsNullOrEmpty(assetPairId))
            {
                var assetPair = await _assetsService.GetAssetPairByIdAsync(assetPairId);

                if (assetPair == null)
                {
                    throw HftApiException.Create(HftApiErrorCode.ItemNotFound, HftApiErrorMessages.AssetPairNotFound)
                        .AddField(nameof(assetPairId));
                }
            }

            if (offset.HasValue && offset < 0)
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.LessThanZero(nameof(offset)))
                    .AddField(nameof(offset));

            if (take.HasValue && take < 0)
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.LessThanZero(nameof(take)))
                    .AddField(nameof(take));

            if (take.HasValue && take > MaxPageSize)
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.TooBig(nameof(take), take.Value.ToString(), MaxPageSize.ToString()))
                    .AddField(nameof(take));

            var trades = await _historyClient.GetTradersAsync(User.GetWalletId(), assetPairId, offset, take, side, from, to);

            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades)));
        }

        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TradeModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> OrderTrades(string orderId)
        {
            var trades = await _historyClient.GetOrderTradesAsync(User.GetWalletId(), orderId);
            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades)));
        }
    }
}
