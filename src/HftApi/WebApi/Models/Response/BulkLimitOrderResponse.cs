using System.Collections.Generic;
using Lykke.HftApi.Domain;

namespace HftApi.WebApi.Models.Response
{
    public class BulkLimitOrderResponse
    {
        public string AssetPairId { get; set; }
        public HftApiErrorCode? Error { get; set; }
        public IReadOnlyList<BulkOrderItemStatusModel> Statuses { get; set; }
    }

    public class BulkOrderItemStatusModel
    {
        public string Id { get; set; }
        public HftApiErrorCode? Error { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
    }
}
