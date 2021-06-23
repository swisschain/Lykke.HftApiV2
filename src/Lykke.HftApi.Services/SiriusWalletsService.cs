using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Common;
using Common.Log;
using HftApi.Common.Configuration;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Entities.Assets;
using Lykke.HftApi.Domain.Entities.DepositWallets;
using Lykke.HftApi.Domain.Entities.Withdrawals;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services.Idempotency;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Cashout;
using Lykke.Service.Operations.Contracts.Commands;
using Microsoft.ApplicationInsights;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swisschain.Sirius.Api.ApiClient;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.User;

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
        private readonly IOperationsClient _operationsClient;
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
            IOperationsClient operationsClient,
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
            _operationsClient = operationsClient;
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

                var userCreateResponse = await _siriusApiClient.Users.CreateAsync(new CreateUserRequest
                {
                    RequestId = userRequestId,
                    NativeId = clientId
                });

                if (userCreateResponse.BodyCase == CreateUserResponse.BodyOneofCase.Error)
                {
                    var message = "Error creating User in Sirius";
                    _log.Warning(nameof(CreateWalletAsync),
                    message,
                    context: new
                    {
                        error = userCreateResponse.Error,
                        clientId,
                        requestId = userRequestId
                    });
                    throw new Exception(message);
                }

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
                    _log.Warning(nameof(CreateWalletAsync),
                    message,
                    context: new
                    {
                        error = createResponse.Error,
                        clientId,
                        requestId = accountRequestId
                    });
                    throw new Exception(message);
                }
            }
        }

        public async Task<DepositWallet> GetWalletAddressAsync(string clientId, string walletId, long siriusAssetId)
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
                _log.Warning(nameof(GetWalletAddressAsync),
                    message,
                    context: new
                    {
                        error = accountSearchResponse.Error,
                        walletId,
                        clientId
                    });
                throw new Exception(message);
            }

            if(!accountSearchResponse.Body.Items.Any())
            {
                return new DepositWallet {State = DepositWalletState.NotFound};
            }

            var account = accountSearchResponse.Body.Items.Single();

            if (account.State == AccountStateModel.Creating)
            {
                return new DepositWallet {State = DepositWalletState.Creating};
            }
            else if(account.State == AccountStateModel.Blocked)
            {
                return new DepositWallet {State = DepositWalletState.Blocked};
            } else if (account.State == AccountStateModel.Active)
            {
                var accountDetailsResponse = await _siriusApiClient.Accounts.GetDetailsAsync(new AccountGetDetailsRequest
                {
                    AccountId = account.Id,
                    AssetId = siriusAssetId
                });

                if (accountDetailsResponse.ResultCase == AccountGetDetailsResponse.ResultOneofCase.Error)
                {
                    var message = "Error fetching Sirius Account details";
                    _log.Warning(nameof(GetWalletAddressAsync),
                        message,
                        context: new
                        {
                            error = accountSearchResponse.Error,
                            walletId,
                            clientId
                        });
                    throw new Exception(message);
                }

                return new DepositWallet
                {
                    State = DepositWalletState.Active,
                    Address =
                        string.IsNullOrEmpty(accountDetailsResponse.Body.AccountDetail.Tag)
                            ? accountDetailsResponse.Body.AccountDetail.Address
                            : $"{accountDetailsResponse.Body.AccountDetail.Address}+{accountDetailsResponse.Body.AccountDetail.Tag}",
                    BaseAddress = accountDetailsResponse.Body.AccountDetail.Address,
                    AddressExtension = accountDetailsResponse.Body.AccountDetail.Tag
                };
            }
            else
            {
                var message = $"Unknown State for Account {account.Id}: {account.State.ToString()}";
                _log.Warning(nameof(GetWalletAddressAsync),
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
                return Guid.Parse(payload);
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
                    BlockchainIntegrationType = Lykke.Service.Assets.Client.Models.BlockchainIntegrationType.Sirius
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
