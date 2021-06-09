using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HftApi.WebApi.Models
{
    public class WithdrawalModel
    {
        public Guid WithdrawalId { get; set; }
        public DateTime Created { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public WithdrawalState State { set; get; }
    }

    public enum WithdrawalState
    {
        InProgress,
        Completed,
        Failed
    }
}
