using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;
using Newtonsoft.Json;

namespace Lykke.HftApi.Services
{
    public class BalanceHttpClient
    {
        private readonly HttpClient _client;

        public BalanceHttpClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<IReadOnlyList<Balance>> GetBalanceAsync(string clientId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/WalletsClientBalances/{clientId}");
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<BalanceModel>>(responseString);

            return data.Select(x => new Balance
            {
                AssetId = x.AssetId,
                Available = x.Balance,
                Reserved = x.Reserved,
                Timestamp = x.UpdatedAt
            }).ToArray();
        }
    }

    internal class BalanceModel
    {
        public string AssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal Reserved { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
