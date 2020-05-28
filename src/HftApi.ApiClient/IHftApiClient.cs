using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.ApiClient
{
    public interface IHftApiClient
    {
        Monitoring.MonitoringClient Monitoring { get; }
        PrivateService.PrivateServiceClient PrivateService { get; }
        PublicService.PublicServiceClient PublicService { get; }
    }
}
