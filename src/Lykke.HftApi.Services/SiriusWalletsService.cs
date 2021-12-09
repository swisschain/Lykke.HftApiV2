using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Google.Protobuf.WellKnownTypes;
using HftApi.Common.Configuration;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities.Assets;
using Lykke.HftApi.Domain.Entities.DepositWallets;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services.Idempotency;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Cashout;
using Lykke.Service.Operations.Contracts.Commands;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.User;
using Swisschain.Sirius.Api.ApiContract.WhitelistItems;

namespace Lykke.HftApi.Services
{
    public class SiriusWalletsService : ISiriusWalletsService
    {
        private readonly long _brokerAccountId;
        private readonly IApiClient _siriusApiClient;
        private readonly IClientDialogsClient _clientDialogsClient;
        private readonly IAssetsService _assetsService;
        private readonly IKycStatusService _kycStatusService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IBalanceService _balanceService;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly TargetClientIdFeeSettings _feeSettings;
        private readonly ValidationService _validationService;
        private readonly IdempotencyService _idempotencyService;
        private readonly ILog _log;
        private readonly IEnumerable<TimeSpan> _delay;

        public SiriusWalletsService(
            long brokerAccountId,
            IApiClient siriusApiClient,
            IClientDialogsClient clientDialogsClient,
            IAssetsService assetsService,
            IKycStatusService kycStatusService,
            IClientAccountClient clientAccountClient,
            IBalanceService balanceService,
            ICqrsEngine cqrsEngine,
            TargetClientIdFeeSettings feeSettings,
            ValidationService validationService,
            IdempotencyService idempotencyService,
            ILogFactory logFactory)
        {
            _brokerAccountId = brokerAccountId;
            _siriusApiClient = siriusApiClient;
            _clientDialogsClient = clientDialogsClient;
            _assetsService = assetsService;
            _kycStatusService = kycStatusService;
            _clientAccountClient = clientAccountClient;
            _balanceService = balanceService;
            _cqrsEngine = cqrsEngine;
            _feeSettings = feeSettings;
            _validationService = validationService;
            _idempotencyService = idempotencyService;
            _log = logFactory.CreateLog(this);

            _delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 7, fastFirst: true);
        }

        public async Task CreateWalletAsync(string clientId, string walletId)
        {
            var accountSearchResponse = await SearchAccountAsync(clientId, walletId);

            if (accountSearchResponse == null)
            {
                var message = "Error getting account from sirius.";

                _log.Warning(message, context: new { clientId, walletId });

                throw new Exception(message);
            }

            if (accountSearchResponse.Body.Items.Count == 0)
            {
                string accountRequestId = $"{_brokerAccountId}_{walletId}_account";
                string userRequestId = $"{clientId}_user";

                var userCreateResponse = await CreateUserAsync(clientId, userRequestId);

                if (userCreateResponse == null)
                {
                    var message = "Error creating user in sirius.";
                    _log.Warning(message, context: new { clientId, requestId = userRequestId });
                    throw new Exception(message);
                }

                var createResponse = await CreateAccountAsync(walletId, userCreateResponse.User.Id, accountRequestId);

                if (createResponse == null)
                {
                    var message = "Error creating account in sirius.";
                    _log.Warning(message, context: new { clientId, requestId = accountRequestId });
                    throw new Exception(message);
                }

                var whitelistingRequestId = $"lykke:hft:deposit:{clientId}:{walletId}";

                var whitelistItemRequest = new WhitelistItemCreateRequest
                {
                    Name = "HFT Wallet Deposits Whitelist",
                    Scope = new WhitelistItemScope
                    {
                        BrokerAccountId = _brokerAccountId,
                        AccountId = createResponse.Body.Account.Id,
                        UserNativeId = clientId
                    },
                    Details = new WhitelistItemDetails
                    {
                        TransactionType = WhitelistTransactionType.Deposit,
                        TagType = new NullableWhitelistItemTagType { Null = NullValue.NullValue }
                    },
                    Lifespan = new WhitelistItemLifespan { StartsAt = Timestamp.FromDateTime(DateTime.UtcNow) },
                    RequestId = whitelistingRequestId
                };

                var whitelistItemCreateResponse = await CreateWhitelistItemAsync(whitelistItemRequest);

                if (whitelistItemCreateResponse == null)
                {
                    var message = "Error creating whitelist item.";
                    _log.Warning(message, context: new { clientId, walletId, requestId = whitelistingRequestId});
                    throw new Exception(message);
                }
            }
        }

        public async Task<List<DepositWallet>> GetWalletAddressesAsync(string clientId, string walletId, long? siriusAssetId = null)
        {
            var result = new List<DepositWallet>();

            var accountSearchResponse = await SearchAccountAsync(clientId, walletId);

            if (accountSearchResponse == null)
            {
                var message = "Error getting account from sirius.";
                _log.Warning(message, context: new { clientId, walletId });
                throw new Exception(message);
            }

            var assets = (await _assetsService.GetAllAssetsAsync())
                .Where(x => !x.IsDisabled && x.BlockchainIntegrationType == BlockchainIntegrationType.Sirius && !string.IsNullOrWhiteSpace(x.SiriusBlockchainId))
                .ToList();

            var assetById = siriusAssetId.HasValue
                ? assets.FirstOrDefault(x => x.SiriusAssetId == siriusAssetId.Value)
                : null;

            if (!accountSearchResponse.Body.Items.Any())
            {
                if (assetById != null)
                {
                    result.Add(new DepositWallet
                    {
                        AssetId = assetById.AssetId,
                        Symbol = assetById.Symbol,
                        State = DepositWalletState.NotFound
                    });
                }

                return result;
            }

            var account = accountSearchResponse.Body.Items.Single();

            switch (account.State)
            {
                case AccountStateModel.Creating:
                    if (assetById != null)
                        result.Add(new DepositWallet {AssetId = assetById.AssetId, Symbol = assetById.Symbol, State = DepositWalletState.Creating});
                    break;
                case AccountStateModel.Blocked:
                    if (assetById != null)
                        result.Add(new DepositWallet {AssetId = assetById.AssetId, Symbol = assetById.Symbol, State = DepositWalletState.Blocked});
                    break;
                case AccountStateModel.Active:
                {
                    var accountDetailsResponse = await SearchAccountDetailsAsync(account.Id, siriusAssetId);

                    if (accountDetailsResponse == null)
                    {
                        var message = "Error getting account details from sirius.";
                        _log.Warning(message, context: new { clientId, walletId });
                        throw new Exception(message);
                    }

                    foreach (var accountItem in accountDetailsResponse.Body.Items)
                    {
                        var asset = assets.FirstOrDefault(a => a.SiriusBlockchainId == accountItem.BlockchainId);

                        if (asset == null)
                            continue;

                        result.Add(new DepositWallet
                        {
                            AssetId = asset.AssetId,
                            Symbol = asset.Symbol,
                            State = DepositWalletState.Active,
                            Address = string.IsNullOrEmpty(accountItem.Tag)
                                ? accountItem.Address
                                : $"{accountItem.Address}+{accountItem.Tag}",
                            BaseAddress = accountItem.Address,
                            AddressExtension = accountItem.Tag
                        });
                    }

                    break;
                }
                default:
                {
                    var message = $"Unknown State for Account {account.Id}: {account.State.ToString()}";
                    _log.Warning(message, context: new { clientId, walletId });
                    throw new Exception(message);
                }
            }

            return result;
        }

        public async Task<Guid> CreateWithdrawalAsync(
            string requestId,
            string clientId,
            string walletId,
            string assetId,
            decimal volume,
            string destinationAddress,
            string destinationAddressExtension)
        {
            var uniqueRequestId = $"{walletId}_{requestId}";

            var validationResult =
                await _validationService.ValidateWithdrawalRequestAsync(assetId, volume);

            if (validationResult != null)
                throw HftApiException.Create(validationResult.Code, validationResult.Message).AddField(validationResult.FieldName);

            var operationId = Guid.NewGuid();

            var payload = await _idempotencyService.CreateEntityOrGetPayload(uniqueRequestId, operationId.ToString());

            if (payload != null)
            {
                operationId = Guid.Parse(payload);
            }

            var asset = await _assetsService.GetAssetByIdAsync(assetId);

            if(asset.BlockchainIntegrationType != BlockchainIntegrationType.Sirius)
                throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Asset unavailable");

            var balances = await _balanceService.GetBalancesAsync(walletId);
            var cashoutSettings = await _clientAccountClient.ClientSettings.GetCashOutBlockSettingsAsync(clientId);
            var kycStatus = await _kycStatusService.GetKycStatusAsync(clientId);

            var cashoutCommand = new CreateCashoutCommand
            {
                OperationId = operationId,
                WalletId = walletId,
                DestinationAddress = destinationAddress,
                DestinationAddressExtension = destinationAddressExtension,
                Volume = volume,
                Asset = new AssetCashoutModel
                {
                    Id = asset.AssetId,
                    DisplayId = asset.Symbol,
                    MultiplierPower = asset.MultiplierPower,
                    AssetAddress = asset.AssetAddress,
                    Accuracy = asset.Accuracy,
                    BlockchainIntegrationLayerId = asset.BlockchainIntegrationLayerId,
                    Blockchain = asset.Blockchain.ToString(),
                    Type = asset.Type?.ToString(),
                    IsTradable = asset.IsTradable,
                    IsTrusted = asset.IsTrusted,
                    KycNeeded = asset.KycNeeded,
                    BlockchainWithdrawal = asset.BlockchainWithdrawal,
                    CashoutMinimalAmount = (decimal)asset.CashoutMinimalAmount,
                    LowVolumeAmount = (decimal?)asset.LowVolumeAmount ?? 0,
                    LykkeEntityId = asset.LykkeEntityId,
                    SiriusAssetId = asset.SiriusAssetId,
                    BlockchainIntegrationType = Service.Assets.Client.Models.BlockchainIntegrationType.Sirius
                },
                Client = new ClientCashoutModel
                {
                    Id = new Guid(clientId),
                    Balance = balances.SingleOrDefault(x => x.AssetId == assetId)?.Available ?? 0,
                    CashOutBlocked = cashoutSettings.CashOutBlocked,
                    KycStatus = kycStatus.ToString()
                },
                GlobalSettings = new GlobalSettingsCashoutModel
                {
                    MaxConfirmationAttempts = -1,
                    TwoFactorEnabled = false,
                    CashOutBlocked = false, // TODO
                    FeeSettings = new FeeSettingsCashoutModel
                    {
                        TargetClients = new Dictionary<string, string>
                        {
                            { "Cashout", _feeSettings.WithdrawalFeeDestinationClientId }
                        }
                    }
                }
            };

            _cqrsEngine.SendCommand(cashoutCommand, "hft-api", OperationsBoundedContext.Name);

            return operationId;
        }

        public async Task<Asset> CheckDepositPreconditionsAsync(string clientId, string assetId=default)
        {
            Asset asset = default;

            if (!string.IsNullOrWhiteSpace(assetId))
            {
                asset = await _assetsService.GetAssetByIdAsync(assetId);

                if (asset == null || asset.IsDisabled || !asset.BlockchainDepositEnabled)
                    throw HftApiException.Create(HftApiErrorCode.ItemNotFound, "Asset not found");

                var assetsAvailableToClient = await _assetsService.GetAllAssetsAsync(clientId);

                if (assetsAvailableToClient.SingleOrDefault(x => x.AssetId == assetId) == null || asset.BlockchainIntegrationType != BlockchainIntegrationType.Sirius)
                    throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Asset unavailable");
            }
            else
            {
                var allAssets = await _assetsService.GetAllAssetsAsync();

                if(allAssets.All(x => x.BlockchainIntegrationType != BlockchainIntegrationType.Sirius))
                    throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Asset unavailable");
            }

            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(clientId);

            if (pendingDialogs.Any(dialog => dialog.ConditionType == DialogConditionType.Predeposit))
                throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Pending dialogs");

            var isKycNeeded = await _kycStatusService.IsKycNeededAsync(clientId);

            if (isKycNeeded)
                throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "KYC required");

            return asset;
        }

        private async Task<AccountSearchResponse> SearchAccountAsync(string clientId, string walletId = null)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { clientId });
                    return true;
                })
                .OrResult<AccountSearchResponse>(response =>
                    {
                        if (response.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.ResultCase == AccountSearchResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
                {
                    BrokerAccountId = _brokerAccountId, UserNativeId = clientId, ReferenceId = walletId ?? clientId
                }));

                if (result.ResultCase != AccountSearchResponse.ResultOneofCase.Error)
                    return result;

                _log.Error(message: $"Error getting account from sirius: {result.Error.ErrorMessage}.", context: new { clientId, walletId });
                return null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting account from sirius.", new { clientId });
                return null;
            }
        }

        private async Task<CreateUserResponse> CreateUserAsync(string clientId, string userRequestId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { clientId, requestId = userRequestId });
                    return true;
                })
                .OrResult<CreateUserResponse>(response =>
                    {
                        if (response.BodyCase == CreateUserResponse.BodyOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.BodyCase == CreateUserResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.Info("Creating user in sirius.", new { clientId, requestId = userRequestId });

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Users.CreateAsync(
                    new CreateUserRequest { RequestId = userRequestId, NativeId = clientId }
                ));

                if (result.BodyCase != CreateUserResponse.BodyOneofCase.Error)
                    return result;

                _log.Error(message: $"Error creating user in sirius: {result.Error.ErrorMessage}.", context: new { clientId, requestId = userRequestId });
                return null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating user in sirius.", new { clientId, requestId = userRequestId });
                return null;
            }
        }

        private async Task<AccountCreateResponse> CreateAccountAsync(string walletId, long userId, string accountRequestId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { walletId, userId, requestId = accountRequestId });
                    return true;
                })
                .OrResult<AccountCreateResponse>(response =>
                    {
                        if (response.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.ResultCase == AccountCreateResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.Info("Creating account in sirius.", new { clientId = walletId, requestId = accountRequestId });

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.CreateAsync(new AccountCreateRequest
                    {
                        RequestId = accountRequestId, BrokerAccountId = _brokerAccountId, UserId = userId, ReferenceId = walletId
                    }
                ));

                if (result.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                {
                    _log.Error(message: $"Error creating account in sirius: {result.Error.ErrorMessage}.", context: new { clientId = walletId, userId, requestId = accountRequestId });
                    return null;
                }

                _log.Info("Account created in sirius.", new { account = result.Body.Account,
                    clientId = walletId, requestId = accountRequestId });

                return result;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating account in sirius.", new { clientId = walletId, requestId = accountRequestId });
                return null;
            }
        }

        private async Task<WhitelistItemCreateResponse> CreateWhitelistItemAsync(WhitelistItemCreateRequest request)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });
                    return true;
                })
                .OrResult<WhitelistItemCreateResponse>(response =>
                    {
                        if (response.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                _log.Info("Creating whitelist item in sirius.",
                    context: new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.WhitelistItems.CreateAsync(request));

                if (result.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                {
                    _log.Error(message: $"Error creating whitelist item in sirius: {result.Error.ErrorMessage}", context: new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });
                    return null;
                }

                _log.Info("Whitelist item created in sirius.",
                    context: new { whitelistItemId = result.WhitelistItem.Id, requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });

                return result;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating whitelist item in sirius." , new { requestId = request.RequestId, clientId = request.Scope.UserNativeId, accountId = request.Scope.AccountId });
                return null;
            }
        }

        private async Task<AccountDetailsSearchResponse> SearchAccountDetailsAsync(long accountId, long? assetId)
        {
            var retryPolicy = Policy
                .Handle<Exception>(ex =>
                {
                    _log.Warning($"Retry on Exception: {ex.Message}.", ex, new { accountId, assetId });
                    return true;
                })
                .OrResult<AccountDetailsSearchResponse>(response =>
                    {
                        if (response.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Error)
                        {
                            _log.Warning("Response from sirius.", context: response.ToJson());
                        }

                        return response.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Error;
                    }
                )
                .WaitAndRetryAsync(_delay);

            try
            {
                var request = assetId.HasValue
                    ? new AccountDetailsSearchRequest { AccountId = accountId, AssetId = assetId }
                    : new AccountDetailsSearchRequest { AccountId = accountId };

                var result = await retryPolicy.ExecuteAsync(async () => await _siriusApiClient.Accounts.SearchDetailsAsync(request));

                if (result.ResultCase != AccountDetailsSearchResponse.ResultOneofCase.Error)
                    return result;

                _log.Error($"Error getting account details from sirius: {result.Error.ErrorMessage}.", context: new { accountId, assetId });
                return null;

            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error getting account details from sirius.", new { accountId, assetId });
                return null;
            }
        }
    }
}
