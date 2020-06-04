using System;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using Lykke.MatchingEngine.Connector.Models.Events;
using Trade = Lykke.HftApi.Domain.Entities.Trade;

namespace HftApi.Worker.Profiles
{
    public class WorkerProfile : Profile
    {
        public WorkerProfile()
        {
            CreateMap<Order, OrderEntity>(MemberList.Destination)
                .ForMember(d => d.PartitionKey, o => o.MapFrom(x => x.WalletId))
                .ForMember(d => d.RowKey, o => o.MapFrom(x => x.Id))
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

            CreateMap<Trade, TradeEntity>(MemberList.Destination)
                .ForMember(d => d.PartitionKey, o => o.MapFrom(x => x.WalletId))
                .ForMember(d => d.RowKey, o => o.MapFrom(x => x.Id))
                .ForMember(d => d.TimeStamp, o => o.Ignore())
                .ForMember(d => d.Expires, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.MapFrom(x => x.Timestamp))
                .ForMember(d => d.AssetPairId, o => o.Ignore()) //fill manually
                .ForMember(d => d.OrderId, o => o.MapFrom(x => x.OrderId)); //fill manually
        }
    }
}
