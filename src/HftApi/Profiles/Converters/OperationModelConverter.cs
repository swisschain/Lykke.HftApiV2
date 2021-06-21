using System;
using Antares.Service.History.GrpcContract.Common;
using AutoMapper;
using HftApi.WebApi.Models;
using HftApi.WebApi.Models.Operations;

namespace HftApi.Profiles.Converters
{
    public class OperationModelConverter : ITypeConverter<HistoryResponseItem, OperationModel>
    {
        public OperationModel Convert(HistoryResponseItem source, OperationModel destination, ResolutionContext context)
        {
            var result = new OperationModel();
            
            if (source.CashIn != null)
            {
                result.HistoricalId = source.Id;
                result.AssetId = source.CashIn.AssetId;
                result.Type = OperationType.Deposit;
                result.TotalAmount = Math.Abs(source.CashIn.Volume);
                result.Fee = source.CashIn.FeeSize ?? 0m;
            }
            else
            {
                result.HistoricalId = source.Id;
                result.AssetId = source.CashOut.AssetId;
                result.Type = OperationType.Withdrawal;
                result.TotalAmount = Math.Abs(source.CashOut.Volume);
                result.Fee = source.CashOut.FeeSize ?? 0m;
            }

            return result;
        }
    }
}
