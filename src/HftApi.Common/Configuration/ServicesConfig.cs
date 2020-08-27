namespace HftApi.Common.Configuration
{
    public class ServicesConfig
    {
        public string AssetsServiceUrl { get; set; }
        public string MarketDataGrpcServiceUrl { get; set; }
        public string HistoryServiceUrl { get; set; }
        public string BalancesServiceUrl { get; set; }
        public string HftInternalServiceUrl { get; set; }
        public string TradesAdapterServiceUrl { get; set; }
    }
}
