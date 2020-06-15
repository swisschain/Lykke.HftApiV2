using BenchmarkDotNet.Attributes;
using Google.Protobuf.WellKnownTypes;

namespace PerformanceTests
{
    [MinColumn, MaxColumn]
    public class BalancesTest : BaseTest
    {
        [Benchmark]
        public void GetBalances()
        {
            var response = Client.PrivateService.GetBalances(new Empty(), Headers);
        }
    }
}
