using System;
using System.Collections.Generic;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using MyNoSqlServer.Abstractions;
using Xunit;

namespace HftApiTests
{
    public class OrderbookTests
    {
        private readonly OrderbooksService _service;

        public OrderbookTests()
        {
            var redisMock = Mock.Of<IDistributedCache>();
            var assetsMock = Mock.Of<IAssetsService>();
            var readerMock = Mock.Of<IMyNoSqlServerDataReader<OrderbookEntity>>();
            var mapperMock = Mock.Of<IMapper>();
            _service = new OrderbooksService(redisMock, assetsMock, readerMock, string.Empty, mapperMock);
        }

        [Fact]
        public void Test_OrderbookUpdates()
        {
            var oldOrderbook = new Orderbook
            {
                AssetPairId = "ETHUSD",
                Timestamp = DateTime.UtcNow,
                Asks = new List<VolumePrice>
                {
                    new VolumePrice(-0.44286435m, 152.65475m),
                    new VolumePrice(-0.01574865m, 153.16139m),
                    new VolumePrice(-0.04724595m, 154.30132m),
                },
                Bids = new List<VolumePrice>
                {
                    new VolumePrice(0.1m, 50.0m),
                    new VolumePrice(0.101596m, 49.49336m),
                    new VolumePrice(0.304788m, 48.35343m),
                }
            };

            var newOrderbook = new Orderbook
            {
                AssetPairId = "ETHUSD",
                Timestamp = DateTime.UtcNow,
                Asks = new List<VolumePrice>
                {
                    new VolumePrice(-0.44286435m, 152.65475m),
                    new VolumePrice(-0.01574865m, 153.16139m),
                    new VolumePrice(-0.05m, 151.30132m),
                },
                Bids = new List<VolumePrice>
                {
                    new VolumePrice(0.04m, 50.0m),
                    new VolumePrice(0.2m, 49.12345m),
                    new VolumePrice(0.304788m, 48.35343m),
                }
            };

            var exprectedOrderbook = new Orderbook
            {
                AssetPairId = "ETHUSD",
                Timestamp = DateTime.UtcNow,
                Asks = new List<VolumePrice>
                {
                    new VolumePrice(-0.05m, 151.30132m),
                    new VolumePrice(0, 154.30132m)
                },
                Bids = new List<VolumePrice>
                {
                    new VolumePrice(0.04m, 50.0m),
                    new VolumePrice(0, 49.49336m),
                    new VolumePrice(0.2m, 49.12345m)
                }
            };

            var update = _service.GetOrderbookUpdates(oldOrderbook, newOrderbook);

            Assert.Equal(2, update.Asks.Count);
            Assert.Equal(3, update.Bids.Count);

            Assert.Equal(-0.05m, update.Asks[0].Volume);
            Assert.Equal(151.30132m, update.Asks[0].Price);

            Assert.Equal(0, update.Asks[1].Volume);
            Assert.Equal(154.30132m, update.Asks[1].Price);

            Assert.Equal(0.04m, update.Bids[0].Volume);
            Assert.Equal(50.0m, update.Bids[0].Price);

            Assert.Equal(0, update.Bids[1].Volume);
            Assert.Equal(49.49336m, update.Bids[1].Price);

            Assert.Equal(0.2m, update.Bids[2].Volume);
            Assert.Equal(49.12345m, update.Bids[2].Price);
        }

        [Fact]
        public void Test_OrderbookUpdates_SamePrice()
        {
            var oldOrderbook = new Orderbook
            {
                AssetPairId = "LKK1YLKK",
                Timestamp = DateTime.UtcNow,
                Asks = new List<VolumePrice>(),
                Bids = new List<VolumePrice>
                {
                    new VolumePrice(50m, 0.962m)
                }
            };

            var newOrderbook = new Orderbook
            {
                AssetPairId = "LKK1YLKK",
                Timestamp = DateTime.UtcNow,
                Asks = new List<VolumePrice>(),
                Bids = new List<VolumePrice>
                {
                    new VolumePrice(50m, 0.962m),
                    new VolumePrice(50m, 0.962m)
                }
            };

            var exprectedOrderbook = new Orderbook
            {
                AssetPairId = "LKK1YLKK",
                Timestamp = DateTime.UtcNow,
                Asks = new List<VolumePrice>(),
                Bids = new List<VolumePrice>
                {
                    new VolumePrice(100m, 0.962m)
                }
            };

            var update = _service.GetOrderbookUpdates(oldOrderbook, newOrderbook);

            Assert.Equal(exprectedOrderbook.Asks.Count, update.Asks.Count);
            Assert.Equal(exprectedOrderbook.Bids.Count, update.Bids.Count);

            Assert.Equal(exprectedOrderbook.Bids[0].Volume, update.Bids[0].Volume);
            Assert.Equal(exprectedOrderbook.Bids[0].Price, update.Bids[0].Price);
        }
    }
}
