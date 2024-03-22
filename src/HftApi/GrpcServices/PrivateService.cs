﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Antares.Service.History.GrpcContract.Common;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HftApi.Extensions;
using HftApi.WebApi.Models.Withdrawals;
using JetBrains.Annotations;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities.OperationsHistory;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Models.Common;
using Microsoft.AspNetCore.Authorization;
using BulkLimitOrderResponse = Lykke.HftApi.ApiContract.BulkLimitOrderResponse;
using LimitOrderResponse = Lykke.HftApi.ApiContract.LimitOrderResponse;
using MarketOrderResponse = Lykke.HftApi.ApiContract.MarketOrderResponse;

namespace HftApi.GrpcServices
{
    [Authorize]
    [UsedImplicitly]
    public class PrivateService : Lykke.HftApi.ApiContract.PrivateService.PrivateServiceBase
    {
        private readonly HistoryWrapperClient _historyClient;
        private readonly IBalanceService _balanceService;
        private readonly ValidationService _validationService;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly BalancesStreamService _balanceUpdateService;
        private readonly OrdersStreamService _orderUpdateService;
        private readonly TradesStreamService _tradeUpdateService;
        private readonly ISiriusWalletsService _siriusWalletsService;
        private readonly IMapper _mapper;

        public PrivateService(
            HistoryWrapperClient historyClient,
            IBalanceService balanceService,
            ValidationService validationService,
            IMatchingEngineClient matchingEngineClient,
            BalancesStreamService balanceUpdateService,
            OrdersStreamService orderUpdateService,
            TradesStreamService tradeUpdateService,
            ISiriusWalletsService siriusWalletsService,
            IMapper mapper
            )
        {
            _historyClient = historyClient;
            _balanceService = balanceService;
            _validationService = validationService;
            _matchingEngineClient = matchingEngineClient;
            _balanceUpdateService = balanceUpdateService;
            _orderUpdateService = orderUpdateService;
            _tradeUpdateService = tradeUpdateService;
            _siriusWalletsService = siriusWalletsService;
            _mapper = mapper;
        }

        public override async Task<BalancesResponse> GetBalances(Empty request, ServerCallContext context)
        {
            var walletId = context.GetHttpContext().User.GetWalletId();
            var balances = await _balanceService.GetBalancesAsync(walletId);

            var res = new BalancesResponse();
            res.Payload.AddRange(_mapper.Map<List<Balance>>(balances));

            return res;
        }

        public override async Task<LimitOrderResponse> PlaceLimitOrder(LimitOrderRequest request, ServerCallContext context)
        {
            var walletId = context.GetHttpContext().User.GetWalletId();
            var orderAction = _mapper.Map<OrderAction>(request.Side);

            var result = await _validationService.ValidateLimitOrderAsync(
                walletId, 
                request.AssetPairId, 
                orderAction, 
                Convert.ToDecimal(request.Price, CultureInfo.InvariantCulture), 
                Convert.ToDecimal(request.Volume, CultureInfo.InvariantCulture));

            if (result != null)
            {
                return new LimitOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(result.Code),
                        Message = result.Message
                    }
                };
            }

            var order = new LimitOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = request.AssetPairId,
                ClientId = walletId,
                Price = Convert.ToDouble(request.Price),
                CancelPreviousOrders = false,
                Volume = Math.Abs(Convert.ToDouble(request.Volume)),
                OrderAction = orderAction
            };

            MeResponseModel response = await _matchingEngineClient.PlaceLimitOrderAsync(order);

            if (response == null)
            {
                return new LimitOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(HftApiErrorCode.MeRuntime),
                        Message = "ME not available"
                    }
                };
            }

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return new LimitOrderResponse
                {
                    Payload = new LimitOrderResponse.Types.LimitOrderPayload
                    {
                        OrderId = response.TransactionId
                    }
                };

            return new LimitOrderResponse
            {
                Error = new Error
                {
                    Code = _mapper.Map<ErrorCode>(code),
                    Message = message
                }
            };
        }

        public override async Task<BulkLimitOrderResponse> PlaceBulkLimitOrder(BulkLimitOrderRequest request, ServerCallContext context)
        {
            var walletId = context.GetHttpContext().User.GetWalletId();

            var items = request.Orders?.ToArray() ?? Array.Empty<BulkOrder>();

            var orders = new List<MultiOrderItemModel>();

            foreach (var item in items)
            {
                var order = new MultiOrderItemModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Price = Convert.ToDouble(item.Price),
                    Volume = Convert.ToDouble(item.Volume),
                    OrderAction = _mapper.Map<OrderAction>(item.Side),
                    OldId = string.IsNullOrEmpty(item.OldId) ? null : item.OldId
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

            if (request.OptionalCancelModeCase != BulkLimitOrderRequest.OptionalCancelModeOneofCase.None)
            {
                multiOrder.CancelMode = _mapper.Map<Lykke.MatchingEngine.Connector.Models.Api.CancelMode>(request.CancelMode);
            }

            MultiLimitOrderResponse response = await _matchingEngineClient.PlaceMultiLimitOrderAsync(multiOrder);

            if (response == null)
            {
                return new BulkLimitOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(HftApiErrorCode.MeRuntime),
                        Message = "ME not available"
                    }
                };
            }

            var bulkResponse = new BulkLimitOrderResponse
            {
                Payload = new BulkLimitOrderResponse.Types.BulkLimitOrderPayload
                {
                    AssetPairId = request.AssetPairId
                }
            };

            bulkResponse.Payload.Statuses.AddRange(response.Statuses?.Select(x => new BulkOrderItemStatus
            {
                Id = x.Id,
                Price = x.Price.ToString(CultureInfo.InvariantCulture),
                Volume = x.Volume.ToString(CultureInfo.InvariantCulture),
                Error = _mapper.Map<ErrorCode>(x.Status.ToHftApiError().code)
            }));

            return bulkResponse;
        }

        public override async Task<MarketOrderResponse> PlaceMarketOrder(MarketOrderRequest request, ServerCallContext context)
        {
            var walletId = context.GetHttpContext().User.GetWalletId();

            var result = await _validationService.ValidateMarketOrderAsync(request.AssetPairId, Convert.ToDecimal(request.Volume, CultureInfo.InvariantCulture));

            if (result != null)
            {
                return new MarketOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(result.Code),
                        Message = result.Message
                    }
                };
            }

            var order = new MarketOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = request.AssetPairId,
                ClientId = walletId,
                Volume = Math.Abs(Convert.ToDouble(request.Volume)),
                OrderAction = _mapper.Map<OrderAction>(request.Side),
                Straight = true
            };

            var response = await _matchingEngineClient.HandleMarketOrderAsync(order);

            if (response == null)
            {
                return new MarketOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(HftApiErrorCode.MeRuntime),
                        Message = "ME not available"
                    }
                };
            }

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return new MarketOrderResponse
                {
                    Payload = new MarketOrderResponse.Types.MarketOrderPayload
                    {
                        OrderId = order.Id,
                        Price = response.Price.ToString(CultureInfo.InvariantCulture)
                    }
                };

            return new MarketOrderResponse
            {
                Error = new Error
                {
                    Code = _mapper.Map<ErrorCode>(code),
                    Message = message
                }
            };
        }

        public override async Task<OrderResponse> GetOrder(OrderRequest request, ServerCallContext context)
        {
            if (string.IsNullOrEmpty(request.OrderId))
            {
                return new OrderResponse
                {
                    Error = new Error
                    {
                        Code = ErrorCode.InvalidField,
                        Message = $"{nameof(request.OrderId)} is required"
                    }
                };
            }

            var order = await _historyClient.GetOrderAsync(request.OrderId);

            return new OrderResponse {Payload = _mapper.Map<Order>(order)};
        }

        public override async Task<OrdersResponse> GetActiveOrders(OrdersRequest request, ServerCallContext context)
        {
            var result = await _validationService.ValidateOrdersRequestAsync(request.AssetPairId, request.Offset, request.Take);

            if (result != null)
            {
                return new OrdersResponse
                {
                    Error = new Error
                    {
                        Code =_mapper.Map<ErrorCode>(result.Code),
                        Message = result.Message
                    }
                };
            }

            if (request.Take == 0)
                request.Take = Constants.MaxPageSize;

            var orders = await _historyClient.GetOrdersByWalletAsync(context.GetHttpContext().User.GetWalletId(), request.AssetPairId,
                new [] { OrderStatus.Placed, OrderStatus.PartiallyMatched }, null, false, request.Offset, request.Take);

            var res = new OrdersResponse();
            res.Payload.AddRange(_mapper.Map<List<Order>>(orders));
            return res;
        }

        public override async Task<OrdersResponse> GetClosedOrders(OrdersRequest request, ServerCallContext context)
        {
            var result = await _validationService.ValidateOrdersRequestAsync(request.AssetPairId, request.Offset, request.Take);

            if (result != null)
            {
                return new OrdersResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(result.Code),
                        Message = result.Message
                    }
                };
            }

            if (request.Take == 0)
                request.Take = Constants.MaxPageSize;

            var orders = await _historyClient.GetOrdersByWalletAsync(context.GetHttpContext().User.GetWalletId(), request.AssetPairId,
                new [] { OrderStatus.Matched, OrderStatus.Cancelled, OrderStatus.Replaced }, null, false, request.Offset, request.Take);

            var res = new OrdersResponse();

            res.Payload.AddRange(_mapper.Map<List<Order>>(orders));

            return res;
        }

        public override async Task<CancelOrderResponse> CancelAllOrders(CancelOrdersRequest request, ServerCallContext context)
        {
            var result = await _validationService.ValidateAssetPairAsync(request.AssetPairId);

            if (result != null)
            {
                return new CancelOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(result.Code),
                        Message = result.Message
                    }
                };
            }

            bool? isBuy;
            switch (request.Side)
            {
                case Side.Buy:
                    isBuy = true;
                    break;
                case Side.Sell:
                    isBuy = false;
                    break;
                default:
                    isBuy = null;
                    break;
            }

            var model = new LimitOrderMassCancelModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = request.AssetPairId,
                ClientId = context.GetHttpContext().User.GetWalletId(),
                IsBuy = isBuy
            };

            MeResponseModel response = await _matchingEngineClient.MassCancelLimitOrdersAsync(model);

            if (response == null)
            {
                return new CancelOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(HftApiErrorCode.MeRuntime),
                        Message = "ME not available"
                    }
                };
            }

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return new CancelOrderResponse
                {
                    Payload = true
                };

            return new CancelOrderResponse
            {
                Error = new Error
                {
                    Code = _mapper.Map<ErrorCode>(code),
                    Message = message
                }
            };
        }

        public override async Task<CancelOrderResponse> CancelOrder(CancelOrderRequest request, ServerCallContext context)
        {
            MeResponseModel response = await _matchingEngineClient.CancelLimitOrderAsync(request.OrderId);

            if (response == null)
            {
                return new CancelOrderResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(HftApiErrorCode.MeRuntime),
                        Message = "ME not available"
                    }
                };
            }

            (HftApiErrorCode code, string message) = response.Status.ToHftApiError();

            if (code == HftApiErrorCode.Success)
                return new CancelOrderResponse
                {
                    Payload = true
                };

            return new CancelOrderResponse
            {
                Error = new Error
                {
                    Code = _mapper.Map<ErrorCode>(code),
                    Message = message
                }
            };
        }

        public override async Task<TradesResponse> GetTrades(TradesRequest request, ServerCallContext context)
        {
            var result = await _validationService.ValidateTradesRequestAsync(request.AssetPairId, request.Offset, request.Take);

            if (result != null)
            {
                return new TradesResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(result.Code),
                        Message = result.Message
                    }
                };
            }

            DateTime? from = null;
            DateTime? to = null;

            if (request.OptionalFromCase != TradesRequest.OptionalFromOneofCase.None)
            {
                from = request.From.ToDateTime();
            }

            if (request.OptionalToCase != TradesRequest.OptionalToOneofCase.None)
            {
                to = request.To.ToDateTime();
            }

            var orderAction = request.OptionalSideCase == TradesRequest.OptionalSideOneofCase.None
                ? (OrderAction?) null
                : _mapper.Map<OrderAction>(request.Side);

            if (request.Take == 0)
                request.Take = Constants.MaxPageSize;

            var trades = await _historyClient.GetTradersAsync(context.GetHttpContext().User.GetWalletId(),
                request.AssetPairId, request.Offset, request.Take, orderAction, from, to);

            var res = new TradesResponse();
            var data = _mapper.Map<List<Trade>>(trades);
            res.Payload.AddRange(data);

            return res;
        }

        public override async Task<TradesResponse> GetOrderTrades(OrderTradesRequest request, ServerCallContext context)
        {
            var trades = await _historyClient.GetOrderTradesAsync(context.GetHttpContext().User.GetWalletId(), request.OrderId);

            var res = new TradesResponse();
            var data = _mapper.Map<List<Trade>>(trades);
            res.Payload.AddRange(data);

            return res;
        }

        public override async Task GetBalanceUpdates(Empty request, IServerStreamWriter<BalanceUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New balance stream connect. peer:{context.Peer}");

            string walletId = context.GetHttpContext().User.GetWalletId();

            var balances = await _balanceService.GetBalancesAsync(walletId);

            var streamInfo = new StreamInfo<BalanceUpdate>
            {
                Stream = responseStream,
                CancelationToken = context.CancellationToken,
                Keys = new [] {context.GetHttpContext().User.GetWalletId()},
                Peer = context.Peer
            };

            var initData = new BalanceUpdate();
            initData.Balances.AddRange(_mapper.Map<List<Balance>>(balances));
            //need to await returned task for stream to be opened
            var task = await _balanceUpdateService.RegisterStreamAsync(streamInfo, new List<BalanceUpdate> { initData });
            await task;
        }

        public async Task GetOrderUpdates(Empty request, IServerStreamWriter<OrderUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New order stream connect. peer:{context.Peer}");

            var streamInfo = new StreamInfo<OrderUpdate>
            {
                Stream = responseStream,
                CancelationToken = context.CancellationToken,
                Keys = new [] {context.GetHttpContext().User.GetWalletId()},
                Peer = context.Peer
            };

            var task = await _orderUpdateService.RegisterStreamAsync(streamInfo);
            await task;
        }

        public override async Task GetTradeUpdates(Empty request, IServerStreamWriter<TradeUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New trade stream connect. peer:{context.Peer}");

            var streamInfo = new StreamInfo<TradeUpdate>
            {
                Stream = responseStream,
                CancelationToken = context.CancellationToken,
                Keys = new []{context.GetHttpContext().User.GetWalletId()},
                Peer = context.Peer
            };

            var task = await _tradeUpdateService.RegisterStreamAsync(streamInfo);
            await task;
        }

        public override async Task<GetOperationsHistoryResponse> GetOperationsHistory(GetOperationsHistoryRequest request, ServerCallContext context)
        {
            var history = await _historyClient.GetOperationsHistoryAsync(
                context.GetHttpContext().User.GetWalletId(),
                request.Offset,
                request.Take);

            var response = new GetOperationsHistoryResponse();
            response.Operations.AddRange(_mapper.Map<List<OperationHistory>>(history));
            return response;
        }

        public override async Task<CreateDepositAddressesResponse> CreateDepositAddresses(Empty request, ServerCallContext context)
        {
            try
            {
                await _siriusWalletsService.CheckDepositPreconditionsAsync(context.GetHttpContext().User.GetClientId());

                await _siriusWalletsService.CreateWalletAsync(
                    context.GetHttpContext().User.GetClientId(),
                    context.GetHttpContext().User.GetWalletId());

                return new CreateDepositAddressesResponse();
            }
            catch (HftApiException e)
            {
                return new CreateDepositAddressesResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(e.ErrorCode),
                        Message = e.Message
                    }
                };
            }
        }

        public override async Task<GetDepositAddressResponse> GetDepositAddress(GetDepositAddressRequest request, ServerCallContext context)
        {
            try
            {
                var asset = await _siriusWalletsService.CheckDepositPreconditionsAsync(context.GetHttpContext().User.GetClientId(), request.AssetId);

                var depositWallets = await _siriusWalletsService.GetWalletAddressesAsync(
                    context.GetHttpContext().User.GetClientId(),
                    context.GetHttpContext().User.GetWalletId(),
                    asset.SiriusAssetId);

                return new GetDepositAddressResponse
                {
                    Payload = _mapper.Map<DepositAddress>(depositWallets.FirstOrDefault())
                };
            }
            catch (HftApiException e)
            {
                return new GetDepositAddressResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(e.ErrorCode),
                        Message = e.Message
                    }
                };
            }
        }

        public override async Task<GetDepositAddressesResponse> GetDepositAddresses(Empty request, ServerCallContext context)
        {
            try
            {
                var depositWallets = await _siriusWalletsService.GetWalletAddressesAsync(
                    context.GetHttpContext().User.GetClientId(),
                    context.GetHttpContext().User.GetWalletId());

                var response = new GetDepositAddressesResponse();

                response.Payload.AddRange(_mapper.Map<DepositAddress[]>(depositWallets));

                return response;
            }
            catch (HftApiException e)
            {
                return new GetDepositAddressesResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(e.ErrorCode),
                        Message = e.Message
                    }
                };
            }
        }

        public override async Task<CreateWithdrawalResponse> CreateWithdrawal(CreateWithdrawalRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _siriusWalletsService.CreateWithdrawalAsync(
                    request.RequestId,
                    context.GetHttpContext().User.GetClientId(),
                    context.GetHttpContext().User.GetWalletId(),
                    request.AssetId,
                    decimal.Parse(request.Volume),
                    request.DestinationAddress,
                    request.DestinationAddressExtension);

                return new CreateWithdrawalResponse {Payload = result.ToString()};
            }
            catch (HftApiException e)
            {
                return new CreateWithdrawalResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(e.ErrorCode),
                        Message = e.Message
                    }
                };
            }
        }
    }
}
