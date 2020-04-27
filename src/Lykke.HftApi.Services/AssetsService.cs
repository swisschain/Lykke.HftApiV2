using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;

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

        public async Task<Asset> GetAssetByIdAsync(string assetId)
        {
            var assets = await GetAllAssetsFromCacheAsync();

            var asset = assets.FirstOrDefault(x => x.AssetId == assetId);

            if (asset == null)
                throw new HftApiException(HftApiErrorCode.ItemNotFound, "Asset not found")
                    .AddField(nameof(assetId), "Asset not found");

            return asset;
        }

        public Task<IReadOnlyList<AssetPair>> GetAllAssetPairsAsync()
        {
            return GetAllAssetPairsFromCacheAsync();
        }

        public async Task<AssetPair> GetAssetPairByIdAsync(string assetPairId)
        {
            var assetPairs = await GetAllAssetPairsFromCacheAsync();

            var assetPair = assetPairs.FirstOrDefault(x => x.AssetPairId == assetPairId);

            if (assetPair == null)
                throw new HftApiException(HftApiErrorCode.ItemNotFound, "Asset pair not found")
                    .AddField(nameof(assetPairId), "Asset pair not found");

            return assetPair;
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

            foreach (var assetPair in assetPairs)
            {
                var baseAsset = assets.FirstOrDefault(x => x.AssetId == assetPair.BaseAssetId);
                var quoteAsset = assets.FirstOrDefault(x => x.AssetId == assetPair.QuoteAssetId);

                assetPair.BaseAssetAccuracy = baseAsset?.Accuracy ?? 0;
                assetPair.QuoteAssetAccuracy = quoteAsset?.Accuracy ?? 0;
            }

            return assetPairs;
        }

        public void Start()
        {
            Task.WhenAll(GetAllAssetsAsync(), GetAllAssetPairsAsync()).GetAwaiter().GetResult();
        }
    }
}
