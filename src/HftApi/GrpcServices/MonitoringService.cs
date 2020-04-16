using System.Threading.Tasks;
using Grpc.Core;
using Swisschain.Sdk.Server.Common;
using Swisschain.Lykke.HftApi.ApiContract;

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
                StartedAt = ApplicationInformation.StartedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return Task.FromResult(result);
        }
    }
}
