using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.MatchingEngine.Connector.Models.Common;

namespace HftApi.WebApi.Models.Request
{
    public class PlaceBulkLimitOrderRequest
    {
        [Required]
        public string AssetPairId { get; set; }
        public bool CancelPreviousOrders { get; set; }
        public CancelMode? CancelMode { get; set; }
        public List<BulkOrderItemModel> Orders { get; set; }
    }

    public class BulkOrderItemModel
    {
        [Required]
        public OrderAction OrderAction { get; set; }
        [Required]
        public decimal Volume { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string OldId { get; set; }
    }
}
