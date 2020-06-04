using Autofac;
using HftApi.Common.Configuration;
using HftApi.Common.Domain.MyNoSqlEntities;
using HftApi.Worker.RabbitSubscribers;
using Lykke.Common.Log;
using Microsoft.Extensions.Logging;
using MyNoSqlServer.Abstractions;
using Swisschain.LykkeLog.Adapter;

namespace HftApi.Worker.Modules
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
            builder.Register(ctx =>
            {
                var logger = ctx.Resolve<ILoggerFactory>();
                return logger.ToLykke();
            }).As<ILogFactory>();

            builder.RegisterType<OrderbooksSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.MeConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.OrderbooksExchangeName)
                .SingleInstance();

            builder.RegisterType<BalancesSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.MeConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.BalancesExchangeName)
                .SingleInstance();

            builder.RegisterType<OrdersSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.MeConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.OrdersExchangeName)
                .SingleInstance();

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<OrderbookEntity>(() =>
                        _config.MyNoSqlServer.WriterServiceUrl,
                    _config.MyNoSqlServer.OrderbooksTableName);
            }).As<IMyNoSqlServerDataWriter<OrderbookEntity>>().SingleInstance();

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<BalanceEntity>(() =>
                        _config.MyNoSqlServer.WriterServiceUrl,
                    _config.MyNoSqlServer.BalancesTableName, DataSynchronizationPeriod.Immediately);
            }).As<IMyNoSqlServerDataWriter<BalanceEntity>>().SingleInstance();

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<OrderEntity>(() =>
                        _config.MyNoSqlServer.WriterServiceUrl,
                    _config.MyNoSqlServer.OrdersTableName);
            }).As<IMyNoSqlServerDataWriter<OrderEntity>>().SingleInstance();

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<TradeEntity>(() =>
                        _config.MyNoSqlServer.WriterServiceUrl,
                    _config.MyNoSqlServer.TradesTableName);
            }).As<IMyNoSqlServerDataWriter<TradeEntity>>().SingleInstance();
        }
    }
}

