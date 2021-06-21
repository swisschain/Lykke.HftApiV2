using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HftApi.WebApi.Models.Withdrawals
{
    public class WithdrawalModel
    {
        public Guid WithdrawalId { get; set; }
        public string AssetId { set; get; }
        public decimal Volume { set; get; }
        public string DestinationAddress { set; get; }
        public string DestinationAddressExtension { set; get; }
        public DateTime Created { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public WithdrawalState State { set; get; }
    }
}
