namespace WhitelistItems
{
    public class AppSettings
    {
        public string MongoDbConnectionString { get; set; }
        public string ApiKeyCollectionName { get; set; }
        public string SiriusApiGrpcUrl { get; set; }
        public string SiriusApiKey { get; set; }
        public long BrokerAccountId { get; set; }
    }
}
