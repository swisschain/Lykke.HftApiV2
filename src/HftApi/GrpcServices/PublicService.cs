using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using JetBrains.Annotations;
using Lykke.Exchange.Api.MarketData;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Services;

namespace HftApi.GrpcServices
{
    [UsedImplicitly]
    public class PublicService : Lykke.HftApi.ApiContract.PublicService.PublicServiceBase
    {
        private readonly IAssetsService _assetsService;
        private readonly IOrderbooksService _orderbooksService;
        private readonly MarketDataService.MarketDataServiceClient _marketDataClient;
        private readonly IStreamService<PriceUpdate> _priceStreamService;
        private readonly IStreamService<TickerUpdate> _tickerUpdateService;
        private readonly IStreamService<Orderbook> _orderbookUpdateService;
        private readonly IMapper _mapper;

        public PublicService(
            IAssetsService assetsService,
            IOrderbooksService orderbooksService,
            MarketDataService.MarketDataServiceClient marketDataClient,
            IStreamService<PriceUpdate> priceStreamService,
            IStreamService<TickerUpdate> tickerUpdateService,
            IStreamService<Orderbook> orderbookUpdateService,
            IMapper mapper
            )
        {
            _assetsService = assetsService;
            _orderbooksService = orderbooksService;
            _marketDataClient = marketDataClient;
            _priceStreamService = priceStreamService;
            _tickerUpdateService = tickerUpdateService;
            _orderbookUpdateService = orderbookUpdateService;
            _mapper = mapper;
        }

        public override async Task<AssetPairsResponse> GetAssetPairs(Empty request, ServerCallContext context)
        {
            var assetPairs = await _assetsService.GetAllAssetPairsAsync();

            var result = new AssetPairsResponse();

            result.Payload.AddRange(_mapper.Map<List<AssetPair>>(assetPairs));

            return result;
        }

        public override async Task<AssetPairResponse> GetAssetPair(AssetPairRequest request, ServerCallContext context)
        {
            var assetPair = await _assetsService.GetAssetPairByIdAsync(request.AssetPairId);

            var result = new AssetPairResponse
            {
                Payload = _mapper.Map<AssetPair>(assetPair)
            };

            return result;
        }

        public override async Task<AssetsResponse> GetAssets(Empty request, ServerCallContext context)
        {
            var assets = await _assetsService.GetAllAssetsAsync();

            var result = new AssetsResponse();

            result.Payload.AddRange(_mapper.Map<List<Asset>>(assets));

            return result;
        }

        public override async Task<AssetResponse> GetAsset(AssetRequest request, ServerCallContext context)
        {
            var asset = await _assetsService.GetAssetByIdAsync(request.AssetId);

            var result = new AssetResponse
            {
                Payload = _mapper.Map<Asset>(asset)
            };

            return result;
        }

        public override async Task<OrderbookResponse> GetOrderbooks(OrderbookRequest request, ServerCallContext context)
        {
            var orderbooks = await _orderbooksService.GetAsync(request.AssetPairId, request.Depth);

            var result = new OrderbookResponse();

            result.Payload.AddRange(_mapper.Map<List<Orderbook>>(orderbooks));

            return result;
        }

        public override async Task<TickersResponse> GetTickers(TickersRequest request, ServerCallContext context)
        {
            var marketData = await _marketDataClient.GetMarketDataAsync(new Empty());
            var items = marketData.Items.ToList();

            if (request.AssetPairIds.Any())
            {
                items = items.Where(x =>
                        request.AssetPairIds.Contains(x.AssetPairId, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }

            var result = new TickersResponse();

            result.Payload.AddRange(_mapper.Map<List<Ticker>>(items));

            return result;
        }

        public override Task GetPriceUpdates(Empty request, IServerStreamWriter<PriceUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New price stream connect. peer:{context.Peer}");

            var streamInfo = new StreamInfo<PriceUpdate>
            {
                Stream = responseStream,
                Peer = context.Peer
            };

            return _priceStreamService.RegisterStream(streamInfo);
        }

        public override Task GetTickerUpdates(Empty request, IServerStreamWriter<TickerUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New ticker stream connect. peer:{context.Peer}");

            var streamInfo = new StreamInfo<TickerUpdate>
            {
                Stream = responseStream,
                Peer = context.Peer
            };

            return _tickerUpdateService.RegisterStream(streamInfo);
        }

        public override Task GetOrderbookUpdates(OrderbookUpdatesRequest request,
            IServerStreamWriter<Orderbook> responseStream,
            ServerCallContext context)
        {
            Console.WriteLine($"New orderbook stream connect. peer:{context.Peer}");

            var streamInfo = new StreamInfo<Orderbook>
            {
                Stream = responseStream,
                Key = request.AssetPairId,
                Peer = context.Peer
            };

            return _orderbookUpdateService.RegisterStream(streamInfo);
        }
    }
}
