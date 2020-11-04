using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Logs;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Services;
using Microsoft.Extensions.Configuration;

namespace MeCommands
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Specify clientId and assetPairId");
                return;
            }

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var settings = new AppSettings();

            config.Bind(settings);

            string clientId = args[0];
            string assetPairId = args[1];

            var client = new TcpMatchingEngineClient(new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses(settings.Host)[0].ToString()), settings.Port),
                EmptyLogFactory.Instance, true);

            client.Start();
            await Task.Delay(300);
            int i = 0;
            Console.Clear();

            var res = await client.MassCancelLimitOrdersAsync(new LimitOrderMassCancelModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = assetPairId,
                ClientId = clientId
            });

            Console.WriteLine($"Response: {res.Status}");
        }
    }
}
