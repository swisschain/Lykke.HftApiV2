using Autofac;
using HftApi.Common.Configuration;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;

namespace HftApi
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
                .As<IAssetsService>();
        }
    }
}
