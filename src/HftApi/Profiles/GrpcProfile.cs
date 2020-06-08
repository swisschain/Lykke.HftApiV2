using System;
using System.Globalization;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using JetBrains.Annotations;
using Lykke.Exchange.Api.MarketData;
using Lykke.HftApi.Domain.Entities;

namespace HftApi.Profiles
{
    [UsedImplicitly]
    public class GrpcProfile : Profile
    {
        public GrpcProfile()
        {
            CreateMap<DateTime, string>().ConvertUsing(dt => dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            CreateMap<decimal, string>().ConvertUsing(d => d.ToString(CultureInfo.InvariantCulture));

            CreateMap<Lykke.HftApi.Domain.Entities.Balance, Lykke.HftApi.ApiContract.Balance>(MemberList.Destination);
            CreateMap<BalanceEntity, Lykke.HftApi.Domain.Entities.Balance>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt))
                .ForMember(d => d.Available, o => o.MapFrom(x => x.Balance));

            CreateMap<Lykke.HftApi.Domain.Entities.AssetPair, Lykke.HftApi.ApiContract.AssetPair>(MemberList.Destination);

            CreateMap<Lykke.HftApi.Domain.Entities.Asset, Lykke.HftApi.ApiContract.Asset>(MemberList.Destination);

            CreateMap<Lykke.HftApi.Domain.Entities.Orderbook, Lykke.HftApi.ApiContract.Orderbook>(MemberList.Destination);

            CreateMap<Lykke.HftApi.Domain.Entities.VolumePrice, Lykke.HftApi.ApiContract.Orderbook.Types.PriceVolume>(MemberList.Destination)
                .ForMember(d => d.V, o => o.MapFrom(x => x.Volume))
                .ForMember(d => d.P, o => o.MapFrom(x => x.Price));

            CreateMap<MarketSlice, Lykke.HftApi.ApiContract.Ticker>(MemberList.Destination);

            CreateMap<Lykke.HftApi.Domain.Entities.Order, Lykke.HftApi.ApiContract.Order>(MemberList.Destination)
                .ForMember(d => d.Volume, o => o.MapFrom(x => Math.Abs(x.Volume)))
                .ForMember(d => d.RemainingVolume, o => o.MapFrom(x => Math.Abs(x.RemainingVolume)))
                .ForMember(d => d.FilledVolume, o => o.MapFrom(x => x.Volume - x.RemainingVolume))
                .ForMember(d => d.FilledVolume, o => o.MapFrom(x => x.Volume - x.RemainingVolume))
                .ForMember(d => d.Cost, o => o.MapFrom(x => x.FilledVolume * x.Price));

            CreateMap<Lykke.HftApi.Domain.Entities.Trade, Lykke.HftApi.ApiContract.Trade>(MemberList.Destination);

            CreateMap<Lykke.HftApi.Domain.Entities.TradeFee, Lykke.HftApi.ApiContract.TradeFee>(MemberList.Destination);

            CreateMap<TickerEntity, Lykke.HftApi.ApiContract.TickerUpdate>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.UpdatedDt));

            CreateMap<PriceEntity, Lykke.HftApi.ApiContract.PriceUpdate>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.UpdatedDt));

            CreateMap<OrderbookEntity, Lykke.HftApi.ApiContract.Orderbook>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt));

            CreateMap<OrderbookEntity, Orderbook>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt));

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
                .ForMember(d => d.RemainingVolume, o => o.MapFrom(x => Math.Abs(x.Volume)))
                .ForMember(d => d.FilledVolume, o => o.MapFrom(x => x.Volume - x.RemainingVolume))
                .ForMember(d => d.Cost, o => o.MapFrom(x => x.FilledVolume * x.Price));

            CreateMap<TradeEntity, Lykke.HftApi.ApiContract.Trade>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.CreatedAt));
        }
    }
}
