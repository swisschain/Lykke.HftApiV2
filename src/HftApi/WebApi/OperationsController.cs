using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Common.Configuration;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using HftApi.WebApi.Models.DepositAddresses;
using HftApi.WebApi.Models.Request;
using HftApi.WebApi.Models.Withdrawals;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [Authorize]
    [ApiController]
    [Route("api/operations")]
    public class OperationsController : ControllerBase
    {
        private readonly ValidationService _validationService;
        private readonly ISiriusWalletsService _siriusWalletsService;
        private readonly HistoryWrapperClient _historyWrapperClient;
        private readonly IMapper _mapper;

        public OperationsController(
            ValidationService validationService,
            ISiriusWalletsService siriusWalletsService,
            HistoryWrapperClient historyWrapperClient,
            IMapper mapper)
        {
            _validationService = validationService;
            _siriusWalletsService = siriusWalletsService;
            _historyWrapperClient = historyWrapperClient;
            _mapper = mapper;
        }

        /// <summary>
        /// Get history of withdrawals and deposits
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="take"></param>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<List<Models.Operations.OperationModel>>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetOperationsHistoryAsync([FromQuery] int offset = 0,
            [FromQuery] int take = 100)
        {
            var history = await _historyWrapperClient.GetOperationsHistoryAsync(
                User.GetWalletId(),
                offset,
                take);

            return Ok(ResponseModel<List<Models.Operations.OperationModel>>.Ok(
                _mapper.Map<List<Models.Operations.OperationModel>>(history.ToList())));
        }

        /// <summary>
        /// Create Deposit addresses
        /// </summary>
        [HttpPost]
        [Route("deposits/addresses")]
        public async Task<IActionResult> CreateDepositAddressesAsync()
        {
            await _siriusWalletsService.CheckDepositPreconditionsAsync(User.GetClientId());

            await _siriusWalletsService.CreateWalletAsync(User.GetClientId(), User.GetWalletId());

            return Ok();
        }

        /// <summary>
        /// Get Deposit address for a given Asset
        /// </summary>
        [HttpGet]
        [Route("deposits/addresses/{assetId}")]
        [ProducesResponseType(typeof(ResponseModel<DepositAddressModel>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetCryptosDepositAddress([FromRoute] string assetId)
        {
            var asset = await _siriusWalletsService.CheckDepositPreconditionsAsync(assetId);

            var depositWallet =
                await _siriusWalletsService.GetWalletAddressAsync(User.GetClientId(),
                    User.GetWalletId(),
                    asset.SiriusAssetId);

            return Ok(_mapper.Map<DepositAddressModel>(depositWallet));
        }

        /// <summary>
        /// Create Withdrawal
        /// </summary>
        /// <param name="requestId">Unique Id for idempotency</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("withdrawals")]
        [ProducesResponseType(typeof(ResponseModel<Guid>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> CreateWithdrawalAsync(
            [Required, FromHeader(Name = "X-Request-ID")] string requestId,
            [FromBody]  CreateWithdrawalRequest request)
        {
            var withdrawalId = await _siriusWalletsService.CreateWithdrawalAsync(requestId,
                User.GetClientId(),
                User.GetWalletId(),
                request.AssetId,
                request.Volume,
                request.DestinationAddress,
                request.DestinationAddressExtension);

            return Ok(withdrawalId);
        }
    }
}
