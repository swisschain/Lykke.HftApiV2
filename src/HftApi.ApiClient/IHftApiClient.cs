using Swisschain.Lykke.HftApi.ApiContract;

namespace Swisschain.Lykke.HftApi.ApiClient
{
    public interface IHftApiClient
    {
        Monitoring.MonitoringClient Monitoring { get; }
    }
}
