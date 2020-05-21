using System.Collections.Concurrent;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Services;
using Lykke.Service.HftInternalService.Client;
using Microsoft.Extensions.Logging;

namespace Lykke.HftApi.Services
{
    public class TokenService : ITokenService
    {
        private readonly IHftInternalClient _hftInternalClient;
        private readonly ILogger<TokenService> _logger;
        private readonly ConcurrentDictionary<string, byte> _cache;

        public TokenService(
            IHftInternalClient hftInternalClient,
            ILogger<TokenService> logger
            )
        {
            _hftInternalClient = hftInternalClient;
            _logger = logger;
            _cache = new ConcurrentDictionary<string, byte>();
        }

        public async Task InitAsync()
        {
            _logger.LogInformation("Getting key ids");
            var ids = await _hftInternalClient.Keys.GetAllKeyIds();
            _logger.LogInformation($"Caching {ids.Count} ids");

            foreach (var id in ids)
            {
                _cache.TryAdd(id, 0);
            }
        }

        public bool IsValid(string id)
        {
           return _cache.ContainsKey(id);
        }

        public void Add(string id)
        {
            _logger.LogInformation($"Adding {id} to cache");
            _cache.TryAdd(id, 0);
        }

        public void Remove(string id)
        {
            _logger.LogInformation($"Removing {id} from cache");
            _cache.TryRemove(id, out _);
        }
    }
}
