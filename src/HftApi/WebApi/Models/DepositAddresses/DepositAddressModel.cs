﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HftApi.WebApi.Models.DepositAddresses
{
    public class DepositAddressModel
    {
        public string AssetId { get; set; }
        public string Symbol { get; set; }
        public string Address { get; set; }
        public string BaseAddress { get; set; }
        public string AddressExtension { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DepositAddressState State { set; get; }
    }
}
