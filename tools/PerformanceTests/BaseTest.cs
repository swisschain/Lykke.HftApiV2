using BenchmarkDotNet.Attributes;
using Grpc.Core;
using Lykke.HftApi.ApiClient;
using Lykke.HftApi.ApiContract;

namespace PerformanceTests
{
    public class BaseTest
    {
        protected readonly HftApiClient Client;
        protected readonly Metadata Headers;

        public BaseTest()
        {
            const string token = "";
            Client = new HftApiClient("https://hft-apiv2-grpc.lykke.com:443");
            Headers = new Metadata {{"Authorization", $"Bearer {token}"}};
        }

        [Benchmark(Baseline = true)]
        public void IsAlive()
        {
            var response = Client.Monitoring.IsAlive(new IsAliveRequest());
        }
    }
}
