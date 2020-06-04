using AutoMapper;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Entities;

namespace HftApi.Profiles
{
    public class WebProfile : Profile
    {
        public WebProfile()
        {
            CreateMap<Order, OrderModel>(MemberList.Destination);
            CreateMap<Trade, TradeModel>(MemberList.Destination);
        }
    }
}
