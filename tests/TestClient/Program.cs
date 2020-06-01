using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lykke.HftApi.ApiClient;
using Lykke.HftApi.ApiContract;
using Newtonsoft.Json;

namespace TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HftApiClient("http://localhost:5001");
            const string token = "";

            var headers = new Metadata {{"Authorization", $"Bearer {token}"}};

            // var res = client.PrivateService.GetBalances(new Empty(),
            //     headers);
            //
            // Console.WriteLine(JsonConvert.SerializeObject(res.Payload));
            //
            // var orders = client.PrivateService.GetActiveOrders(new OrdersRequest {Take = 1}, headers);
            // Console.WriteLine(JsonConvert.SerializeObject(orders.Payload));

            // while (true)
            // {
            //     try
            //     {
            //         using var updates = client.PublicService.GetOrderbookUpdates(new OrderbookUpdatesRequest{AssetPairId = "ETHUSD"});
            //
            //         await foreach (var item in updates.ResponseStream.ReadAllAsync())
            //         {
            //             Console.WriteLine($"{JsonConvert.SerializeObject(item)}");
            //         }
            //
            //         Console.WriteLine("End of stream");
            //     }
            //     catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            //     {
            //         Console.WriteLine("Stream cancelled.");
            //     }
            //     catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal)
            //     {
            //         Console.WriteLine($"Internal error: {ex.StatusCode}; {ex.Message}");
            //     }
            //     catch (RpcException ex)
            //     {
            //         Console.WriteLine($"RpcException. {ex.Status}; {ex.StatusCode}");
            //         Console.WriteLine(ex.ToString());
            //     }
            //     catch (Exception ex)
            //     {
            //         Console.WriteLine($"exception: {ex.GetType().Name}");
            //         Console.WriteLine(ex.ToString());
            //     }
            //
            //     await Task.Delay(5000);
            // }
        }
    }
}
