using System.ComponentModel.DataAnnotations;

namespace HftApi.WebApi.Models.Request
{
    public class CreateWithdrawalRequest
    {
        [Required]
        public string AssetId { get; set; }
        [Required]
        public decimal Volume { get; set; }
        [Required]
        public string DestinationAddress { get; set; }
        public string DestinationAddressExtension { get; set; }
    }
}
