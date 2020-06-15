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
    [Route("api/assets")]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetsService _assetsService;
        private readonly ValidationService _validationService;

        public AssetsController(
            IAssetsService assetsService,
            ValidationService validationService
            )
        {
            _assetsService = assetsService;
            _validationService = validationService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<IReadOnlyList<Asset>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAssets()
        {
            var assets = await _assetsService.GetAllAssetsAsync();
            return Ok(ResponseModel<IReadOnlyList<Asset>>.Ok(assets));
        }

        [HttpGet("{assetId}")]
        [ProducesResponseType(typeof(ResponseModel<Asset>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssetAsync(string assetId)
        {
            var result = await _validationService.ValidateAssetAsync(assetId);

            if (result != null)
                throw HftApiException.Create(result.Code, result.Message).AddField(result.FieldName);

            var asset = await _assetsService.GetAssetByIdAsync(assetId);
            return Ok(ResponseModel<Asset>.Ok(asset));
        }
    }
}
