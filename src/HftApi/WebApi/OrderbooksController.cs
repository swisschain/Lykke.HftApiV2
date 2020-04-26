using System.Collections.Generic;
using System.Threading.Tasks;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Route("api/orderbooks")]
    public class OrderbooksController : ControllerBase
    {
        private readonly IOrderbooksService _orderbooksService;

        public OrderbooksController(
            IOrderbooksService orderbooksService
            )
        {
            _orderbooksService = orderbooksService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Orderbook>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderbooks(string assetPairId = null, int? depth = null)
        {
            var orderbooks = await _orderbooksService.GetAsync(assetPairId, depth);
            return Ok(ResponseModel<IReadOnlyCollection<Orderbook>>.Ok(orderbooks));
        }
    }
}
