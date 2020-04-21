using System;
using System.Threading.Tasks;

namespace Lykke.HftApi.Domain.Services
{
    public interface ICacheService
    {
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> func, TimeSpan cacheDuration);
    }
}
