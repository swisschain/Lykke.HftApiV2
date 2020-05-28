using Lykke.HftApi.ApiClient.Common;
using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.ApiClient
{
    public class HftApiClient : BaseGrpcClient, IHftApiClient
    {
        public HftApiClient(string serverGrpcUrl) : base(serverGrpcUrl)
        {
            Monitoring = new Monitoring.MonitoringClient(Channel);
            PrivateService = new PrivateService.PrivateServiceClient(Channel);
            PublicService = new PublicService.PublicServiceClient(Channel);
        }

        public Monitoring.MonitoringClient Monitoring { get; }
        public PrivateService.PrivateServiceClient PrivateService { get; set; }
        public PublicService.PublicServiceClient PublicService { get; set; }
    }
}
