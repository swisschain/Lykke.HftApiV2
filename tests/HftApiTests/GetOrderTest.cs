using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Lykke.HftApi.ApiClient;
using Lykke.HftApi.ApiContract;
using Npgsql;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace HftApiTests
{
    public class GetOrderTest

    {
        private readonly ITestOutputHelper _testOutputHelper;
        private HftApiClient _client;
        private Metadata _headers;

        #region secret
        private const string ConStr = "";
        #endregion

        public GetOrderTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            const string token = "";
            _client = new HftApiClient("https://hft-apiv2-grpc.lykke.com:443");
            _headers = new Metadata {{"Authorization", $"Bearer {token}"}};
        }

        [Fact(Skip = "manual testing")]
        public async Task GetOrderDelay()
        {
            var response = _client.PrivateService.PlaceLimitOrder(new LimitOrderRequest
            {
                AssetPairId = "ETHBTC",
                Side = Side.Sell,
                Volume = "0.01",
                Price = "200"
            }, _headers);

            if (response.Payload != null)
            {
                _testOutputHelper.WriteLine($"order id = {response.Payload.OrderId}");
                bool done = false;
                var sw = new Stopwatch();
                sw.Start();

                while (!done)
                {
                    try
                    {
                        //var orderExists = await IsOrderExists(response.Payload.OrderId);
                        var orderExists = _client.PrivateService.GetOrder(
                            new OrderRequest {OrderId = response.Payload.OrderId},
                            _headers) != null;

                        if (orderExists)
                        {
                            sw.Stop();
                            _testOutputHelper.WriteLine($"Get order {response.Payload.OrderId}");
                            _testOutputHelper.WriteLine($"Total time: {sw.ElapsedMilliseconds} msec.");

                            // if (order.Payload.Status == "Placed")
                            // {
                            var cancelResponse =_client.PrivateService.CancelOrder(new CancelOrderRequest {OrderId = response.Payload.OrderId}, _headers);
                            _testOutputHelper.WriteLine($"Cancel order result: {cancelResponse.Payload} {(cancelResponse.Error != null ? $"{cancelResponse.Error.Code}: {cancelResponse.Error.Message}" : "")}");
                            //}

                            done = true;
                        }
                        else
                        {
                            _testOutputHelper.WriteLine("order not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        _testOutputHelper.WriteLine("order not found");
                    }
                }
            }

            if (response.Error != null)
            {
                _testOutputHelper.WriteLine($"Error: {response.Error.Code} - {response.Error.Message}");
            }
        }

        [Fact (Skip = "manual testing")]
        public async Task GetActiveOrdersAsync()
        {
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    var orders = _client.PrivateService.GetActiveOrders(new OrdersRequest {Take = 10}, _headers);
                    _testOutputHelper.WriteLine($"get order {i}");
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine(ex.Message);
                }
            }
        }

        private async Task<bool> IsOrderExists(string orderId)
        {
            var sw = new Stopwatch();
            sw.Start();
            using (var con = new NpgsqlConnection(ConStr))
            using (var command = con.CreateCommand())
            {
                await con.OpenAsync();
                command.CommandText = $"select count(*) from public.orders where id = '{orderId}'";
                var result = (long)(await command.ExecuteScalarAsync());
                sw.Stop();
                _testOutputHelper.WriteLine($"Get from db: {sw.ElapsedMilliseconds} msec.");
                return result > 0;
            }
        }
    }
}
