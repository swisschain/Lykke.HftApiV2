using System;
using Autofac;
using HftApi.Common.Configuration;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.RabbitSubscribers;
using Lykke.Common.Log;
using Lykke.Exchange.Api.MarketData.Contract;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.HftInternalService.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.Operations.Client;
using Lykke.Service.TradesAdapter.Client;
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
        private readonly AppConfig _config;

        public AutofacModule(AppConfig config)
        {
            _config = config;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AssetsService>()
                .WithParameter(TypedParameter.From(_config.Cache.AssetsCacheDuration))
                .As<IAssetsService>()
                .As<IStartable>()
                .AutoActivate();

            builder.RegisterType<OrderbooksService>()
                .As<IOrderbooksService>()
                .WithParameter(TypedParameter.From(_config.Redis.OrderBooksCacheKeyPattern))
                .SingleInstance();

            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = _config.Redis.RedisConfiguration,
                InstanceName = _config.Redis.InstanceName
            });

            builder.RegisterInstance(cache)
                .As<IDistributedCache>()
                .SingleInstance();

            builder.RegisterMarketDataClient(new MarketDataServiceClientSettings{
                GrpcServiceUrl = _config.Services.MarketDataGrpcServiceUrl});

            builder.Register(ctx =>
            {
                var logger = ctx.Resolve<ILoggerFactory>();
                return logger.ToLykke();
            }).As<ILogFactory>();

            builder.RegisterMeClient(_config.MatchingEngine.GetIpEndPoint());

            builder.RegisterType<KeyUpdateSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.HftInternal.ConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.HftInternal.ExchangeName)
                .SingleInstance();

            builder.RegisterHftInternalClient(_config.Services.HftInternalServiceUrl);

            builder.RegisterType<TokenService>()
                .As<ITokenService>()
                .SingleInstance();

            builder.RegisterType<BalanceService>()
                .As<IBalanceService>()
                .SingleInstance();

            builder.RegisterType<ValidationService>()
                .AsSelf()
                .SingleInstance();

            var reconnectTimeoutInSec = Environment.GetEnvironmentVariable("NOSQL_PING_INTERVAL") ?? "15";

            builder.Register(ctx =>
            {
                var client = new MyNoSqlTcpClient(() => _config.MyNoSqlServer.ReaderServiceUrl, $"{ApplicationInformation.AppName}-{Environment.MachineName}", int.Parse(reconnectTimeoutInSec));
                client.Start();
                return client;
            }).AsSelf().SingleInstance();

            builder.RegisterInstance(_config.FeeSettings)
                .AsSelf();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<TickerEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.TickersTableName)
            ).As<IMyNoSqlServerDataReader<TickerEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<PriceEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.PricesTableName)
            ).As<IMyNoSqlServerDataReader<PriceEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<OrderbookEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.OrderbooksTableName)
            ).As<IMyNoSqlServerDataReader<OrderbookEntity>>().SingleInstance();

            builder.Register(ctx =>
                new MyNoSqlReadRepository<BalanceEntity>(ctx.Resolve<MyNoSqlTcpClient>(), _config.MyNoSqlServer.BalancesTableName)
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
                .WithParameter(TypedParameter.From(_config.Services.SiriusApiServiceClient.BrokerAccountId))
                .WithParameter(TypedParameter.From(_config.Services.SiriusApiServiceClient.WalletsActiveRetryCount))
                .WithParameter(TypedParameter.From(_config.Services.SiriusApiServiceClient.WaitForActiveWalletsTimeout))
                .SingleInstance();

            builder.RegisterType<TradesSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.Orders.ConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.Orders.ExchangeName)
                .SingleInstance();

            builder.Register(ctx =>
                    new TradesAdapterClient(_config.Services.TradesAdapterServiceUrl,
                        ctx.Resolve<ILogFactory>().CreateLog(nameof(TradesAdapterClient)))
                )
                .As<ITradesAdapterClient>()
                .SingleInstance();
            
            builder.Register(x => new KycStatusServiceClient(_config.Services.KycServiceClient, x.Resolve<ILogFactory>()))
                .As<IKycStatusService>()
                .SingleInstance();
            
            builder.RegisterClientAccountClient(_config.Services.ClientAccountServiceUrl);
            
            builder.RegisterOperationsClient(_config.Services.OperationsServiceUrl);
            
            builder.RegisterClientDialogsClient(_config.Services.ClientDialogsServiceUrl);
            
            builder.RegisterInstance(
                new Swisschain.Sirius.Api.ApiClient.ApiClient(_config.Services.SiriusApiServiceClient.GrpcServiceUrl, _config.Services.SiriusApiServiceClient.ApiKey)
            ).As<Swisschain.Sirius.Api.ApiClient.IApiClient>();

            builder.RegisterType<PublicTradesSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.PublicTrades.ConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.PublicTrades.ExchangeName)
                .SingleInstance();
        }
    }
}
