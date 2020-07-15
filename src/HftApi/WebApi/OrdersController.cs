using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using HftApi.WebApi.Models.Request;
using HftApi.WebApi.Models.Response;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Services;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.History.Contracts.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyNoSqlServer.Abstractions;
using MarketOrderResponse = HftApi.WebApi.Models.Response.MarketOrderResponse;

namespace HftApi.WebApi
{
    [ApiController]
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly HistoryHttpClient _historyClient;
        private readonly ValidationService _validationService;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly IMyNoSqlServerDataReader<OrderEntity> _ordersReader;
        private readonly IMapper _mapper;

        public OrdersController(
            HistoryHttpClient historyClient,
            ValidationService validationService,
            IMatchingEngineClient matchingEngineClient,
            IMyNoSqlServerDataReader<OrderEntity> ordersReader,
            IMapper mapper
            )
        {
            _historyClient = historyClient;
            _validationService = validationService;
            _matchingEngineClient = matchingEngineClient;
            _ordersReader = ordersReader;
            _mapper = mapper;
        }

        /// <summary>
        /// Place a limit order
        /// </summary>
        /// <remarks>Place a limit order.</remarks>
        [HttpPost("limit")]
        [ProducesResponseType(typeof(ResponseModel<LimitOrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceLimitOrder(PlaceLimitOrderRequest request)
        {
            var walletId = User.GetWalletId();

            var result = await _validationService.ValidateLimitOrderAsync(walletId, request.AssetPairId, request.Side, request.Price, request.Volume);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message).AddField(result.FieldName);

            var order = new LimitOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = request.AssetPairId,
                ClientId = walletId,
                Price = (double)request.Price,
                CancelPreviousOrders = false,
                Volume = (double)Math.Abs(request.Volume),
                OrderAction = request.Side
            };

            MeResponseModel response = await _matchingEngineClient.PlaceLimitOrderAsync(order);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return Ok(ResponseModel<LimitOrderResponse>.Ok(new LimitOrderResponse {OrderId = response.TransactionId}));

            throw HftApiException.Create(code, message);
        }

        /// <summary>
        /// Place multiple limit orders
        /// </summary>
        /// <remarks>Place multiple limit orders in one package. The method also allows you to replace orders in the order book. You can replace all orders completely, or each separately.</remarks>
        [HttpPost("bulk")]
        [ProducesResponseType(typeof(ResponseModel<BulkLimitOrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceBulkOrder([FromBody] PlaceBulkLimitOrderRequest request)
        {
            var walletId = User.GetWalletId();

            var items = request.Orders?.ToArray() ?? Array.Empty<BulkOrderItemModel>();

            var orders = new List<MultiOrderItemModel>();

            foreach (var item in items)
            {
                var order = new MultiOrderItemModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Price = (double)item.Price,
                    Volume = (double)item.Volume,
                    OrderAction = item.OrderAction,
                    OldId = item.OldId
                };

                orders.Add(order);
            }

            var multiOrder = new MultiLimitOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = walletId,
                AssetPairId = request.AssetPairId,
                CancelPreviousOrders = request.CancelPreviousOrders,
                Orders = orders.ToArray()
            };

            if (request.CancelMode.HasValue)
            {
                multiOrder.CancelMode = request.CancelMode.Value;
            }

            MultiLimitOrderResponse response = await _matchingEngineClient.PlaceMultiLimitOrderAsync(multiOrder);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            var bulkResponse = new BulkLimitOrderResponse
            {
                AssetPairId = request.AssetPairId,
                Error = response.Status.ToHftApiError().code,
                Statuses = response.Statuses?.Select(x => new BulkOrderItemStatusModel
                {
                    Id = x.Id,
                    Price = (decimal)x.Price,
                    Volume = (decimal)x.Volume,
                    Error = x.Status.ToHftApiError().code
                }).ToArray()
            };

            return Ok(ResponseModel<BulkLimitOrderResponse>.Ok(bulkResponse));
        }

        /// <summary>
        /// Place a market order
        /// </summary>
        /// <remarks>Place a Fill-Or-Kill market order.</remarks>
        [HttpPost("market")]
        [ProducesResponseType(typeof(ResponseModel<MarketOrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceMarketOrder(PlaceMarketOrderRequest request)
        {
            var result = await _validationService.ValidateMarketOrderAsync(request.AssetPairId, request.Volume);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message).AddField(result.FieldName);

            var walletId = User.GetWalletId();

            var order = new MarketOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = request.AssetPairId,
                ClientId = walletId,
                Volume = (double)request.Volume,
                OrderAction = request.Side,
                Straight = true
            };

            var response = await _matchingEngineClient.HandleMarketOrderAsync(order);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
            {
                return Ok(ResponseModel<MarketOrderResponse>.Ok(new MarketOrderResponse {OrderId = order.Id, Price = (decimal)response.Price}));
            }

            throw HftApiException.Create(code, message);
        }

        /// <summary>
        /// Get active orders
        /// </summary>
        /// <remarks>Get active orders orders from history.</remarks>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<OrderModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveOrders(
            [FromQuery]string assetPairId = null,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100
            )
        {
            var result = await _validationService.ValidateOrdersRequestAsync(assetPairId, offset, take);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message).AddField(result.FieldName);

            var statuses = new List<string> {OrderStatus.Placed.ToString(), OrderStatus.PartiallyMatched.ToString()};

            var orders = _ordersReader.Get(User.GetWalletId(), offset ?? 0, take ?? 100,
                x => (string.IsNullOrEmpty(assetPairId) || x.AssetPairId == assetPairId) && statuses.Contains(x.Status));

            var ordersModel = _mapper.Map<IReadOnlyCollection<OrderModel>>(orders);

            return Ok(ResponseModel<IReadOnlyCollection<OrderModel>>.Ok(ordersModel));
        }

        /// <summary>
        /// Get closed orders
        /// </summary>
        /// <remarks>Get closed orders from history.</remarks>
        [HttpGet("closed")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<OrderModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCloasedOrders(
            [FromQuery]string assetPairId = null,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100
            )
        {
            var result = await _validationService.ValidateOrdersRequestAsync(assetPairId, offset, take);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message).AddField(result.FieldName);

            var orders = await _historyClient.GetOrdersByWalletAsync(User.GetWalletId(), assetPairId,
                new [] { OrderStatus.Matched, OrderStatus.Cancelled, OrderStatus.Replaced }, null, false, offset, take );

            var ordersModel = _mapper.Map<IReadOnlyCollection<OrderModel>>(orders);

            return Ok(ResponseModel<IReadOnlyCollection<OrderModel>>.Ok(_mapper.Map<IReadOnlyCollection<OrderModel>>(ordersModel)));
        }

        /// <summary>
        /// Mass cancel orders
        /// </summary>
        /// <remarks>Cancel all active orders or filter order to cancel by AssetPair or Side.</remarks>
        [HttpDelete]
        [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelAllOrders([FromQuery]string assetPairId = null, [FromQuery]OrderAction? side = null)
        {
            var assetPairResult = await _validationService.ValidateAssetPairAsync(assetPairId);

            if (assetPairResult != null)
                throw HftApiException.Create(assetPairResult.Code, assetPairResult.Message).AddField(assetPairResult.FieldName);

            bool? isBuy;
            switch (side)
            {
                case OrderAction.Buy:
                    isBuy = true;
                    break;
                case OrderAction.Sell:
                    isBuy = false;
                    break;
                default:
                    isBuy = null;
                    break;
            }

            var model = new LimitOrderMassCancelModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = assetPairId,
                ClientId = User.GetWalletId(),
                IsBuy = isBuy
            };

            MeResponseModel response = await _matchingEngineClient.MassCancelLimitOrdersAsync(model);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return Ok(ResponseModel<string>.Ok(null));

            throw HftApiException.Create(code, message);
        }

        /// <summary>
        /// Cancel orders by ID
        /// </summary>
        /// <remarks>Cancel a specific order by order ID.</remarks>
        [HttpDelete("{orderId}")]
        [ProducesResponseType(typeof(ResponseModel<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            MeResponseModel response = await _matchingEngineClient.CancelLimitOrderAsync(orderId);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return Ok(ResponseModel<string>.Ok(null));

            throw HftApiException.Create(code, message);
        }
    }
}
