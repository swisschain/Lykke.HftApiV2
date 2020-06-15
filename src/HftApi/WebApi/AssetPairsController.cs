using System.Collections.Generic;
using System.Threading.Tasks;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Exceptions;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Route("api/assetpairs")]
    public class AssetPairsController : ControllerBase
    {
        private readonly IAssetsService _assetsService;
        private readonly ValidationService _validationService;

        public AssetPairsController(
            IAssetsService assetsService,
            ValidationService validationService
            )
        {
            _assetsService = assetsService;
            _validationService = validationService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyList<AssetPair>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAssetPairs()
        {
            var assetPairs = await _assetsService.GetAllAssetPairsAsync();
            return Ok(ResponseModel<IReadOnlyList<AssetPair>>.Ok(assetPairs));
        }

        [HttpGet("{assetPairId}")]
        [ProducesResponseType(typeof(ResponseModel<AssetPair>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssetPairAsync(string assetPairId)
        {
            var result = await _validationService.ValidateAssetPairAsync(assetPairId);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message).AddField(result.FieldName);

            var assetPair = await _assetsService.GetAssetPairByIdAsync(assetPairId);
            return Ok(ResponseModel<AssetPair>.Ok(assetPair));
        }
    }
}
