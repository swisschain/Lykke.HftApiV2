using Lykke.Service.Kyc.Client;

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
        public string ClientAccountServiceUrl { get; set; }
        public string OperationsServiceUrl { get; set; }
        public string ClientDialogsServiceUrl { set; get; }
        public KycServiceClientSettings KycServiceClient { get; set; }
        public SiriusApiServiceClient SiriusApiServiceClient { set; get; }
    }
}
