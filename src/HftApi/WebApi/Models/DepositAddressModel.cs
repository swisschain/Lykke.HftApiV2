using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HftApi.WebApi.Models
{
    public class DepositAddressModel
    {
        public string Address { get; set; }
        public string BaseAddress { get; set; }
        public string AddressExtension { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DepositAddressState State { set; get; }
    }

    public enum DepositAddressState
    {
        NotFound,
        Creating,
        Active,
        Blocked
    }
}
