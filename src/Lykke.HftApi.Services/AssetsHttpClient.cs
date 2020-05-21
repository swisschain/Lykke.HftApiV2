using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;
using Newtonsoft.Json;

namespace Lykke.HftApi.Services
{
    public class AssetsHttpClient
    {
        private readonly HttpClient _client;

        public AssetsHttpClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyList<Asset>> GetAllAssetsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/assets?includeNonTradable=false");
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<AssetModel>>(responseString);

            return data.Select(x => new Asset
            {
                AssetId = x.Id,
                Name = x.Name,
                Symbol = x.DisplayId,
                Accuracy = x.DisplayAccuracy ?? x.Accuracy
            }).ToArray();
        }

        public async Task<IReadOnlyList<AssetPair>> GetAllAssetPairsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/asset-pairs");
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<AssetPairModel>>(responseString);

            return data.Where(x => !x.IsDisabled).Select(x => new AssetPair
            {
                AssetPairId = x.Id,
                BaseAssetId = x.BaseAssetId,
                QuoteAssetId = x.QuotingAssetId,
                Name = x.Name,
                PriceAccuracy = x.Accuracy,
                MinVolume = x.MinVolume,
                MinOppositeVolume = x.MinInvertedVolume
            }).ToArray();
        }
    }

    public class AssetModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayId { get; set; }
        public int? DisplayAccuracy { get; set; }
        public int Accuracy { get; set; }
    }

    public class AssetPairModel
    {
        public string Id { get; set; }
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public string Name { get; set; }
        public int Accuracy { get; set; }
        public decimal MinVolume { get; set; }
        public decimal MinInvertedVolume { get; set; }
        public bool IsDisabled { get; set; }
    }
}
