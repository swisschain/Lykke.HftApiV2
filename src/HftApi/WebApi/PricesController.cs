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
    [Route("api/prices")]
    public class PricesController : ControllerBase
    {
        private readonly IMyNoSqlServerDataReader<PriceEntity> _pricesReader;
        private readonly MarketDataService.MarketDataServiceClient _marketDataClient;
        private readonly IMapper _mapper;

        public PricesController(
            IMyNoSqlServerDataReader<PriceEntity> pricesReader,
            MarketDataService.MarketDataServiceClient marketDataClient,
            IMapper mapper
            )
        {
            _pricesReader = pricesReader;
            _marketDataClient = marketDataClient;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<PriceModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPrices([FromQuery]string[] assetPairIds)
        {
            var entities = _pricesReader.Get(PriceEntity.GetPk());

            List<PriceModel> result;

            if (entities.Any())
            {
                result = _mapper.Map<List<PriceModel>>(entities);
            }
            else
            {
                var marketData = await _marketDataClient.GetMarketDataAsync(new Empty());
                result = _mapper.Map<List<PriceModel>>(marketData.Items.ToList());
            }

            if (assetPairIds.Any())
            {
                result = result.Where(x =>
                        assetPairIds.Contains(x.AssetPairId, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }

            return Ok(ResponseModel<IReadOnlyCollection<PriceModel>>.Ok(result));
        }
    }
}
