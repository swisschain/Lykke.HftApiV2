using Newtonsoft.Json;

namespace Lykke.HftApi.Domain.Entities
{
    public class VolumePrice
    {
        [JsonProperty("v")]
        public decimal Volume { get; set; }
        [JsonProperty("p")]
        public decimal Price { get; set; }

        public VolumePrice()
        {
        }

        public VolumePrice(decimal volume, decimal price)
        {
            Volume = volume;
            Price = price;
        }
    }
}
