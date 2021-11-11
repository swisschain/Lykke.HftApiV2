using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        }

        public async Task CreateWalletAsync(string clientId, string walletId)
        {
            var accountSearchResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                BrokerAccountId = _brokerAccountId,
                UserNativeId = clientId,
                ReferenceId = walletId
            });

            if (accountSearchResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
            {
                var message = "Error fetching Sirius Account";
                _log.Warning(nameof(CreateWalletAsync),
                    message,
                    context: new
                    {
                        error = accountSearchResponse.Error,
                        walletId,
                        clientId
                    });
                throw new Exception(message);
            }

            if (!accountSearchResponse.Body.Items.Any())
            {
                string accountRequestId = $"{_brokerAccountId}_{walletId}_account";
                string userRequestId = $"{clientId}_user";

                _log.Info("Creating user in sirius.", new { clientId, requestId = userRequestId });

                var userCreateResponse = await _siriusApiClient.Users.CreateAsync(new CreateUserRequest
                {
                    RequestId = userRequestId,
                    NativeId = clientId
                });

                if (userCreateResponse.BodyCase == CreateUserResponse.BodyOneofCase.Error)
                {
                    var message = "Error creating user in sirius.";
                    _log.Warning(message, context: new { error = userCreateResponse.Error, clientId, requestId = userRequestId });
                    throw new Exception(message);
                }

                _log.Info("Creating account in sirius.", new { clientId, requestId = accountRequestId });

                var createResponse = await _siriusApiClient.Accounts.CreateAsync(new AccountCreateRequest
                {
                    RequestId = accountRequestId,
                    BrokerAccountId = _brokerAccountId,
                    UserId = userCreateResponse.User.Id,
                    ReferenceId = walletId
                });

                if (createResponse.ResultCase == AccountCreateResponse.ResultOneofCase.Error)
                {
                    var message = "Error creating user in sirius";
                    _log.Warning(message, context: new { error = createResponse.Error, clientId, requestId = accountRequestId });
                    throw new Exception(message);
                }

                var whitelistingRequestId = $"lykke:hft_wallet:{clientId}:{walletId}";

                var whitelistItemCreateResponse = await _siriusApiClient.WhitelistItems.CreateAsync(new WhitelistItemCreateRequest
                {
                    Name = "Hft Wallet Whitelist",
                    Scope = new WhitelistItemScope
                    {
                        BrokerAccountId = _brokerAccountId,
                        AccountId = createResponse.Body.Account.Id,
                        UserNativeId = clientId,
                        AccountReferenceId = walletId
                    },
                    Details = new WhitelistItemDetails
                    {
                        TransactionType = WhitelistTransactionType.Deposit,
                        TagType = new  NullableWhitelistItemTagType
                        {
                            Null = NullValue.NullValue
                        }
                    },
                    Lifespan = new WhitelistItemLifespan
                    {
                        StartsAt = Timestamp.FromDateTime(DateTime.UtcNow)
                    },
                    RequestId = whitelistingRequestId
                });

                if (whitelistItemCreateResponse.BodyCase == WhitelistItemCreateResponse.BodyOneofCase.Error)
                {
                    _log.Warning("Error creating whitelist item.", context: new { error = whitelistItemCreateResponse.Error, clientId });
                }
            }
        }

        public async Task<List<DepositWallet>> GetWalletAddressesAsync(string clientId, string walletId, long? siriusAssetId = null)
        {
            var result = new List<DepositWallet>();

            var accountSearchResponse = await _siriusApiClient.Accounts.SearchAsync(new AccountSearchRequest
            {
                BrokerAccountId = _brokerAccountId,
                UserNativeId = clientId,
                ReferenceId = walletId
            });

            if (accountSearchResponse.ResultCase == AccountSearchResponse.ResultOneofCase.Error)
            {
                var message = "Error fetching Sirius Account";
                _log.Warning(nameof(GetWalletAddressesAsync),
                    message,
                    context: new
                    {
                        error = accountSearchResponse.Error,
                        walletId,
                        clientId
                    });
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
                    if (siriusAssetId.HasValue)
                    {
                        var accountDetailsResponse = await _siriusApiClient.Accounts.GetDetailsAsync(new AccountGetDetailsRequest
                        {
                            AccountId = account.Id,
                            AssetId = siriusAssetId.Value
                        });

                        if (accountDetailsResponse.ResultCase == AccountGetDetailsResponse.ResultOneofCase.Error)
                        {
                            var message = "Error fetching Sirius Account details";
                            _log.Warning(nameof(GetWalletAddressesAsync),
                                message,
                                context: new
                                {
                                    error = accountSearchResponse.Error,
                                    walletId,
                                    clientId
                                });
                            throw new Exception(message);
                        }

                        result.Add(new DepositWallet
                        {
                            AssetId = assetById?.AssetId ?? string.Empty,
                            Symbol = assetById?.Symbol ?? string.Empty,
                            State = DepositWalletState.Active,
                            Address =
                                string.IsNullOrEmpty(accountDetailsResponse.Body.AccountDetail.Tag)
                                    ? accountDetailsResponse.Body.AccountDetail.Address
                                    : $"{accountDetailsResponse.Body.AccountDetail.Address}+{accountDetailsResponse.Body.AccountDetail.Tag}",
                            BaseAddress = accountDetailsResponse.Body.AccountDetail.Address,
                            AddressExtension = accountDetailsResponse.Body.AccountDetail.Tag
                        });
                    }
                    else
                    {
                        var accountDetails = await _siriusApiClient.Accounts.SearchDetailsAsync(new AccountDetailsSearchRequest
                        {
                            AccountId = account.Id
                        });

                        if (accountDetails.ResultCase == AccountDetailsSearchResponse.ResultOneofCase.Error)
                        {
                            var message = "Error fetching Sirius Accounts details";
                            _log.Warning(nameof(GetWalletAddressesAsync),
                                message,
                                context: new
                                {
                                    error = accountSearchResponse.Error,
                                    walletId,
                                    clientId
                                });

                            throw new Exception(message);
                        }

                        foreach (var accountItem in accountDetails.Body.Items)
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
                    }

                    break;
                }
                default:
                {
                    var message = $"Unknown State for Account {account.Id}: {account.State.ToString()}";
                    _log.Warning(nameof(GetWalletAddressesAsync),
                        message,
                        context: new
                        {
                            error = accountSearchResponse.Error,
                            walletId,
                            clientId
                        });
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
    }
}
