using System;
using System.Threading;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Lykke.HftApi.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        public CacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> func, TimeSpan cacheDuration)
        {
            if (_memoryCache.TryGetValue(key, out T result))
                return result;

            try
            {
                await Semaphore.WaitAsync();

                if (_memoryCache.TryGetValue(key, out result))
                    return result;

                var data = await func();
                _memoryCache.Set(key, data, cacheDuration);
                return data;
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
