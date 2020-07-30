using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HftApi.Common.Configuration;
using HftApi.Worker.Modules;
using JetBrains.Annotations;
using Lykke.HftApi.Services;
using Swisschain.Sdk.Server.Common;

namespace HftApi.Worker
{
    [UsedImplicitly]
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            base.ConfigureServicesExt(services);

            services.AddHttpClient();

            services.AddHttpClient<BalanceHttpClient>(client =>
            {
                client.BaseAddress = new Uri(Config.Services.BalancesServiceUrl);
            });
        }

        protected override void ConfigureContainerExt(ContainerBuilder builder)
        {
            builder.RegisterModule(new AutofacModule(Config));
            builder.RegisterModule(new AutoMapperModule());
        }
    }
}
