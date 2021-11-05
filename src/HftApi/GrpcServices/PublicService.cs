﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HftApi.Common.Domain.MyNoSqlEntities;
using JetBrains.Annotations;
using Lykke.Exchange.Api.MarketData;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.Service.TradesAdapter.Client;
using MyNoSqlServer.Abstractions;

namespace HftApi.GrpcServices
{
    [UsedImplicitly]
    public class PublicService : Lykke.HftApi.ApiContract.PublicService.PublicServiceBase
    {
        private readonly IAssetsService _assetsService;
        private readonly IOrderbooksService _orderbooksService;
        private readonly MarketDataService.MarketDataServiceClient _marketDataClient;
        private readonly ITradesAdapterClient _tradesAdapterClient;
        private readonly PricesStreamService _priceStreamService;
        private readonly TickersStreamService _tickerUpdateService;
        private readonly OrderbookStreamService _orderbookUpdateService;
        private readonly PublicTradesStreamService _publicTradesStreamService;
        private readonly ValidationService _validationService;
        private readonly IMyNoSqlServerDataReader<TickerEntity> _tickersReader;
        private readonly IMyNoSqlServerDataReader<PriceEntity> _pricesReader;
        private readonly IMapper _mapper;

        public PublicService(
            IAssetsService assetsService,
            IOrderbooksService orderbooksService,
            MarketDataService.MarketDataServiceClient marketDataClient,
            ITradesAdapterClient tradesAdapterClient,
            PricesStreamService priceStreamService,
            TickersStreamService tickerUpdateService,
            OrderbookStreamService orderbookUpdateService,
            PublicTradesStreamService publicTradesStreamService,
            ValidationService validationService,
            IMyNoSqlServerDataReader<TickerEntity> tickersReader,
            IMyNoSqlServerDataReader<PriceEntity> pricesReader,
            IMapper mapper
            )
        {
            _assetsService = assetsService;
            _orderbooksService = orderbooksService;
            _marketDataClient = marketDataClient;
            _tradesAdapterClient = tradesAdapterClient;
            _priceStreamService = priceStreamService;
            _tickerUpdateService = tickerUpdateService;
            _orderbookUpdateService = orderbookUpdateService;
            _publicTradesStreamService = publicTradesStreamService;
            _validationService = validationService;
            _tickersReader = tickersReader;
            _pricesReader = pricesReader;
            _mapper = mapper;
        }

        public override async Task<PublicTradeUpdate> GetPublicTrades(PublicTradesRequest request, ServerCallContext context)
        {
            var data = await _tradesAdapterClient.GetTradesByAssetPairIdAsync(request.AssetPairId, request.Offset, request.Take);

            var result = new PublicTradeUpdate();
            result.Trades.AddRange(_mapper.Map<List<PublicTrade>>(data.Records));
            return result;
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
            if (request.AssetPairId == "givemeerror")
                throw new ArgumentException("Exception for this asset pair");

            var validationResult = await _validationService.ValidateAssetPairAsync(request.AssetPairId);

            if (validationResult != null)
            {
                return new AssetPairResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(validationResult.Code),
                        Message = validationResult.Message
                    }
                };
            }

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
            var validationResult = await _validationService.ValidateAssetAsync(request.AssetId);

            if (validationResult != null)
            {
                return new AssetResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(validationResult.Code),
                        Message = validationResult.Message
                    }
                };
            }

            var asset = await _assetsService.GetAssetByIdAsync(request.AssetId);

            var result = new AssetResponse
            {
                Payload = _mapper.Map<Asset>(asset)
            };

            return result;
        }

        public override async Task<OrderbookResponse> GetOrderbooks(OrderbookRequest request, ServerCallContext context)
        {
            var assetPairResult = await _validationService.ValidateAssetPairAsync(request.AssetPairId);

            if (assetPairResult != null)
            {
                return new OrderbookResponse
                {
                    Error = new Error
                    {
                        Code = _mapper.Map<ErrorCode>(assetPairResult.Code),
                        Message = assetPairResult.Message
                    }
                };
            }

            var orderbooks = await _orderbooksService.GetAsync(new[] { request.AssetPairId }, request.Depth);

            var result = new OrderbookResponse();

            foreach (var orderbook in orderbooks)
            {
                var resOrderBook = _mapper.Map<Orderbook>(orderbook);
                result.Payload.Add(resOrderBook);
            }

            return result;
        }

        public override async Task<TickersResponse> GetTickers(TickersRequest request, ServerCallContext context)
        {
            var entities = _tickersReader.Get(TickerEntity.GetPk());

            List<TickerUpdate> result;

            if (entities.Any())
            {
                result = _mapper.Map<List<TickerUpdate>>(entities);
            }
            else
            {
                var marketData = await _marketDataClient.GetMarketDataAsync(new Empty());
                result = _mapper.Map<List<TickerUpdate>>(marketData.Items.ToList());
            }


            if (request.AssetPairIds.Any())
            {
                result = result.Where(x =>
                        request.AssetPairIds.Contains(x.AssetPairId, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }

            var response = new TickersResponse();

            response.Payload.AddRange(result);

            return response;
        }

        public override async Task<PricesResponse> GetPrices(PricesRequest request, ServerCallContext context)
        {
            var entities = _pricesReader.Get(PriceEntity.GetPk());

            List<PriceUpdate> result;

            if (entities.Any())
            {
                result = _mapper.Map<List<PriceUpdate>>(entities);
            }
            else
            {
                var marketData = await _marketDataClient.GetMarketDataAsync(new Empty());
                result = _mapper.Map<List<PriceUpdate>>(marketData.Items.ToList());
            }

            if (request.AssetPairIds.Any())
            {
                result = result.Where(x =>
                        request.AssetPairIds.Contains(x.AssetPairId, StringComparer.InvariantCultureIgnoreCase))
                    .ToList();
            }

            var response = new PricesResponse();

            response.Payload.AddRange(result);

            return response;
        }

        public override async Task GetPriceUpdates(PriceUpdatesRequest request, IServerStreamWriter<PriceUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New price stream connect. peer:{context.Peer}");

            var entities = _pricesReader.Get(PriceEntity.GetPk());

            var prices = _mapper.Map<List<PriceUpdate>>(entities);

            if (request.AssetPairIds.Any())
                prices = prices.Where(x => request.AssetPairIds.Contains(x.AssetPairId)).ToList();

            var streamInfo = new StreamInfo<PriceUpdate>
            {
                Stream = responseStream,
                CancelationToken = context.CancellationToken,
                Peer = context.Peer,
                Keys = request.AssetPairIds.ToArray()
            };

            var task = await _priceStreamService.RegisterStreamAsync(streamInfo, prices);
            await task;
        }

        public override async Task GetTickerUpdates(Empty request, IServerStreamWriter<TickerUpdate> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"New ticker stream connect. peer:{context.Peer}");

            var streamInfo = new StreamInfo<TickerUpdate>
            {
                Stream = responseStream,
                CancelationToken = context.CancellationToken,
                Peer = context.Peer
            };

            var task = await _tickerUpdateService.RegisterStreamAsync(streamInfo);
            await task;
        }

        public override async Task GetOrderbookUpdates(OrderbookUpdatesRequest request,
            IServerStreamWriter<Orderbook> responseStream,
            ServerCallContext context)
        {
            Console.WriteLine($"New orderbook stream connect. peer:{context.Peer}");
            var assetPairIds = new List<string>();

            if (!string.IsNullOrEmpty(request.AssetPairId))
            {
                assetPairIds.Add(request.AssetPairId);
            }
            else
            {
                assetPairIds = request.AssetPairIds?.Distinct().ToList() ?? new List<string>();
            }

            var data = await _orderbooksService.GetAsync(assetPairIds);

            var orderbooks = new List<Orderbook>();

            foreach (var item in data)
            {
                var orderbook = _mapper.Map<Orderbook>(item);
                orderbooks.Add(orderbook);
            }

            var streamInfo = new StreamInfo<Orderbook>
            {
                Stream = responseStream,
                CancelationToken = context.CancellationToken,
                Keys = assetPairIds.ToArray(),
                Peer = context.Peer
            };

            var task = await _orderbookUpdateService.RegisterStreamAsync(streamInfo, orderbooks);
            await task;
        }

        public override async Task GetPublicTradeUpdates(PublicTradesUpdatesRequest request,
            IServerStreamWriter<PublicTradeUpdate> responseStream,
            ServerCallContext context)
        {
            Console.WriteLine($"New public trades stream connect. peer:{context.Peer}");

            var data = await _tradesAdapterClient.GetTradesByAssetPairIdAsync(request.AssetPairId, 0, 50);

            var trades = _mapper.Map<List<PublicTrade>>(data.Records);

            var initData = new PublicTradeUpdate();
            initData.Trades.AddRange(trades);

            var streamInfo = new StreamInfo<PublicTradeUpdate>
            {
                Stream = responseStream,
                CancelationToken = context.CancellationToken,
                Keys = new [] {request.AssetPairId},
                Peer = context.Peer
            };

            var task = await _publicTradesStreamService.RegisterStreamAsync(streamInfo, new List<PublicTradeUpdate>{initData});
            await task;
        }
    }
}
