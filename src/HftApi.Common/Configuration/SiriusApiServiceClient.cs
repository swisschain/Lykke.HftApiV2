using System;

namespace HftApi.Common.Configuration
{
    public class SiriusApiServiceClient
    {
        public string GrpcServiceUrl { get; set; }
        public string ApiKey { get; set; }
        public long BrokerAccountId { get; set; }
        public int WalletsActiveRetryCount { get; set; } = 100;
        public TimeSpan WaitForActiveWalletsTimeout { get; set; } = TimeSpan.FromSeconds(1);
    }
}
