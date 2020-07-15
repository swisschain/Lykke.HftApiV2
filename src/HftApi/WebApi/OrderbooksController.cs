using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Route("api/orderbooks")]
    public class OrderbooksController : ControllerBase
    {
        private readonly IOrderbooksService _orderbooksService;
        private readonly ValidationService _validationService;
        private readonly IMapper _mapper;

        public OrderbooksController(
            IOrderbooksService orderbooksService,
            ValidationService validationService,
            IMapper mapper
            )
        {
            _orderbooksService = orderbooksService;
            _validationService = validationService;
            _mapper = mapper;
        }

        /// <summary>
        /// Asset Pair Order Book Ticker
        /// </summary>
        /// <remarks>Get the order book by asset pair. The order books contain a list of Buy(Bid) and Sell(Ask) orders with their corresponding price and volume.</remarks>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<OrderbookModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrderbooks(string assetPairId = null, int? depth = null)
        {
            var result = await _validationService.ValidateAssetPairAsync(assetPairId);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message).AddField(result.FieldName);

            var orderbooks = await _orderbooksService.GetAsync(assetPairId, depth);
            return Ok(ResponseModel<IReadOnlyCollection<OrderbookModel>>.Ok(_mapper.Map<IReadOnlyCollection<OrderbookModel>>(orderbooks)));
        }
    }
}
