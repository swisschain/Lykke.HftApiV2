using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HftApi.WebApi.Models.Withdrawals
{
    public class WithdrawalModel
    {
        public Guid Id { get; set; }
        public string AssetId { set; get; }
        public decimal Volume { set; get; }
        public string DestinationAddress { set; get; }
        public string DestinationAddressExtension { set; get; }
        public DateTime Created { get; set; }
    }
}
