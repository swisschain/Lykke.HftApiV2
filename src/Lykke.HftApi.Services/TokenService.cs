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
        private readonly IBlockedClientsService _blockedClients;
        private readonly ConcurrentDictionary<string, byte> _cache;

        public TokenService(
            IHftInternalClient hftInternalClient,
            ILogger<TokenService> logger,
            IBlockedClientsService blockedClients)
        {
            _hftInternalClient = hftInternalClient;
            _logger = logger;
            _blockedClients = blockedClients;
            _cache = new ConcurrentDictionary<string, byte>();
        }

        public async Task InitAsync()
        {
            _logger.LogInformation("API keys cache is being initialized");

            var keys = await _hftInternalClient.Keys.GetAllKeys();
            
            foreach (var key in keys)
            {
                if (!await _blockedClients.IsClientBlocked(key.ClientId))
                {
                    _cache.TryAdd(key.Id, 0);
                }
            }

            _logger.LogInformation($"API keys cache has been initialized. {_cache.Count} active keys were added to the cache");
        }

        public bool IsValid(string id)
        {
           return _cache.ContainsKey(id);
        }

        public void Add(string id)
        {
            _cache.TryAdd(id, 0);
        }

        public void Remove(string id)
        {
            _cache.TryRemove(id, out _);
        }
    }
}
