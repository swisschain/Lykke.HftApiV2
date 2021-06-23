using System;
using Autofac;
using AzureStorage;
using AzureStorage.Tables;
using HftApi.Common.Configuration;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.RabbitSubscribers;
using Lykke.Common.Log;
using Lykke.Exchange.Api.MarketData.Contract;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.HftApi.Services.Idempotency;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.TradesAdapter.Client;
using Lykke.SettingsReader;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Swisschain.LykkeLog.Adapter;
using Swisschain.Sdk.Server.Common;

namespace HftApi.Modules
{
    public class AutofacModule : Module
    {
        private readonly IReloadingManagerWithConfiguration<AppConfig> _config;

        public AutofacModule(IReloadingManagerWithConfiguration<AppConfig> config)
        {
            _config = config;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AssetsService>()
                .WithParameter(TypedParameter.From(_config.CurrentValue.Cache.AssetsCacheDuration))
                .As<IAssetsService>()
                .As<IStartable>()
                .AutoActivate();

            builder.RegisterType<OrderbooksService>()
                .As<IOrderbooksService>()
                .WithParameter(TypedParameter.From(_config.CurrentValue.Redis.OrderBooksCacheKeyPattern))
                .SingleInstance();

            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = _config.CurrentValue.Redis.RedisConfiguration,
                InstanceName = _config.CurrentValue.Redis.InstanceName
            });

            builder.RegisterInstance(cache)
                .As<IDistributedCache>()
                .SingleInstance();

            builder.RegisterMarketDataClient(new MarketDataServiceClientSettings{
                GrpcServiceUrl = _config.CurrentValue.Services.MarketDataGrpcServiceUrl});

            builder.Register(ctx =>
            {
                var logger = ctx.Resolve<ILoggerFactory>();
                return logger.ToLykke();
            }).As<ILogFactory>();

            builder.RegisterMeClient(_config.CurrentValue.MatchingEngine.GetIpEndPoint());

            builder.RegisterType<KeyUpdateSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.CurrentValue.RabbitMq.HftInternal.ConnectionString)
                .WithParameter("exchangeName", _config.CurrentValue.RabbitMq.HftInternal.ExchangeName)
                .SingleInstance();

            builder.RegisterHftInternalClient(_config.CurrentValue.Services.HftInternalServiceUrl);

            builder.RegisterType<TokenService>()
                .As<ITokenService>()
                .SingleInstance();

            builder.RegisterType<BalanceService>()
                .As<IBalanceService>()
                .SingleInstance();

            builder.RegisterType<ValidationService>()
                .AsSelf()
                .SingleInstance();
            
            builder.RegisterType<IdempotencyService>()
                .AsSelf()
                .SingleInstance();

            var reconnectTimeoutInSec = Environment.GetEnvironmentVariable("NOSQL_PING_INTERVAL") ?? "15";

            builder.Register(ctx =>
            {
                var client = new MyNoSqlTcpClient(() => _config.CurrentValue.MyNoSqlServer.ReaderServiceUrl, $"{ApplicationInformation.AppName}-{Environment.MachineName}", int.Parse(reconnectTimeoutInSec));
                client.Start();
                return client;
            }).AsSelf().SingleInstance();

            builder.RegisterInstance(_config.CurrentValue.FeeSettings)
                .AsSelf();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<TickerEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.CurrentValue.MyNoSqlServer.TickersTableName)
            ).As<IMyNoSqlServerDataReader<TickerEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<PriceEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.CurrentValue.MyNoSqlServer.PricesTableName)
            ).As<IMyNoSqlServerDataReader<PriceEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<OrderbookEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.CurrentValue.MyNoSqlServer.OrderbooksTableName)
            ).As<IMyNoSqlServerDataReader<OrderbookEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<BalanceEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.CurrentValue.MyNoSqlServer.BalancesTableName)
            ).As<IMyNoSqlServerDataReader<BalanceEntity>>().SingleInstance();

            builder.RegisterType<PricesStreamService>()
                .WithParameter(TypedParameter.From(true))
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<TickersStreamService>()
                .WithParameter(TypedParameter.From(true))
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<OrderbookStreamService>()
                .WithParameter(TypedParameter.From(true))
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<BalancesStreamService>()
                .WithParameter(TypedParameter.From(true))
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<OrdersStreamService>()
                .WithParameter(TypedParameter.From(true))
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<TradesStreamService>()
                .WithParameter(TypedParameter.From(true))
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<PublicTradesStreamService>()
                .WithParameter(TypedParameter.From(true))
                .AsSelf()
                .SingleInstance();
            builder.RegisterType<StreamsManager>().AsSelf().SingleInstance();
            builder.RegisterType<SiriusWalletsService>()
                .As<ISiriusWalletsService>()
                .WithParameter(TypedParameter.From(_config.CurrentValue.Services.SiriusApiServiceClient.BrokerAccountId))
                .WithParameter(TypedParameter.From(_config.CurrentValue.Services.SiriusApiServiceClient.WalletsActiveRetryCount))
                .WithParameter(TypedParameter.From(_config.CurrentValue.Services.SiriusApiServiceClient.WaitForActiveWalletsTimeout))
                .SingleInstance();

            builder.RegisterType<TradesSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.CurrentValue.RabbitMq.Orders.ConnectionString)
                .WithParameter("exchangeName", _config.CurrentValue.RabbitMq.Orders.ExchangeName)
                .SingleInstance();

            builder.Register(ctx =>
                    new TradesAdapterClient(_config.CurrentValue.Services.TradesAdapterServiceUrl,
                        ctx.Resolve<ILogFactory>().CreateLog(nameof(TradesAdapterClient)))
                )
                .As<ITradesAdapterClient>()
                .SingleInstance();
            
#pragma warning disable 618
            builder.Register(x => new KycStatusServiceClient(_config.CurrentValue.Services.KycServiceClient, x.Resolve<ILogFactory>()))
#pragma warning restore 618
                .As<IKycStatusService>()
                .SingleInstance();
            
            builder.RegisterClientAccountClient(_config.CurrentValue.Services.ClientAccountServiceUrl);
            
            builder.RegisterOperationsClient(_config.CurrentValue.Services.OperationsServiceUrl);
            
            builder.RegisterClientDialogsClient(_config.CurrentValue.Services.ClientDialogsServiceUrl);
            
            builder.RegisterInstance(
                new Swisschain.Sirius.Api.ApiClient.ApiClient(_config.CurrentValue.Services.SiriusApiServiceClient.GrpcServiceUrl, _config.CurrentValue.Services.SiriusApiServiceClient.ApiKey)
            ).As<Swisschain.Sirius.Api.ApiClient.IApiClient>();

            builder.RegisterType<PublicTradesSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.CurrentValue.RabbitMq.PublicTrades.ConnectionString)
                .WithParameter("exchangeName", _config.CurrentValue.RabbitMq.PublicTrades.ExchangeName)
                .SingleInstance();
            
            builder.Register(ctx =>
                AzureTableStorage<IdempotentEntity>.Create(_config.Nested(x => x.Db.DataConnString),
                    "HftApiIdempotency", ctx.Resolve<ILogFactory>())
            ).As<INoSQLTableStorage<IdempotentEntity>>().SingleInstance();
        }
    }
}
