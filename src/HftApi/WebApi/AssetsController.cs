using System.Collections.Generic;
using System.Threading.Tasks;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [ApiController]
    [Route("api/assets")]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetsService _assetsService;

        public AssetsController(IAssetsService assetsService)
        {
            _assetsService = assetsService;
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
            var asset = await _assetsService.GetAssetByIdAsync(assetId);
            return Ok(ResponseModel<Asset>.Ok(asset));
        }
    }
}
