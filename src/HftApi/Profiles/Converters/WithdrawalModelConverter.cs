using AutoMapper;
using HftApi.WebApi.Models;
using HftApi.WebApi.Models.Withdrawals;
using Newtonsoft.Json.Linq;
using OperationModel = Lykke.Service.Operations.Contracts.OperationModel;

namespace HftApi.Profiles.Converters
{
    public class WithdrawalModelConverter : ITypeConverter<OperationModel, WithdrawalModel>
    {
        public WithdrawalModel Convert(OperationModel source, WithdrawalModel destination, ResolutionContext context)
        {
            dynamic operationContext = JObject.Parse(source.ContextJson);

            var result = new WithdrawalModel
            {
                Created = source.Created,
                State = context.Mapper.Map<WithdrawalState>(source.Status),
                WithdrawalId = source.Id,
                Volume = operationContext.Volume,
                AssetId = operationContext.Asset.Id,
                DestinationAddress = operationContext.DestinationAddress,
                DestinationAddressExtension = operationContext.DestinationAddressExtension
            };

            return result;
        }
    }
}
