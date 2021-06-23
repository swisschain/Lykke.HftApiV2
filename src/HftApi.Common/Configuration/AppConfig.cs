namespace HftApi.Common.Configuration
{
    public class AppConfig
    {
        public DbSettings Db { set; get; }
        public string DocumentationUrl { get; set; }
        public AuthConfig Auth { get; set; }
        public ServicesConfig Services { get; set; }
        public CacheConfig Cache { get; set; }
        public RedisConfig Redis { get; set; }
        public MeConfig MatchingEngine { get; set; }
        public RabbitMqConfig RabbitMq { get; set; }
        public MyNoSqlConfig MyNoSqlServer { get; set; }
        public SagasRabbitMq SagasRabbitMq { set; get; }
        public TargetClientIdFeeSettings FeeSettings { set; get; }
    }
}
