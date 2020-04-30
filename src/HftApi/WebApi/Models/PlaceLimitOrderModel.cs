using System.ComponentModel.DataAnnotations;
using Lykke.MatchingEngine.Connector.Models.Common;

namespace HftApi.WebApi.Models
{
    public class PlaceLimitOrderModel
    {
        [Required]
        public string AssetPairId { get; set; }
        [Required]
        public OrderAction Side { get; set; }
        [Required]
        public decimal Volume { get; set; }
        [Required]
        public decimal Price { get; set; }
    }
}
