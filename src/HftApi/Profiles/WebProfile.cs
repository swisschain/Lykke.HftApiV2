using System;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.WebApi.Models;
using Lykke.Exchange.Api.MarketData;
using Lykke.HftApi.Domain.Entities;

namespace HftApi.Profiles
{
    public class WebProfile : Profile
    {
        public WebProfile()
        {
            CreateMap<OrderEntity, OrderModel>(MemberList.Destination);
            CreateMap<Order, OrderModel>(MemberList.Destination);
            CreateMap<Trade, TradeModel>(MemberList.Destination);
            CreateMap<TradeEntity, TradeModel>(MemberList.Destination);
            CreateMap<TickerEntity, TickerModel>(MemberList.Destination);
            CreateMap<Orderbook, OrderbookModel>(MemberList.Destination);
            CreateMap<VolumePrice, VolumePriceModel>(MemberList.Destination);
            CreateMap<MarketSlice, TickerModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => DateTime.UtcNow));
        }
    }
}
