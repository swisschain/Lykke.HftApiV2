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
                .WithParameter("connectionString", _config.RabbitMq.Orderbooks.ConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.Orderbooks.ExchangeName)
                .SingleInstance();

            builder.RegisterType<BalancesSubscriber>()
                .As<IStartable>()
                .AutoActivate()
                .WithParameter("connectionString", _config.RabbitMq.Balances.ConnectionString)
                .WithParameter("exchangeName", _config.RabbitMq.Balances.ExchangeName)
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
        }
    }
}

