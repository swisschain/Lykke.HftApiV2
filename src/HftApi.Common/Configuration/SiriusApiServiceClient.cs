using System;
using Lykke.SettingsReader.Attributes;

namespace HftApi.Common.Configuration
{
    public class SiriusApiServiceClient
    {
        public string GrpcServiceUrl { get; set; }
        public string ApiKey { get; set; }
        public long BrokerAccountId { get; set; }
        
        [Optional]
        public int WalletsActiveRetryCount { get; set; } = 100;
        
        [Optional]
        public TimeSpan WaitForActiveWalletsTimeout { get; set; } = TimeSpan.FromSeconds(1);
    }
}
