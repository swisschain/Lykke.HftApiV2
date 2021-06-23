using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Entities.Assets;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services.AssetsClient;

namespace Lykke.HftApi.Services
{
    public class AssetsService : IAssetsService, IStartable
    {
        private readonly ICacheService _cache;
        private readonly TimeSpan _cacheDuration;
        private readonly AssetsHttpClient _client;

        public AssetsService(AssetsHttpClient client, ICacheService cacheService, TimeSpan cacheDuration)
        {
            _cache = cacheService;
            _cacheDuration = cacheDuration;
            _client = client;
        }

        public Task<IReadOnlyList<Asset>> GetAllAssetsAsync()
        {
            return GetAllAssetsFromCacheAsync();
        }

        public async Task<IReadOnlyList<Asset>> GetAllAssetsAsync(string clientId)
        {
            var allAssets = await GetAllAssetsFromCacheAsync();

            var availableAssets = await _client.GetAssetsAvailableForClientAsync(clientId);

            return allAssets.Where(x => availableAssets.Contains(x.AssetId)).ToList();
        }

        public async Task<Asset> GetAssetByIdAsync(string assetId)
        {
            var assets = await GetAllAssetsFromCacheAsync();

            return assets.FirstOrDefault(x => x.AssetId == assetId);
        }

        public Task<IReadOnlyList<AssetPair>> GetAllAssetPairsAsync()
        {
            return GetAllAssetPairsFromCacheAsync();
        }

        public async Task<AssetPair> GetAssetPairByIdAsync(string assetPairId)
        {
            var assetPairs = await GetAllAssetPairsFromCacheAsync();

            return assetPairs.FirstOrDefault(x => x.AssetPairId == assetPairId);
        }

        private Task<IReadOnlyList<Asset>> GetAllAssetsFromCacheAsync()
        {
            return _cache.GetOrAddAsync("Assets", async () => await _client.GetAllAssetsAsync(), _cacheDuration);
        }

        private async Task<IReadOnlyList<AssetPair>> GetAllAssetPairsFromCacheAsync()
        {
            var assetsTask = GetAllAssetsAsync();
            var assetPairsTask = _cache.GetOrAddAsync("AssetPairs", async () => await _client.GetAllAssetPairsAsync(), _cacheDuration);

            await Task.WhenAll(assetsTask, assetPairsTask);

            var assetPairs = assetPairsTask.Result.ToList();
            var assets = assetsTask.Result;

            var result = new List<AssetPair>();

            foreach (var assetPair in assetPairs)
            {
                var baseAsset = assets.FirstOrDefault(x => x.AssetId == assetPair.BaseAssetId);
                var quoteAsset = assets.FirstOrDefault(x => x.AssetId == assetPair.QuoteAssetId);
                if (baseAsset == null || quoteAsset == null)
                    continue;

                assetPair.BaseAssetAccuracy = baseAsset.Accuracy;
                assetPair.QuoteAssetAccuracy = quoteAsset.Accuracy;
                result.Add(assetPair);
            }

            return result;
        }

        public void Start()
        {
            Task.WhenAll(GetAllAssetsAsync(), GetAllAssetPairsAsync()).GetAwaiter().GetResult();
        }
    }
}
