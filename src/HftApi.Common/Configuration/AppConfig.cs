namespace HftApi.Common.Configuration
{
    public class AppConfig
    {
        public DbConfig Db { get; set; }
        public AuthConfig Auth { get; set; }
        public ServicesConfig Services { get; set; }
        public CacheConfig Cache { get; set; }
        public RedisConfig Redis { get; set; }
        public MeConfig MatchingEngine { get; set; }
    }
}
