using System;
using System.Collections.Generic;
using Autofac;
using Common.Log;
using HftApi.Common.Configuration;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;

namespace HftApi.Modules
{
    public class CqrsModule : Module
    {
        private readonly SagasRabbitMq _settings;

        public CqrsModule(SagasRabbitMq settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri(_settings.RabbitConnectionString)
            };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();

            builder
                .Register(ctx =>
                {
                    var logFactory = ctx.Resolve<ILogFactory>();
                    
                    var messagingEngine = new MessagingEngine(logFactory.CreateLog(nameof(MessagingEngine)),
                        new TransportResolver(new Dictionary<string, TransportInfo>
                        {
                            {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                        }),
                        new RabbitMqTransportFactory());
                    
                    const string defaultPipeline = "commands";
                    const string defaultRoute = "self";

                    var engine = new CqrsEngine(logFactory.CreateLog(nameof(CqrsEngine)),
                        ctx.Resolve<IDependencyResolver>(),
                        messagingEngine,
                        new DefaultEndpointProvider(),
                        true,
                        Register.DefaultEndpointResolver(new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            SerializationFormat.MessagePack,
                            environment: "lykke",
                            exclusiveQueuePostfix: "k8s")),

                        Register.BoundedContext("hft-api")
                            .PublishingCommands(typeof(CreateCashoutCommand))
                                .To(OperationsBoundedContext.Name).With(defaultPipeline));
                    engine.StartPublishers();
                    return engine;
                })
                .As<ICqrsEngine>()
                .SingleInstance()
                .AutoActivate();
        }
    }
}
