using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.WebApi.Models;
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
        private readonly IMapper _mapper;

        public OrderbooksController(
            IOrderbooksService orderbooksService,
            IMapper mapper
            )
        {
            _orderbooksService = orderbooksService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<OrderbookModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderbooks(string assetPairId = null, int? depth = null)
        {
            var orderbooks = await _orderbooksService.GetAsync(assetPairId, depth);
            return Ok(ResponseModel<IReadOnlyCollection<OrderbookModel>>.Ok(_mapper.Map<IReadOnlyCollection<OrderbookModel>>(orderbooks)));
        }
    }
}
