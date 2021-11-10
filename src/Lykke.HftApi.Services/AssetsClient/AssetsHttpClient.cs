using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Entities.Assets;
using Newtonsoft.Json;

namespace Lykke.HftApi.Services.AssetsClient
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
            var data = JsonConvert.DeserializeObject<List<AssetResponseModel>>(responseString);

            return data.Select(x =>
                {
                    Blockchain blockchain = x.Blockchain switch
                    {
                        BlockchainResponse.None => Blockchain.None,
                        BlockchainResponse.Bitcoin => Blockchain.Bitcoin,
                        BlockchainResponse.Ethereum => Blockchain.Ethereum,
                        _ => throw new ArgumentOutOfRangeException(nameof(x.Blockchain), x.Blockchain, null)
                    };

                    AssetType? type = x.Type switch
                    {
                        AssetTypeResponse.Erc20Token => AssetType.Erc20Token,
                        null => default,
                        _ => throw new ArgumentOutOfRangeException(nameof(x.Type), x.Type, null)
                    };

                    BlockchainIntegrationType blockchainIntegrationType = x.BlockchainIntegrationType switch
                        {
                            BlockchainIntegrationTypeResponse.None => BlockchainIntegrationType.None,
                            BlockchainIntegrationTypeResponse.Bil => BlockchainIntegrationType.Bil,
                            BlockchainIntegrationTypeResponse.Sirius => BlockchainIntegrationType.Sirius,
                            _ => throw new ArgumentOutOfRangeException(nameof(x.BlockchainIntegrationType), x.BlockchainIntegrationType, null)
                        };

                    return new Asset
                    {
                        AssetId = x.Id,
                        Name = x.Name,
                        Symbol = x.DisplayId ?? x.Id,
                        Accuracy = x.DisplayAccuracy ?? x.Accuracy,
                        MultiplierPower = x.MultiplierPower,
                        AssetAddress = x.AssetAddress,
                        BlockchainIntegrationLayerId = x.BlockchainIntegrationLayerId,
                        Blockchain = blockchain,
                        Type = type,
                        IsTradable = x.IsTradable,
                        IsTrusted = x.IsTrusted,
                        KycNeeded = x.KycNeeded,
                        BlockchainWithdrawal = x.BlockchainWithdrawal,
                        CashoutMinimalAmount = x.CashoutMinimalAmount,
                        LowVolumeAmount = x.LowVolumeAmount,
                        LykkeEntityId = x.LykkeEntityId,
                        SiriusAssetId = x.SiriusAssetId,
                        SiriusBlockchainId = x.SiriusBlockchainId,
                        BlockchainIntegrationType = blockchainIntegrationType,
                        IsDisabled = x.IsDisabled,
                        BlockchainDepositEnabled = x.BlockchainDepositEnabled
                    };
                })
                .ToArray();
        }

        public async Task<IReadOnlyList<AssetPair>> GetAllAssetPairsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/asset-pairs");
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<AssetPairResponseModel>>(responseString);

            return data.Where(x => !x.IsDisabled)
                .Select(x => new AssetPair
                {
                    AssetPairId = x.Id,
                    BaseAssetId = x.BaseAssetId,
                    QuoteAssetId = x.QuotingAssetId,
                    Name = x.Name,
                    PriceAccuracy = x.Accuracy,
                    MinVolume = x.MinVolume,
                    MinOppositeVolume = x.MinInvertedVolume
                })
                .ToArray();
        }

        public async Task<HashSet<string>> GetAssetsAvailableForClientAsync(string clientId)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, $"/api/v2/clients/{clientId}/asset-ids?isIosDevice=true");
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HashSet<string>>(responseString);
        }
    }
}
