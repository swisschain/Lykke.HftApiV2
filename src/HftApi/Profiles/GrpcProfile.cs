using System;
using System.Globalization;
using System.Linq;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using HftApi.Common.Domain.MyNoSqlEntities;
using JetBrains.Annotations;
using Lykke.Exchange.Api.MarketData;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities;
using Lykke.MatchingEngine.Connector.Models.Common;
using Asset = Lykke.HftApi.Domain.Entities.Asset;
using AssetPair = Lykke.HftApi.Domain.Entities.AssetPair;
using Balance = Lykke.HftApi.Domain.Entities.Balance;
using CancelMode = Lykke.MatchingEngine.Connector.Models.Api.CancelMode;
using Order = Lykke.HftApi.Domain.Entities.Order;
using Orderbook = Lykke.HftApi.Domain.Entities.Orderbook;
using Trade = Lykke.HftApi.Domain.Entities.Trade;
using TradeFee = Lykke.HftApi.Domain.Entities.TradeFee;

namespace HftApi.Profiles
{
    [UsedImplicitly]
    public class GrpcProfile : Profile
    {
        public GrpcProfile()
        {
            CreateMap<DateTime, string>().ConvertUsing(dt => dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            CreateMap<DateTime?, string>().ConvertUsing(dt => dt.HasValue ? dt.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : string.Empty);
            CreateMap<DateTime, Timestamp>().ConvertUsing((dt, timestamp) => Timestamp.FromDateTime(dt.ToUniversalTime()));
            CreateMap<Timestamp, DateTime>().ConvertUsing((dt, timestamp) => dt.ToDateTime());
            CreateMap<decimal, string>().ConvertUsing(d => d.ToString(CultureInfo.InvariantCulture));

            CreateMap<Balance, Lykke.HftApi.ApiContract.Balance>(MemberList.Destination);

            CreateMap<BalanceEntity, Balance>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt))
                .ForMember(d => d.Available, o => o.MapFrom(x => x.Balance));

            CreateMap<AssetPair, Lykke.HftApi.ApiContract.AssetPair>(MemberList.Destination);

            CreateMap<Asset, Lykke.HftApi.ApiContract.Asset>(MemberList.Destination);

            CreateMap<Orderbook, Lykke.HftApi.ApiContract.Orderbook>(MemberList.Destination);

            CreateMap<VolumePrice, Lykke.HftApi.ApiContract.Orderbook.Types.PriceVolume>(MemberList.Destination)
                .ForMember(d => d.V, o => o.MapFrom(x => x.Volume))
                .ForMember(d => d.P, o => o.MapFrom(x => x.Price));

            CreateMap<MarketSlice, TickerUpdate>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => DateTime.UtcNow));

            CreateMap<MarketSlice, PriceUpdate>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => DateTime.UtcNow));

            CreateMap<Order, Lykke.HftApi.ApiContract.Order>(MemberList.Destination)
                .ForMember(d => d.Volume, o => o.MapFrom(x => Math.Abs(x.Volume)))
                .ForMember(d => d.RemainingVolume, o => o.MapFrom(x => Math.Abs(x.RemainingVolume)))
                .ForMember(d => d.FilledVolume, o => o.MapFrom(x => x.Volume - x.RemainingVolume))
                .ForMember(d => d.FilledVolume, o => o.MapFrom(x => x.Volume - x.RemainingVolume))
                .ForMember(d => d.Cost, o => o.MapFrom(x => x.FilledVolume * x.Price));

            CreateMap<Trade, Lykke.HftApi.ApiContract.Trade>(MemberList.Destination);

            CreateMap<TradeFee, Lykke.HftApi.ApiContract.TradeFee>(MemberList.Destination);

            CreateMap<TickerEntity, TickerUpdate>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.UpdatedDt));

            CreateMap<PriceEntity, PriceUpdate>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.UpdatedDt));

            CreateMap<OrderbookEntity, Lykke.HftApi.ApiContract.Orderbook>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt));

            CreateMap<OrderbookEntity, Orderbook>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt));

            CreateMap<Lykke.HftApi.ApiContract.Orderbook, Orderbook>(MemberList.Destination)
                .ForMember(d => d.Asks, o => o.MapFrom(x => x.Asks.ToList()))
                .ForMember(d => d.Bids, o => o.MapFrom(x => x.Bids.ToList()));

            CreateMap<Lykke.HftApi.ApiContract.Orderbook.Types.PriceVolume, VolumePrice>(MemberList.Destination)
                .ForMember(d => d.Volume, o => o.MapFrom(x => x.V))
                .ForMember(d => d.Price, o => o.MapFrom(x => x.P));

            CreateMap<VolumePriceEntity, VolumePrice>(MemberList.Destination);

            CreateMap<VolumePriceEntity, Lykke.HftApi.ApiContract.Orderbook.Types.PriceVolume>(MemberList.Destination)
                .ForMember(d => d.V, o => o.MapFrom(x => x.Volume))
                .ForMember(d => d.P, o => o.MapFrom(x => x.Price));

            CreateMap<BalanceEntity, Lykke.HftApi.ApiContract.Balance>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt))
                .ForMember(d => d.Available, o => o.MapFrom(x => x.Balance))
                .ForMember(d => d.Reserved, o => o.MapFrom(x => x.Reserved));

            CreateMap<OrderEntity, Lykke.HftApi.ApiContract.Order>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt))
                .ForMember(d => d.Volume, o => o.MapFrom(x => Math.Abs(x.Volume)))
                .ForMember(d => d.RemainingVolume, o => o.MapFrom(x => Math.Abs(x.RemainingVolume)))
                .ForMember(d => d.FilledVolume, o => o.MapFrom(x => x.Volume - x.RemainingVolume))
                .ForMember(d => d.Cost, o => o.MapFrom(x => x.FilledVolume * x.Price));

            CreateMap<TradeEntity, Lykke.HftApi.ApiContract.Trade>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt));

            CreateMap<CancelMode, Lykke.HftApi.ApiContract.CancelMode>();
            CreateMap<OrderAction, Side>();
            CreateMap<HftApiErrorCode, ErrorCode>();

            CreateMap<Lykke.MatchingEngine.Connector.Models.Events.Order, OrderEntity>(MemberList.Destination)
                .ForMember(d => d.PartitionKey, o => o.MapFrom(x => x.WalletId))
                .ForMember(d => d.RowKey, o => o.MapFrom(x => x.ExternalId))
                .ForMember(d => d.TimeStamp, o => o.Ignore())
                .ForMember(d => d.Expires, o => o.Ignore())
                .ForMember(d => d.Id, o => o.MapFrom(x => x.ExternalId))
                .ForMember(d => d.LastTradeTimestamp, o => o.MapFrom(x => x.LastMatchTime))
                .ForMember(d => d.Volume, o => o.MapFrom(x => Math.Abs(Convert.ToDecimal(x.Volume))))
                .ForMember(d => d.RemainingVolume, o => o.MapFrom(x => Math.Abs(Convert.ToDecimal(x.RemainingVolume))))
                .ForMember(d => d.Type, o => o.MapFrom(x =>  x.OrderType.ToString()));

            CreateMap<Lykke.MatchingEngine.Connector.Models.Events.Trade, Trade>(MemberList.Destination)
                .ForMember(d => d.Id, o => o.MapFrom(x => x.TradeId))
                .ForMember(d => d.WalletId, o => o.Ignore()) //fill manually
                .ForMember(d => d.AssetPairId, o => o.Ignore()) //fill manually
                .ForMember(d => d.OrderId, o => o.Ignore()) //fill manually
                .ForMember(d => d.OppositeOrderId, o => o.MapFrom(x => x.OppositeExternalOrderId))
                .ForMember(d => d.QuoteVolume, o => o.MapFrom(x => x.QuotingVolume))
                .ForMember(d => d.QuoteAssetId, o => o.MapFrom(x => x.QuotingAssetId))
                .ForMember(d => d.Fee, o => o.Ignore());

            CreateMap<Lykke.MatchingEngine.Connector.Models.Events.Trade, TradeEntity>(MemberList.Destination)
                .ForMember(d => d.PartitionKey, o => o.Ignore())//fill manually
                .ForMember(d => d.RowKey, o => o.MapFrom(x => x.TradeId))
                .ForMember(d => d.Id, o => o.MapFrom(x => x.TradeId))
                .ForMember(d => d.TimeStamp, o => o.Ignore())
                .ForMember(d => d.Expires, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.MapFrom(x => x.Timestamp))
                .ForMember(d => d.AssetPairId, o => o.Ignore()) //fill manually
                .ForMember(d => d.OrderId, o => o.Ignore())
                .ForMember(d => d.WalletId, o => o.Ignore())
                .ForMember(d => d.QuoteVolume, o => o.MapFrom(x => x.QuotingVolume))
                .ForMember(d => d.QuoteAssetId, o => o.MapFrom(x => x.QuotingAssetId))
                .ForMember(d => d.Fee, o => o.Ignore());
        }
    }
}
