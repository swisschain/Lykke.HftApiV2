using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HftApi.Common.Configuration;
using HftApi.Common.Persistence;
using Swisschain.Sdk.Server.Common;

namespace HftApi.Worker
{
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
            services.AddPersistence(Config.Db.ConnectionString);
        }
    }
}
