using Swisschain.Lykke.HftApi.ApiClient.Common;
using Swisschain.Lykke.HftApi.ApiContract;

namespace Swisschain.Lykke.HftApi.ApiClient
{
    public class HftApiClient : BaseGrpcClient, IHftApiClient
    {
        public HftApiClient(string serverGrpcUrl) : base(serverGrpcUrl)
        {
            Monitoring = new Monitoring.MonitoringClient(Channel);
        }

        public Monitoring.MonitoringClient Monitoring { get; }
    }
}
