using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Antares.Service.History.GrpcClient;
using Antares.Service.History.GrpcContract.Common;
using Antares.Service.History.GrpcContract.History;
using AutoMapper;
using HftApi.Common.Configuration;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using HftApi.WebApi.Models.DepositAddresses;
using HftApi.WebApi.Models.Request;
using HftApi.WebApi.Models.Withdrawals;
using Lykke.Cqrs;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Entities.Assets;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientDialogs.Client;
using Lykke.Service.ClientDialogs.Client.Models;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Cashout;
using Lykke.Service.Operations.Contracts.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using BlockchainIntegrationType = Lykke.HftApi.Domain.Entities.Assets.BlockchainIntegrationType;
using OperationModel = Lykke.Service.Operations.Contracts.OperationModel;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace HftApi.WebApi
{
    [Authorize]
    [ApiController]
    [Route("api/operations")]
    public class OperationsController : ControllerBase
    {
        private readonly ValidationService _validationService;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IBalanceService _balanceService;
        private readonly IKycStatusService _kycStatusService;
        private readonly IOperationsClient _operationsClient;
        private readonly IAssetsService _assetsService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ISiriusWalletsService _siriusWalletsService;
        private readonly IClientDialogsClient _clientDialogsClient;
        private readonly IHistoryGrpcClient _historyGrpcClient;
        private readonly TargetClientIdFeeSettings _feeSettings;
        private readonly IMapper _mapper;

        public OperationsController(
            ValidationService validationService,
            ICqrsEngine cqrsEngine,
            IOperationsClient operationsClient,
            IKycStatusService kycStatusService,
            IBalanceService balanceService,
            IAssetsService assetsService,
            IClientAccountClient clientAccountClient,
            ISiriusWalletsService siriusWalletsService,
            IClientDialogsClient clientDialogsClient,
            IHistoryGrpcClient historyGrpcClient,
            TargetClientIdFeeSettings feeSettings,
            IMapper mapper)
        {
            _validationService = validationService;
            _cqrsEngine = cqrsEngine;
            _operationsClient = operationsClient;
            _kycStatusService = kycStatusService;
            _balanceService = balanceService;
            _assetsService = assetsService;
            _clientAccountClient = clientAccountClient;
            _siriusWalletsService = siriusWalletsService;
            _clientDialogsClient = clientDialogsClient;
            _historyGrpcClient = historyGrpcClient;
            _feeSettings = feeSettings;
            _mapper = mapper;
        }

        /// <summary>
        /// Get history of withdrawals and deposits
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="take"></param>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<List<Models.Operations.OperationModel>>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetOperationsHistoryAsync(
            [FromQuery]int offset = 0,
            [FromQuery]int take = 100)
        {
            var history = await _historyGrpcClient.History.GetHistoryAsync(new HistoryGetHistoryRequest
            {
                WalletId = User.GetWalletId(),
                Type =
                {
                    HistoryType.CashIn,
                    HistoryType.CashOut
                },
                Pagination = new PaginationInt32
                {
                    Offset = offset,
                    Limit = take
                }
            });
            
            return Ok(ResponseModel<List<Models.Operations.OperationModel>>.Ok(_mapper.Map<List<Models.Operations.OperationModel>>(history.Items.ToList())));
        }
        
        /// <summary>
        /// Create Deposit addresses
        /// </summary>
        [HttpPost]
        [Route("deposits/addresses")]
        public async Task<IActionResult> CreateDepositAddressesAsync()
        {
            await CheckDepositPreconditionsAsync();

            await _siriusWalletsService.CreateWalletAsync(User.GetClientId(), User.GetWalletId());
            
            return Ok();
        }
        
        /// <summary>
        /// Get Deposit address for a given Asset
        /// </summary>
        [HttpGet]
        [Route("deposits/addresses/{assetId}")]
        [ProducesResponseType(typeof(ResponseModel<DepositAddressModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCryptosDepositAddresses([FromRoute] string assetId)
        {
            var asset = await CheckDepositPreconditionsAsync(assetId);

            var depositWallet = await _siriusWalletsService.GetWalletAddressAsync(User.GetClientId(),  User.GetWalletId(), asset.SiriusAssetId);

            return Ok(_mapper.Map<DepositAddressModel>(depositWallet));
        }
        
        /// <summary>
        /// Create Withdrawal
        /// </summary>
        /// <param name="withdrawalId">Id of the Withdrawal</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("withdrawals/{withdrawalId}")]
        [ProducesResponseType(typeof(ResponseModel<Guid>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateWithdrawalAsync([FromRoute] Guid withdrawalId, [FromBody] CreateWithdrawalRequest request)
        {
            var result = await _validationService.ValidateWithdrawalRequestAsync(withdrawalId, request.AssetId, request.Volume);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message)
                    .AddField(result.FieldName);
            
            var asset = await _assetsService.GetAssetByIdAsync(request.AssetId);
            
            if(asset.BlockchainIntegrationType != BlockchainIntegrationType.Sirius)
                throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Asset unavailable");

            var balances = await _balanceService.GetBalancesAsync(User.GetWalletId());
            var cashoutSettings = await _clientAccountClient.ClientSettings.GetCashOutBlockSettingsAsync(User.GetClientId());
            var kycStatus = await _kycStatusService.GetKycStatusAsync(User.GetClientId());

            var cashoutCommand = new CreateCashoutCommand
            {
                OperationId = withdrawalId,
                WalletId = User.GetWalletId(),
                DestinationAddress = request.DestinationAddress,
                DestinationAddressExtension = request.DestinationAddressExtension,
                Volume = request.Volume,
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
                    Id = new Guid(User.GetClientId()),
                    Balance = balances.SingleOrDefault(x => x.AssetId == request.AssetId)?.Available ?? 0,
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

            return Ok(ResponseModel<Guid>.Ok(withdrawalId));
        }
        
        /// <summary>
        /// Get Withdrawal by Id
        /// </summary>
        /// <param name="withdrawalId">Id of the Withdrawal</param>
        /// <returns></returns>
        [HttpGet]
        [Route("withdrawals/{withdrawalId}")]
        [ProducesResponseType(typeof(ResponseModel<WithdrawalModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get(Guid withdrawalId)
        {
            OperationModel operation = null;

            try
            {
                operation = await _operationsClient.Operations.Get(withdrawalId);
                
                JObject.Parse(operation.ContextJson).TryGetValue("WalletId", out var walletId);
                
                if(walletId == null || operation.Type != OperationType.Cashout || walletId.Value<string>() != User.GetWalletId())
                    throw HftApiException.Create(HftApiErrorCode.ItemNotFound, "Withdrawal not found");
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                    throw HftApiException.Create(HftApiErrorCode.ItemNotFound, "Withdrawal not found");

                throw;
            }

            if (operation == null)
                throw HftApiException.Create(HftApiErrorCode.ItemNotFound, "Withdrawal not found");

            return Ok(ResponseModel<WithdrawalModel>.Ok(_mapper.Map<WithdrawalModel>(operation)));
        }

        private async Task<Asset> CheckDepositPreconditionsAsync(string assetId=default)
        {
            Asset asset = default;
            
            if (!string.IsNullOrWhiteSpace(assetId))
            {
                asset = await _assetsService.GetAssetByIdAsync(assetId);

                if (asset == null || asset.IsDisabled || !asset.BlockchainDepositEnabled)
                    throw HftApiException.Create(HftApiErrorCode.ItemNotFound, "Asset not found");

                var assetsAvailableToClient = await _assetsService.GetAllAssetsAsync(User.GetClientId());

                if (assetsAvailableToClient.SingleOrDefault(x => x.AssetId == assetId) == null || asset.BlockchainIntegrationType != BlockchainIntegrationType.Sirius)
                    throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Asset unavailable");
            }
            else
            {
                var allAssets = await _assetsService.GetAllAssetsAsync();
                
                if(allAssets.All(x => x.BlockchainIntegrationType != BlockchainIntegrationType.Sirius))
                    throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Asset unavailable");
            }

            var pendingDialogs = await _clientDialogsClient.ClientDialogs.GetDialogsAsync(User.GetClientId());

            if (pendingDialogs.Any(dialog => dialog.ConditionType == DialogConditionType.Predeposit))
                throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "Pending dialogs");

            var isKycNeeded = await _kycStatusService.IsKycNeededAsync(User.GetClientId());

            if (isKycNeeded)
                throw HftApiException.Create(HftApiErrorCode.ActionForbidden, "KYC required");

            return asset;
        }
    }
}
