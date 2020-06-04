using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;
using Lykke.MatchingEngine.Connector.Models.Common;
using Lykke.Service.History.Contracts.Enums;
using Lykke.Service.History.Contracts.History;
using Lykke.Service.History.Contracts.Orders;
using Newtonsoft.Json;

namespace Lykke.HftApi.Services
{
    public class HistoryHttpClient
    {
        private readonly HttpClient _client;

        public HistoryHttpClient(HttpClient client)
        {
            _client = client;
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
            var queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("walletId", walletId),
                new KeyValuePair<string, string>("offset", offset.ToString()),
                new KeyValuePair<string, string>("limit", limit.ToString())
            };

            if (!string.IsNullOrEmpty(assetPairId))
                queryParams.Add(new KeyValuePair<string, string>("assetPairId", assetPairId));

            if (status != null)
                queryParams.AddRange(status.Select(x => new KeyValuePair<string, string>("status", x.ToString())));

            if (type != null)
                queryParams.AddRange(type.Select(x => new KeyValuePair<string, string>("type", x.ToString())));

            string url = await GetUrlWithQueryStringAsync("/api/orders/list", queryParams);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<IReadOnlyCollection<OrderModel>>(responseString);

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
            var queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("walletId", walletId),
                new KeyValuePair<string, string>("offset", offset.ToString()),
                new KeyValuePair<string, string>("limit", limit.ToString())
            };

            if (!string.IsNullOrEmpty(assetPairId))
                queryParams.Add(new KeyValuePair<string, string>("assetPairId", assetPairId));

            if (side != null)
                queryParams.Add(new KeyValuePair<string, string>("tradeType", side.ToString()));

            if (from != null)
                queryParams.Add(new KeyValuePair<string, string>("from", from.Value.ToString(CultureInfo.InvariantCulture)));

            if (to != null)
                queryParams.Add(new KeyValuePair<string, string>("to", to.Value.ToString(CultureInfo.InvariantCulture)));

            var url = await GetUrlWithQueryStringAsync("/api/trades", queryParams);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var trades = JsonConvert.DeserializeObject<IReadOnlyCollection<TradeModel>>(responseString);

            return trades.Select(x => x.ToDomain()).ToList();
        }

        public async Task<IReadOnlyCollection<Trade>> GetOrderTradesAsync(string walletId, string orderId = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/trades/order/{walletId}/{orderId}");
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var trades = JsonConvert.DeserializeObject<IReadOnlyCollection<TradeModel>>(responseString);

            return trades.Select(x => x.ToDomain()).ToList();
        }

        private async Task<string> GetUrlWithQueryStringAsync(string url, List<KeyValuePair<string, string>> parameters)
        {
            using var content = new FormUrlEncodedContent(parameters);
            var queryParams = await content.ReadAsStringAsync();
            return $"{url}?{queryParams}";
        }
    }

    public static class Converter
    {
        public static Trade ToDomain(this TradeModel trade)
        {
            return new Trade
            {
                Id = trade.Id.ToString(),
                Index = trade.Index,
                Timestamp = trade.Timestamp,
                AssetPairId = trade.AssetPairId,
                OrderId = trade.OrderId.ToString(),
                Role = trade.Role.ToString(),
                Price = trade.Price,
                BaseVolume = Math.Abs(trade.BaseVolume),
                QuoteVolume = Math.Abs(trade.QuotingVolume),
                BaseAssetId = trade.BaseAssetId,
                QuoteAssetId = trade.QuotingAssetId,
                Fee = trade.FeeSize != null && (!string.IsNullOrEmpty(trade.FeeAssetId))
                    ? new TradeFee
                    {
                        Size = trade.FeeSize.Value,
                        AssetId = trade.FeeAssetId
                    }
                    : null
            };
        }

        public static Order ToDomain(this OrderModel order, IReadOnlyCollection<Trade> trades = null)
        {
            return new Order
            {
                Id = order.Id.ToString(),
                Timestamp = order.CreateDt,
                LastTradeTimestamp = order.MatchDt,
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
