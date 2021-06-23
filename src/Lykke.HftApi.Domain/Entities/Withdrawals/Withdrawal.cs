using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swisschain.Sirius.Api.ApiContract.Withdrawal;

namespace Lykke.HftApi.Domain.Entities.Withdrawals
{
    public class Withdrawal
    {
        public Guid Id { get; set; }
        public string AssetId { set; get; }
        public decimal Volume { set; get; }
        public string DestinationAddress { set; get; }
        public string DestinationAddressExtension { set; get; }
        public DateTime Created { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public WithdrawalState State { set; get; }
    }
}
