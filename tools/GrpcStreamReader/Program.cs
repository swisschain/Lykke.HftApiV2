using System;
using System.Linq;
using System.Threading.Tasks;
using Fclp;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lykke.HftApi.ApiClient;
using Lykke.HftApi.ApiContract;
using Newtonsoft.Json;
using Enum = System.Enum;

namespace GrpcStreamReader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var appArguments = TryGetAppArguments(args);

            if (appArguments == null)
            {
                return;
            }

            if (appArguments.StreamName == StreamName.Orderbooks && string.IsNullOrEmpty(appArguments.StreamKey))
            {
                Console.WriteLine($"Stream key is required for orderbook stream");
                return;
            }

            if (appArguments.StreamName == StreamName.Balances && string.IsNullOrEmpty(appArguments.Token))
            {
                Console.WriteLine($"Token is required for balances stream");
                return;
            }

            var client = new HftApiClient(appArguments.GrpcUrl);

            var headers = new Metadata();

            if (!string.IsNullOrEmpty(appArguments.Token))
            {
                headers.Add("Authorization", $"Bearer {appArguments.Token}");
            }

            while (true)
            {
                try
                {
                    switch (appArguments.StreamName)
                    {
                        case StreamName.Prices:
                            {
                                Console.WriteLine("Get price updates....");
                                var prices = client.PublicService.GetPriceUpdates(new Empty());

                                await foreach (var item in prices.ResponseStream.ReadAllAsync())
                                {
                                    Console.WriteLine($"{JsonConvert.SerializeObject(item)}");
                                }
                            }
                            break;
                        case StreamName.Tickers:
                            {
                                Console.WriteLine("Get ticker updates....");
                                var tickers = client.PublicService.GetTickerUpdates(new Empty());

                                await foreach (var item in tickers.ResponseStream.ReadAllAsync())
                                {
                                    Console.WriteLine($"{JsonConvert.SerializeObject(item)}");
                                }
                            }
                            break;
                        case StreamName.Orderbooks:
                            {
                                Console.WriteLine($"Get orderbook updates for asset pair: {appArguments.StreamKey}");
                                using var orderbooks = client.PublicService.GetOrderbookUpdates(new OrderbookUpdatesRequest{AssetPairId = appArguments.StreamKey});

                                await foreach (var item in orderbooks.ResponseStream.ReadAllAsync())
                                {
                                    Console.WriteLine($"{JsonConvert.SerializeObject(item)}");
                                }

                                Console.WriteLine("test");
                            }
                            break;
                        case StreamName.Balances:
                            {
                                Console.WriteLine($"Get balance updates....");
                                using var orderbooks = client.PrivateService.GetBalanceUpdates(new Empty(), headers);

                                await foreach (var item in orderbooks.ResponseStream.ReadAllAsync())
                                {
                                    Console.WriteLine($"{JsonConvert.SerializeObject(item)}");
                                }
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Console.WriteLine("End of stream");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Console.WriteLine("Stream cancelled.");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal)
                {
                    Console.WriteLine($"Internal error: {ex.StatusCode}; {ex.Message}");
                }
                catch (RpcException ex)
                {
                    Console.WriteLine($"RpcException. {ex.Status}; {ex.StatusCode}");
                    Console.WriteLine(ex.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"exception: {ex.GetType().Name}");
                    Console.WriteLine(ex.ToString());
                }

                await Task.Delay(5000);
            }
        }

        private static AppArguments TryGetAppArguments(string[] args)
        {
            var parser = new FluentCommandLineParser<AppArguments>();

            parser.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));

            parser.Setup(x => x.GrpcUrl)
                .As('u', "url")
                .Required()
                .WithDescription("-u <uri>. Hft api grpc url. Required");

            parser.Setup(x => x.StreamName)
                .As('n', "name")
                .Required()
                .WithDescription($"-n <stream name>. GRPC stream name. Required. Available values: { string.Join(", ", Enum.GetValues(typeof(StreamName)).Cast<StreamName>())}");

            parser.Setup(x => x.StreamKey)
                .As('k', "key")
                .WithDescription("-k <Stream key>. GRPC stream key parameter.");

            parser.Setup(x => x.Token)
                .As('t', "token")
                .WithDescription("-t <hft api key/token>. Hft api token.");

            var parsingResult = parser.Parse(args);

            if (!parsingResult.HasErrors)
            {
                return parser.Object;
            }

            Console.WriteLine("Lykke HFT GRPC Stream Reader (c) 2020");
            Console.WriteLine("Usage:");

            parser.HelpOption.ShowHelp(parser.Options);

            return null;
        }
    }
}
