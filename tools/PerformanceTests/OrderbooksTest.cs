using BenchmarkDotNet.Attributes;
using Lykke.HftApi.ApiContract;

namespace PerformanceTests
{
    [SimpleJob(launchCount:3)]
    [MinColumn, MaxColumn]
    public class OrderbooksTest : BaseTest
    {
        [Benchmark]
        public void GetAllOrderbooks()
        {
            var response = Client.PublicService.GetOrderbooks(new OrderbookRequest());
        }

        [Benchmark]
        public void GetOrderbook()
        {
            var response = Client.PublicService.GetOrderbooks(new OrderbookRequest{AssetPairId = "BTCUSD"});
        }
    }
}
