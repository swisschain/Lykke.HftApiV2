using System;
using MyNoSqlServer.Abstractions;

namespace HftApi.Common.Domain.MyNoSqlEntities
{
    public class TickerEntity : IMyNoSqlDbEntity
    {
        public string AssetPairId { get; set; }
        public decimal VolumeBase { get; set; }
        public decimal VolumeQuote { get; set; }
        public decimal PriceChange { get; set; }
        public decimal LastPrice { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public DateTime UpdatedDt { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }

        public static string GetPk() => "Ticker";
    }
}
