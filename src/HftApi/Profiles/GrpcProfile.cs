using System.Globalization;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;
using Lykke.Exchange.Api.MarketData;

namespace HftApi.Profiles
{
    [UsedImplicitly]
    public class GrpcProfile : Profile
    {
        public GrpcProfile()
        {
            CreateMap<Lykke.HftApi.Domain.Entities.Balance, Lykke.HftApi.ApiContract.Balance>(MemberList.Destination)
                .ForMember(d => d.Available, o => o.MapFrom(x => x.Available.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.Reserved, o => o.MapFrom(x => x.Reserved.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.Timestamp.ToUniversalTime())));

            CreateMap<Lykke.HftApi.Domain.Entities.AssetPair, Lykke.HftApi.ApiContract.AssetPair>(MemberList.Destination)
                .ForMember(d => d.MinVolume, o => o.MapFrom(x => x.MinVolume.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.MinOppositeVolume, o => o.MapFrom(x => x.MinOppositeVolume.ToString(CultureInfo.InvariantCulture)));

            CreateMap<Lykke.HftApi.Domain.Entities.Asset, Lykke.HftApi.ApiContract.Asset>(MemberList.Destination);

            CreateMap<Lykke.HftApi.Domain.Entities.Orderbook, Lykke.HftApi.ApiContract.Orderbook>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.Timestamp.ToUniversalTime())));

            CreateMap<Lykke.HftApi.Domain.Entities.VolumePrice, Lykke.HftApi.ApiContract.Orderbook.Types.PriceVolume>(MemberList.Destination)
                .ForMember(d => d.V, o => o.MapFrom(x => x.Volume.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.P, o => o.MapFrom(x => x.Price.ToString(CultureInfo.InvariantCulture)));

            CreateMap<MarketSlice, Lykke.HftApi.ApiContract.Ticker>(MemberList.Destination);

            CreateMap<Lykke.HftApi.Domain.Entities.Order, Lykke.HftApi.ApiContract.Order>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.Timestamp.ToUniversalTime())))
                .ForMember(d => d.LastTradeTimestamp, o => o.MapFrom(x => x.LastTradeTimestamp.HasValue ? Timestamp.FromDateTime(x.LastTradeTimestamp.Value.ToUniversalTime()) : null))
                .ForMember(d => d.FilledVolume, o => o.MapFrom(x => x.Volume - x.RemainingVolume))
                .ForMember(d => d.Cost, o => o.MapFrom(x => x.FilledVolume * x.Price));

            CreateMap<Lykke.HftApi.Domain.Entities.Trade, Lykke.HftApi.ApiContract.Trade>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.Timestamp.ToUniversalTime())))
                .ForMember(d => d.Price, o => o.MapFrom(x => x.Price.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.BaseVolume, o => o.MapFrom(x => x.BaseVolume.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.QuoteVolume, o => o.MapFrom(x => x.QuoteVolume.ToString(CultureInfo.InvariantCulture)));

            CreateMap<Lykke.HftApi.Domain.Entities.TradeFee, Lykke.HftApi.ApiContract.TradeFee>(MemberList.Destination)
                .ForMember(d => d.Size, o => o.MapFrom(x => x.Size.ToString(CultureInfo.InvariantCulture)));

            CreateMap<Lykke.HftApi.Domain.Entities.TickerEntity, Lykke.HftApi.ApiContract.TickerUpdate>(MemberList.Destination)
                .ForMember(d => d.VolumeBase, o => o.MapFrom(x => x.VolumeBase.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.VolumeQuote, o => o.MapFrom(x => x.VolumeQuote.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.PriceChange, o => o.MapFrom(x => x.PriceChange.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.LastPrice, o => o.MapFrom(x => x.LastPrice.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.High, o => o.MapFrom(x => x.High.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.Low, o => o.MapFrom(x => x.Low.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.TimeStamp.ToUniversalTime())));

            CreateMap<Lykke.HftApi.Domain.Entities.PriceEntity, Lykke.HftApi.ApiContract.PriceUpdate>(MemberList.Destination)
                .ForMember(d => d.Ask, o => o.MapFrom(x => x.Ask.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.Bid, o => o.MapFrom(x => x.Bid.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.TimeStamp.ToUniversalTime())));

            CreateMap<Lykke.HftApi.Domain.Entities.OrderbookEntity, Lykke.HftApi.ApiContract.Orderbook>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.TimeStamp.ToUniversalTime())));

            CreateMap<Lykke.HftApi.Domain.Entities.BalanceEntity, Lykke.HftApi.ApiContract.Balance>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => Timestamp.FromDateTime(x.TimeStamp.ToUniversalTime())))
                .ForMember(d => d.Available, o => o.MapFrom(x => x.Balance.ToString(CultureInfo.InvariantCulture)))
                .ForMember(d => d.Reserved, o => o.MapFrom(x => x.Reserved.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
