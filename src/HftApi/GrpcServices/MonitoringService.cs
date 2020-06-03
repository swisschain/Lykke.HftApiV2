using System;
using System.Threading.Tasks;
using Grpc.Core;
using Lykke.HftApi.ApiContract;
using Swisschain.Sdk.Server.Common;

namespace HftApi.GrpcServices
{
    public class MonitoringService : Monitoring.MonitoringBase
    {
        public override Task<IsAliveResponce> IsAlive(IsAliveRequest request, ServerCallContext context)
        {
            var result = new IsAliveResponce
            {
                Name = ApplicationInformation.AppName,
                Version = ApplicationInformation.AppVersion,
                StartedAt = ApplicationInformation.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                Env = ApplicationEnvironment.Environment,
                Hostname = Environment.MachineName
            };

            return Task.FromResult(result);
        }
    }
}
