using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.History.Contracts.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IAssetsService _assetsService;
        private readonly HistoryHttpClient _historyClient;
        private readonly IMatchingEngineClient _matchingEngineClient;

        public OrdersController(
            IAssetsService assetsService,
            HistoryHttpClient historyClient,
            IMatchingEngineClient matchingEngineClient
            )
        {
            _assetsService = assetsService;
            _historyClient = historyClient;
            _matchingEngineClient = matchingEngineClient;
        }

        [HttpPost("limit")]
        [ProducesResponseType(typeof(ResponseModel<LimitOrderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceLimitOrder(PlaceLimitOrderModel model)
        {
            //TODO: validation
            #region Validation
            var assetPair = await _assetsService.GetAssetPairByIdAsync(model.AssetPairId);

            if (assetPair == null)
            {
                throw HftApiException.Create(HftApiErrorCode.ItemNotFound, "Asset pair not found")
                    .AddField(nameof(model.AssetPairId));
            }

            if (model.Price <= 0)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, $"{nameof(model.Volume)} must be greater 0")
                    .AddField(nameof(model.Price));
            }

            if (model.Volume < assetPair.MinVolume)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, $"{nameof(model.Volume)} must be greater than {assetPair.MinVolume}")
                    .AddField(nameof(model.Volume));
            }

            #endregion

            var walletId = User.GetWalletId();

            var order = new LimitOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = model.AssetPairId,
                ClientId = walletId,
                Price = (double)model.Price,
                CancelPreviousOrders = false,
                Volume = (double)Math.Abs(model.Volume),
                OrderAction = model.Side
            };

            MeResponseModel response = await _matchingEngineClient.PlaceLimitOrderAsync(order);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return Ok(ResponseModel<LimitOrderResponse>.Ok(new LimitOrderResponse {OrderId = response.TransactionId}));

            throw HftApiException.Create(code, message);
        }

        [HttpPost("market")]
        [ProducesResponseType(typeof(ResponseModel<MarketOrderResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PlaceMarketOrder(PlaceMarketOrderModel model)
        {
            //TODO: validation
            #region Validation
            var assetPair = await _assetsService.GetAssetPairByIdAsync(model.AssetPairId);

            if (assetPair == null)
            {
                throw HftApiException.Create(HftApiErrorCode.ItemNotFound, "Asset pair not found")
                    .AddField(nameof(model.AssetPairId));
            }

            if (model.Volume < assetPair.MinVolume)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, $"{nameof(model.Volume)} must be greater than {assetPair.MinVolume}")
                    .AddField(nameof(model.Volume));
            }

            #endregion

            var walletId = User.GetWalletId();

            var order = new MarketOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = model.AssetPairId,
                ClientId = walletId,
                Volume = (double)Math.Abs(model.Volume),
                OrderAction = model.Side,
                Straight = true
            };

            MarketOrderResponse response = await _matchingEngineClient.HandleMarketOrderAsync(order);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
            {
                return Ok(ResponseModel<MarketOrderResponseModel>.Ok(new MarketOrderResponseModel {OrderId = order.Id, Price = (decimal)response.Price}));
            }

            throw HftApiException.Create(code, message);
        }

        [HttpGet("active")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Order>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveOrders(
            [FromQuery]string asetPairId = null,
            [FromQuery]bool withTrades = false,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100
            )
        {
            //TODO: validation
            var orders = await _historyClient.GetOrdersByWalletAsync(User.GetWalletId(), asetPairId, new []
            {
                OrderStatus.Placed,
                OrderStatus.PartiallyMatched
            }, null, withTrades, offset, take );
            return Ok(ResponseModel<IReadOnlyCollection<Order>>.Ok(orders));
        }

        [HttpGet("closed")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Order>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCloasedOrders(
            [FromQuery]string asetPairId = null,
            [FromQuery]bool withTrades = false,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100
            )
        {
            //TODO: validation
            var orders = await _historyClient.GetOrdersByWalletAsync(User.GetWalletId(), asetPairId,
                new [] { OrderStatus.Matched}, null, withTrades, offset, take );
            return Ok(ResponseModel<IReadOnlyCollection<Order>>.Ok(orders));
        }

        [HttpDelete]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelAllOrder(string assetPairId = null, OrderAction? side = null)
        {
            //TODO: validation

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
                Id = new Guid().ToString(),
                AssetPairId = assetPairId,
                ClientId = User.GetWalletId(),
                IsBuy = isBuy
            };

            MeResponseModel response = await _matchingEngineClient.MassCancelLimitOrdersAsync(model);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return Ok();

            throw HftApiException.Create(code, message);
        }

        [HttpDelete("{orderId}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            MeResponseModel response = await _matchingEngineClient.CancelLimitOrderAsync(orderId);

            if (response == null)
                throw HftApiException.Create(HftApiErrorCode.MeRuntime, "ME not available");

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return Ok();

            throw HftApiException.Create(code, message);
        }
    }
}
