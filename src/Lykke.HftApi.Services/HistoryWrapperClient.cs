using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Antares.Service.History.GrpcClient;
using Antares.Service.History.GrpcContract.Common;
using Antares.Service.History.GrpcContract.Orders;
using Antares.Service.History.GrpcContract.Trades;
using Google.Protobuf.WellKnownTypes;
using Lykke.HftApi.Domain.Entities;
using Lykke.MatchingEngine.Connector.Models.Common;

namespace Lykke.HftApi.Services
{
    public class HistoryWrapperClient
    {
        private readonly IHistoryGrpcClient _historyGrpcClient;

        public HistoryWrapperClient(IHistoryGrpcClient historyGrpcClient)
        {
            _historyGrpcClient = historyGrpcClient;
        }

        public async Task<IReadOnlyCollection<Order>> GetOrdersByWalletAsync(
            string walletId,
            string assetPairId = null,
            OrderStatus[] status = null,
            OrderType[] type = null,
            bool withTrades = false,
            int? offset = 0,
            int? limit = 100
            )
        {
            var orderStatus = status == null ? Array.Empty<Antares.Service.History.GrpcContract.Common.OrderStatus>() :
                status;

            var orderType = type == null ? Array.Empty<Antares.Service.History.GrpcContract.Orders.OrderType>() :
                type;

            var responseGrpc = await _historyGrpcClient.Orders.GetOrderListAsync(new GetOrderListRequest()
            {
                WalletId = walletId,
                AssetPairId = assetPairId,
                Pagination = new PaginationInt32()
                {
                    Limit = limit ?? 100,
                    Offset = offset ?? 0
                },
                Status = { orderStatus },
                Type = { orderType }
            });

            var orders = responseGrpc.Items;
            var tradeTasks = new List<Task<IReadOnlyCollection<Trade>>>();

            if (withTrades)
            {
                tradeTasks.AddRange(orders.Select(x => GetOrderTradesAsync(x.WalletId.ToString(), x.Id.ToString())));
                await Task.WhenAll(tradeTasks);
            }

            var trades = tradeTasks.SelectMany(x => x.Result).ToList();

            var result = new List<Order>();

            foreach (var order in orders)
            {
                var orderTrades = trades.Where(x => x.OrderId == order.Id.ToString()).ToList();
                result.Add(order.ToDomain(orderTrades));
            }

            return result;
        }

        public async Task<IReadOnlyCollection<Trade>> GetTradersAsync(
            string walletId,
            string assetPairId = null,
            int? offset = 0,
            int? limit = 100,
            OrderAction? side = null,
            DateTime? from = null,
            DateTime? to = null
            )
        {
            var trades = await _historyGrpcClient.Trades.GetTradesAsync(new GetTradesRequest()
            {
                AssetPairId = assetPairId,
                From = from != null ? Timestamp.FromDateTime(from.Value) : null,
                Pagination = new PaginationInt32()
                {
                    Limit = limit ?? 100,
                    Offset = offset ?? 0
                },
                To = to != null ? Timestamp.FromDateTime(to.Value) : null,
                TradeType = side switch {
                    OrderAction.Buy => TradeType.Buy,
                    OrderAction.Sell => TradeType.Sell,
                    null => TradeType.None,
                    _ => throw new ArgumentOutOfRangeException(nameof(side), side, null)
                },
                WalletId = walletId
            });

            return trades.Items.Select(x => x.TradeToDomain()).ToList();
        }

        public async Task<IReadOnlyCollection<Trade>> GetOrderTradesAsync(string walletId, string orderId = null)
        {
            var orderTrades = await _historyGrpcClient.Trades.GetTradesByOrderIdAsync(new GetTradesByOrderIdRequest()
            {
                Id = orderId,
                WalletId = walletId
            });

            return orderTrades.Items.Select(x => x.TradeToDomain()).ToList();
        }

        public async Task<Order> GetOrderAsync(string orderId)
        {
            //GetOrderById
            var order = await _historyGrpcClient.Orders.GetOrderAsync(new GetOrderRequest()
            {
                Id = orderId
            });

            return order.Item.ToDomain();
        }
    }

    public static class Converter
    {
        public static Trade TradeToDomain(this HistoryResponseItem trade)
        {
            return new Trade
            {
                Id = trade.Id.ToString(),
                Timestamp = trade.Timestamp.ToDateTime(),
                AssetPairId = trade.Trade.AssetPairId,
                OrderId = trade.Trade.OrderId,
                Role = trade.Trade.Role.ToString(),
                Price = trade.Trade.Price,
                BaseVolume = trade.Trade.BaseVolume,
                QuoteVolume = trade.Trade.QuotingVolume,
                BaseAssetId = trade.Trade.BaseAssetId,
                QuoteAssetId = trade.Trade.QuotingAssetId,
                Fee = trade.Trade.FeeSize != null && (!string.IsNullOrEmpty(trade.Trade.FeeAssetId))
                    ? new TradeFee
                    {
                        Size = trade.Trade.FeeSize,
                        AssetId = trade.Trade.FeeAssetId
                    }
                    : null
            };
        }

        public static Order ToDomain(this Antares.Service.History.GrpcContract.Orders.OrderModel order, IReadOnlyCollection<Trade> trades = null)
        {
            return new Order
            {
                Id = order.Id.ToString(),
                Timestamp = order.CreateDt.ToDateTime(),
                LastTradeTimestamp = order.MatchDt?.ToDateTime(),
                Status = order.Status.ToString(),
                AssetPairId = order.AssetPairId,
                Type = order.Type.ToString(),
                Side = order.Side.ToString(),
                Price = order.Price ?? 0,
                Volume = Math.Abs(order.Volume),
                RemainingVolume = Math.Abs(order.RemainingVolume),
                Trades = trades ?? Array.Empty<Trade>()
            };
        }
    }
}
