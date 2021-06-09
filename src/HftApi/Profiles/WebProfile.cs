using System;
using System.Globalization;
using Antares.Service.History.GrpcContract.Common;
using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.Profiles.Converters;
using HftApi.WebApi.Models;
using Lykke.Exchange.Api.MarketData;
using Lykke.HftApi.Domain.Entities;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Swisschain.Sirius.Api.ApiContract.Account;
using Trade = Lykke.HftApi.Domain.Entities.Trade;
using TradeModel = HftApi.WebApi.Models.TradeModel;

namespace HftApi.Profiles
{
    public class WebProfile : Profile
    {
        public WebProfile()
        {
            CreateMap<DateTime, long>().ConvertUsing(dt => (long)(dt.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds);
            CreateMap<string, decimal>().ConvertUsing((str, res) => decimal.TryParse(str,
                NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m);
            CreateMap<OrderEntity, OrderModel>(MemberList.Destination);
            CreateMap<Order, OrderModel>(MemberList.Destination);
            CreateMap<Trade, TradeModel>(MemberList.Destination)
                .ForMember(d => d.BaseVolume, o => o.MapFrom(x => Math.Abs(x.BaseVolume)))
                .ForMember(d => d.QuoteVolume, o => o.MapFrom(x => Math.Abs(x.QuoteVolume)))
                .ForMember(d => d.Side, o => o.MapFrom(x => x.BaseVolume < 0 ? TradeSide.Sell : TradeSide.Buy));
            CreateMap<TradeEntity, TradeModel>(MemberList.Destination)
                .ForMember(d => d.BaseVolume, o => o.MapFrom(x => Math.Abs(x.BaseVolume)))
                .ForMember(d => d.QuoteVolume, o => o.MapFrom(x => Math.Abs(x.QuoteVolume)))
                .ForMember(d => d.Side, o => o.MapFrom(x => x.BaseVolume < 0 ? TradeSide.Sell : TradeSide.Buy));
            CreateMap<TickerEntity, TickerModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.UpdatedDt));
            CreateMap<Orderbook, OrderbookModel>(MemberList.Destination);
            CreateMap<VolumePrice, VolumePriceModel>(MemberList.Destination);
            CreateMap<MarketSlice, TickerModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => DateTime.UtcNow));
            CreateMap<PriceEntity, PriceModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.UpdatedDt));
            CreateMap<MarketSlice, PriceModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => DateTime.UtcNow));
            CreateMap<Lykke.Service.TradesAdapter.AutorestClient.Models.Trade, PublicTradeModel>(MemberList.Destination)
                .ForMember(d => d.Timestamp, o => o.MapFrom(x => x.DateTime))
                .ForMember(d => d.Side, o => o.MapFrom(x => x.Action == Lykke.Service.TradesAdapter.AutorestClient.Models.TradeAction.Buy ? TradeSide.Buy : TradeSide.Sell));

            CreateMap<Balance, BalanceModel>(MemberList.Destination);

            CreateMap<Lykke.Service.Operations.Contracts.OperationStatus, WithdrawalState>()
                .ConvertUsingEnumMapping(x =>
                    x.MapValue(Lykke.Service.Operations.Contracts.OperationStatus.Created,
                            WithdrawalState.InProgress)
                        .MapValue(Lykke.Service.Operations.Contracts.OperationStatus.Accepted,
                            WithdrawalState.InProgress)
                        .MapValue(Lykke.Service.Operations.Contracts.OperationStatus.Completed,
                            WithdrawalState.Completed)
                        .MapValue(Lykke.Service.Operations.Contracts.OperationStatus.Confirmed,
                            WithdrawalState.InProgress)
                        .MapValue(Lykke.Service.Operations.Contracts.OperationStatus.Corrupted,
                            WithdrawalState.Failed)
                        .MapValue(Lykke.Service.Operations.Contracts.OperationStatus.Failed,
                            WithdrawalState.Failed));
            CreateMap<Lykke.Service.Operations.Contracts.OperationModel, WithdrawalModel>(MemberList.Destination)
                .ForMember(x => x.WithdrawalId, x => x.MapFrom(y => y.Id))
                .ForMember(x => x.Created, x => x.MapFrom(y => y.Created))
                .ForMember(x => x.State, x => x.MapFrom(y => y.Status));
            
            CreateMap<HistoryResponseItem, OperationModel>(MemberList.Destination)
                .ConvertUsing(new OperationModelConverter());

            CreateMap<DepositWallet, DepositAddressModel>();
        }
    }
}
