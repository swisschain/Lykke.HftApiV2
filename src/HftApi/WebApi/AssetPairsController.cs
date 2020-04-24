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
    [Route("api/assetpairs")]
    public class AssetPairsController : ControllerBase
    {
        private readonly IAssetsService _assetsService;

        public AssetPairsController(IAssetsService assetsService)
        {
            _assetsService = assetsService;
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
            var assetPair = await _assetsService.GetAssetPairByIdAsync(assetPairId);
            return Ok(ResponseModel<AssetPair>.Ok(assetPair));
        }
    }
}
