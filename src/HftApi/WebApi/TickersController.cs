using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.WebApi.Models;
using Lykke.Exchange.Api.MarketData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyNoSqlServer.Abstractions;

namespace HftApi.WebApi
{
    [ApiController]
    [Route("api/tickers")]
    public class TickersController : ControllerBase
    {
        private readonly IMyNoSqlServerDataReader<TickerEntity> _tickersReader;
        private readonly MarketDataService.MarketDataServiceClient _marketDataClient;
        private readonly IMapper _mapper;

        public TickersController(
            IMyNoSqlServerDataReader<TickerEntity> tickersReader,
            MarketDataService.MarketDataServiceClient marketDataClient,
            IMapper mapper
            )
        {
            _tickersReader = tickersReader;
            _marketDataClient = marketDataClient;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<TickerModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTickers([FromQuery]string[] assetPairIds)
        {
            var entities = _tickersReader.Get(TickerEntity.GetPk());

            List<TickerModel> result;

            if (entities.Any())
            {
                result = _mapper.Map<List<TickerModel>>(entities);
            }
            else
            {
                var marketData = await _marketDataClient.GetMarketDataAsync(new Empty());
                result = _mapper.Map<List<TickerModel>>(marketData.Items.ToList());
            }

            if (assetPairIds.Any())
            {
                result = result.Where(x =>
                        assetPairIds.Contains(x.AssetPairId, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }

            return Ok(ResponseModel<IReadOnlyCollection<TickerModel>>.Ok(result));
        }
    }
}
