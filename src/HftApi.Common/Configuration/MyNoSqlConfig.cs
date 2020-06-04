namespace HftApi.Common.Configuration
{
    public class MyNoSqlConfig
    {
        public string WriterServiceUrl { get; set; }
        public string ReaderServiceUrl { get; set; }
        public string TickersTableName { get; set; }
        public string PricesTableName { get; set; }
        public string OrderbooksTableName { get; set; }
        public string BalancesTableName { get; set; }
        public string OrdersTableName { get; set; }
    }
}
