using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const int MaxPageSize = 500;
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
            #region Validation

            var assetPair = await _assetsService.GetAssetPairByIdAsync(model.AssetPairId);

            if (assetPair == null)
            {
                throw HftApiException.Create(HftApiErrorCode.ItemNotFound, HftApiErrorMessages.AssetPairNotFound)
                    .AddField(nameof(model.AssetPairId));
            }

            if (model.Price <= 0)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.LessThanZero(nameof(model.Price)))
                    .AddField(nameof(model.Price));
            }

            if (model.Volume <= 0)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.LessThanZero(nameof(model.Volume)))
                    .AddField(nameof(model.Volume));
            }

            if (model.Volume < assetPair.MinVolume)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.MustBeGreaterThan(nameof(model.Volume), assetPair.MinVolume.ToString(CultureInfo.InvariantCulture)))
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
            #region Validation

            var assetPair = await _assetsService.GetAssetPairByIdAsync(model.AssetPairId);

            if (assetPair == null)
            {
                throw HftApiException.Create(HftApiErrorCode.ItemNotFound, HftApiErrorMessages.AssetPairNotFound)
                    .AddField(nameof(model.AssetPairId));
            }

            if (model.Volume <= 0)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.LessThanZero(nameof(model.Volume)))
                    .AddField(nameof(model.Volume));
            }

            if (model.Volume < assetPair.MinVolume)
            {
                throw HftApiException.Create(HftApiErrorCode.InvalidField, HftApiErrorMessages.MustBeGreaterThan(nameof(model.Volume), assetPair.MinVolume.ToString(CultureInfo.InvariantCulture)))
                    .AddField(nameof(model.Volume));
            }

            #endregion

            var walletId = User.GetWalletId();

            var order = new MarketOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = model.AssetPairId,
                ClientId = walletId,
                Volume = (double)model.Volume,
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
            [FromQuery]string assetPairId = null,
            [FromQuery]bool withTrades = false,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100
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

            var orders = await _historyClient.GetOrdersByWalletAsync(User.GetWalletId(), assetPairId, new []
            {
                OrderStatus.Placed,
                OrderStatus.PartiallyMatched
            }, null, withTrades, offset, take );

            return Ok(ResponseModel<IReadOnlyCollection<Order>>.Ok(orders));
        }

        [HttpGet("closed")]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyCollection<Order>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCloasedOrders(
            [FromQuery]string assetPairId = null,
            [FromQuery]bool withTrades = false,
            [FromQuery]int? offset = 0,
            [FromQuery]int? take = 100
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

            var orders = await _historyClient.GetOrdersByWalletAsync(User.GetWalletId(), assetPairId,
                new [] { OrderStatus.Matched}, null, withTrades, offset, take );

            return Ok(ResponseModel<IReadOnlyCollection<Order>>.Ok(orders));
        }

        [HttpDelete]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelAllOrders(string assetPairId = null, OrderAction? side = null)
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