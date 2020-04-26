using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Lykke.HftApi.Services
{
    public class OrderbooksService : IOrderbooksService
    {
        private readonly IDistributedCache _redisCache;
        private readonly IAssetsService _assetsService;
        private readonly string _orderBooksCacheKeyPattern;

        public OrderbooksService(
            IDistributedCache redisCache,
            IAssetsService assetsService,
            string orderBooksCacheKeyPattern
            )
        {
            _redisCache = redisCache;
            _assetsService = assetsService;
            _orderBooksCacheKeyPattern = orderBooksCacheKeyPattern;
        }

        public async Task<IReadOnlyCollection<Orderbook>> GetAsync(string assetPairId = null, int? depth = null)
        {
            var orderbooks = new List<Orderbook>();

            if (assetPairId == null)
            {
                var assetPairs = await _assetsService.GetAllAssetPairsAsync();

                var results = await Task.WhenAll(assetPairs.Select(pair => GetOrderbookAsync(pair.AssetPairId)));

                orderbooks = results.ToList();
            }
            else
            {
                var orderbook = await GetOrderbookAsync(assetPairId);
                orderbooks.Add(orderbook);
            }

            if (depth.HasValue)
            {
                foreach (var orderbook in orderbooks)
                {
                    if (orderbook.Bids.Any())
                        orderbook.Bids = orderbook.Bids.Take(depth.Value).ToList();

                    if (orderbook.Asks.Any())
                        orderbook.Asks = orderbook.Asks.Take(depth.Value).ToList();
                }
            }

            return orderbooks;
        }

        private async Task<Orderbook> GetOrderbookAsync(string assetPairId)
        {
            var buyBook = GetOrderbook(assetPairId, true);
            var sellBook = GetOrderbook(assetPairId, false);

            await Task.WhenAll(buyBook, sellBook);

            return new Orderbook
            {
                AssetPair = assetPairId,
                Timestamp = buyBook.Result.Timestamp > sellBook.Result.Timestamp
                    ? buyBook.Result.Timestamp
                    : sellBook.Result.Timestamp,
                Bids = buyBook.Result.Prices.Select(x => new VolumePrice(x.Volume, x.Price)).ToList(),
                Asks = sellBook.Result.Prices.Select(x => new VolumePrice(x.Volume, x.Price)).ToList()
            };
        }

        private async Task<OrderbookModel> GetOrderbook(string assetPair, bool buy)
        {
            var orderBook = await _redisCache.GetStringAsync(GetKeyForOrderBook(assetPair, buy));
            return orderBook != null
                ? JsonConvert.DeserializeObject<OrderbookModel>(orderBook)
                : new OrderbookModel { AssetPair = assetPair, Timestamp = DateTime.UtcNow };
        }

        private string GetKeyForOrderBook(string assetPairId, bool isBuy)
        {
            return string.Format(_orderBooksCacheKeyPattern, assetPairId, isBuy);
        }

        private class OrderbookModel
        {
            public string AssetPair { get; set; }
            public bool IsBuy { get; set; }
            public DateTime Timestamp { get; set; }
            public IReadOnlyCollection<VolumePriceModel> Prices { get; set; } = Array.Empty<VolumePriceModel>();
        }

        private class VolumePriceModel
        {
            public decimal Volume { get; set; }
            public decimal Price { get; set; }
        }
    }
}
