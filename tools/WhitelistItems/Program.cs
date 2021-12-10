using System;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace WhitelistItems
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));

            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            var settings = new AppSettings();

            var logFactory = LogFactory.Create().AddConsole();
            var log = logFactory.CreateLog("WhitelistItems");

            config.Bind(settings);

            var mongoUrl = new MongoUrl(settings.MongoDbConnectionString);
            ConventionRegistry.Register("Ignore extra", new ConventionPack { new IgnoreExtraElementsConvention(true) }, x => true);

            var database = new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName);

            var collection = database.GetCollection<ApiKey>(settings.ApiKeyCollectionName);

            var service = new SiriusWalletsService(settings, log);

            var now = DateTime.UtcNow;
            var allApiKeys = (await collection.FindAsync(x => x.ValidTill == null || x.ValidTill > now)).ToList();

            log.Info($"Processing {allApiKeys.Count} api keys");

            foreach (var apiKey in allApiKeys)
            {
                try
                {
                    await service.CreateWalletAsync(apiKey.ClientId, apiKey.WalletId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing walletId = {apiKey.WalletId}, clientId = {apiKey.ClientId}: {ex.Message}.");
                }
            }

            log.Info($"Finished processing!");
        }
    }
}
