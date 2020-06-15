using BenchmarkDotNet.Attributes;
using Lykke.HftApi.ApiContract;

namespace PerformanceTests
{
    [MinColumn, MaxColumn]
    public class PlaceCancelOrderTest : BaseTest
    {
        [Benchmark]
        public void PlaceCancelLimitOrder()
        {
            var response = Client.PrivateService.PlaceLimitOrder(new LimitOrderRequest
            {
                AssetPairId = "ETHBTC",
                Side = Side.Sell,
                Volume = "0.001",
                Price = "200"
            }, Headers);

            if (response.Payload != null)
            {
                Client.PrivateService.CancelOrder(new CancelOrderRequest {OrderId = response.Payload.OrderId}, Headers);
            }
        }
    }
}
