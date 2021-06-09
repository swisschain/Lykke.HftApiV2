using System;
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

            return data.Select(x =>
            {
                Lykke.HftApi.Domain.Entities.Blockchain blockchain;
                try
                {
                    switch (x.Blockchain)
                    {
                        case Blockchain.None:
                            blockchain = Domain.Entities.Blockchain.None;
                            break;
                        case Blockchain.Bitcoin:
                            blockchain = Domain.Entities.Blockchain.Bitcoin;
                            break;
                        case Blockchain.Ethereum:
                            blockchain = Domain.Entities.Blockchain.Ethereum;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    throw;
                }

                Lykke.HftApi.Domain.Entities.AssetType? type = null;
                try
                {
                    switch (x.Type)
                    {
                        case AssetType.Erc20Token:
                            type = Domain.Entities.AssetType.Erc20Token;
                            break;
                        case null:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    throw;
                }

                Lykke.HftApi.Domain.Entities.BlockchainIntegrationType blockchainIntegrationType;
                try
                {
                    switch (x.BlockchainIntegrationType)
                    {
                        case BlockchainIntegrationType.None:
                            blockchainIntegrationType = Domain.Entities.BlockchainIntegrationType.None;
                            break;
                        case BlockchainIntegrationType.Bil:
                            blockchainIntegrationType = Domain.Entities.BlockchainIntegrationType.Bil;
                            break;
                        case BlockchainIntegrationType.Sirius:
                            blockchainIntegrationType = Domain.Entities.BlockchainIntegrationType.Sirius;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    throw;
                }

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
                    BlockchainIntegrationType = blockchainIntegrationType,
                    IsDisabled = x.IsDisabled,
                    BlockchainDepositEnabled = x.BlockchainDepositEnabled
                };
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

        public async Task<HashSet<string>> GetAssetsAvailableForClientAsync(string clientId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/clients/{clientId}/asset-ids?isIosDevice=true");
            var response = await _client.SendAsync(request);
            
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<HashSet<string>>(responseString);
        }
    }

    public class AssetModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayId { get; set; }
        public int? DisplayAccuracy { get; set; }
        public int Accuracy { get; set; }
        public int MultiplierPower { set; get; }
        public string AssetAddress { set; get; }
        public string BlockchainIntegrationLayerId { set; get; }
        public Blockchain Blockchain { set; get; }
        public AssetType? Type { set; get; }
        public bool IsTradable { set; get; }
        public bool IsTrusted { set; get; }
        public bool KycNeeded { set; get; }
        public bool BlockchainWithdrawal { set; get; }
        public double CashoutMinimalAmount { set; get; }
        public double? LowVolumeAmount { set; get; }
        public string LykkeEntityId { set; get; }
        public long SiriusAssetId { set; get; }
        public BlockchainIntegrationType BlockchainIntegrationType { set; get; }
        public bool IsDisabled { set; get; }
        public bool BlockchainDepositEnabled { set; get; }
    }
    
    public enum BlockchainIntegrationType
    {
        None,
        Bil,
        Sirius,
    }
    
    public enum AssetType
    {
        Erc20Token,
    }
    
    public enum Blockchain
    {
        None,
        Bitcoin,
        Ethereum,
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
