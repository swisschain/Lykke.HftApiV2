using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using HftApi.WebApi.Models;
using Lykke.Exchange.Api.MarketData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Route("api/tickers")]
    public class TickersController : ControllerBase
    {
        private readonly MarketDataService.MarketDataServiceClient _marketDataClient;

        public TickersController(
            MarketDataService.MarketDataServiceClient marketDataClient)
        {
            _marketDataClient = marketDataClient;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<MarketSlice>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTickers([FromQuery]string[] assetPairIds)
        {
            var marketData = await _marketDataClient.GetMarketDataAsync(new Empty());
            var result = marketData.Items.ToList();

            if (assetPairIds.Any())
            {
                result = result.Where(x =>
                        assetPairIds.Contains(x.AssetPairId, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }


            return Ok(ResponseModel<IReadOnlyCollection<MarketSlice>>.Ok(result));
        }
    }
}
