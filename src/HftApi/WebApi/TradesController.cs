using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Services;
using Lykke.MatchingEngine.Connector.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyNoSqlServer.Abstractions;

namespace HftApi.WebApi
{
    [ApiController]
    [Authorize]
    [Route("api/trades")]
    public class TradesController : ControllerBase
    {
        private readonly ValidationService _validationService;
        private readonly IMyNoSqlServerDataReader<TradeEntity> _tradesReader;
        private readonly IMapper _mapper;
        private const int MaxPageSize = 500;

        public TradesController(
            ValidationService validationService,
            IMyNoSqlServerDataReader<TradeEntity> tradesReader,
            IMapper mapper
            )
        {
            _validationService = validationService;
            _tradesReader = tradesReader;
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
            var result = await _validationService.ValidateOrdersRequestAsync(assetPairId, offset, take);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message)
                    .AddField(result.FieldName);

            var trades = _tradesReader.Get(User.GetWalletId(), offset ?? 0, take ?? MaxPageSize,
                x =>
                    (string.IsNullOrEmpty(assetPairId) || x.AssetPairId == assetPairId) &&
                    (!side.HasValue || (side == OrderAction.Buy && x.Role == "Maker" || side == OrderAction.Sell && x.Role == "Taker")) &&
                    (!from.HasValue || x.CreatedAt >= from.Value) &&
                    (!to.HasValue || x.CreatedAt <= to.Value));

            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades.OrderByDescending(x => x.CreatedAt))));
        }

        [HttpGet("order/{orderId}")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TradeModel>>), StatusCodes.Status200OK)]
        public IActionResult OrderTrades(string orderId)
        {
            var trades = _tradesReader.Get(User.GetWalletId(), orderId);
            return Ok(ResponseModel<IReadOnlyCollection<TradeModel>>.Ok(_mapper.Map<IReadOnlyCollection<TradeModel>>(trades)));
        }
    }
}
