using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Services;
using Lykke.MatchingEngine.Connector.Models.Common;

namespace Lykke.HftApi.Services
{
    public class ValidationService
    {
        private readonly IAssetsService _assetsService;
        private readonly IBalanceService _balanceService;

        public ValidationService(
            IAssetsService assetsService,
            IBalanceService balanceService
            )
        {
            _assetsService = assetsService;
            _balanceService = balanceService;
        }

        public async Task<ValidationResult> ValidateLimitOrderAsync(string walletId, string assetPairId, OrderAction side, decimal price, decimal volume)
        {
            if (price <= 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero(nameof(price)),
                    FieldName = nameof(price)
                };
            }

            if (volume <= 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero(nameof(volume)),
                    FieldName = nameof(volume)
                };
            }

            var assetPair = await _assetsService.GetAssetPairByIdAsync(assetPairId);

            if (assetPair == null)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.ItemNotFound,
                    Message = HftApiErrorMessages.AssetPairNotFound,
                    FieldName = nameof(assetPairId)
                };
            }

            if (volume < assetPair.MinVolume)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.MustBeGreaterThan(nameof(volume), assetPair.MinVolume.ToString(CultureInfo.InvariantCulture)),
                    FieldName = nameof(volume)
                };
            }

            decimal totalVolume;
            string asset;

            if (side == OrderAction.Buy)
            {
                asset = assetPair.QuoteAssetId;
                totalVolume = price * volume;
            }
            else
            {
                asset = assetPair.BaseAssetId;
                totalVolume = volume;
            }

            var balances = await _balanceService.GetBalancesAsync(walletId);

            var assetBalance = balances.FirstOrDefault(x => x.AssetId == asset);

            if (assetBalance == null || assetBalance.Available - assetBalance.Reserved < totalVolume)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.MeNotEnoughFunds,
                    Message = "Not enough funds",
                    FieldName = nameof(volume)
                };
            }

            return null;
        }

        public async Task<ValidationResult> ValidateMarketOrderAsync(string assetPairId, decimal volume)
        {
            if (volume <= 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero(nameof(volume)),
                    FieldName = nameof(volume)
                };
            }

            var assetPair = await _assetsService.GetAssetPairByIdAsync(assetPairId);

            if (assetPair == null)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.ItemNotFound,
                    Message = HftApiErrorMessages.AssetPairNotFound,
                    FieldName = nameof(assetPairId)
                };
            }

            if (volume < assetPair.MinVolume)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.MustBeGreaterThan(nameof(volume), assetPair.MinVolume.ToString(CultureInfo.InvariantCulture)),
                    FieldName = nameof(volume)
                };
            }

            return null;
        }

        public async Task<ValidationResult> ValidateOrdersRequestAsync(string assetPairId, int? offset, int? take)
        {
            var assetPairResult = await ValidateAssetPairAsync(assetPairId);

            if (assetPairResult != null)
                return assetPairResult;

            if (offset.HasValue && offset < 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero(nameof(offset)),
                    FieldName = nameof(offset)
                };
            }

            if (take.HasValue && take < 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero(nameof(take)),
                    FieldName = nameof(take)
                };
            }

            if (take.HasValue && take > Constants.MaxPageSize)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.TooBig(nameof(take), take.Value.ToString(), Constants.MaxPageSize.ToString()),
                    FieldName = nameof(take)
                };
            }

            return null;
        }

        public async Task<ValidationResult> ValidateTradesRequestAsync(string assetPairId, int? offset, int? take)
        {
            var assetPairResult = await ValidateAssetPairAsync(assetPairId);

            if (assetPairResult != null)
                return assetPairResult;

            if (offset.HasValue && offset < 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero(nameof(offset)),
                    FieldName = nameof(offset)
                };
            }

            if (take.HasValue && take < 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero(nameof(take)),
                    FieldName = nameof(take)
                };
            }

            if (take.HasValue && take > Constants.MaxPageSize)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.TooBig(nameof(take), take.Value.ToString(), Constants.MaxPageSize.ToString()),
                    FieldName = nameof(take)
                };
            }

            return null;
        }

        public async Task<ValidationResult> ValidateAssetPairAsync(string assetPairId)
        {
            if (string.IsNullOrEmpty(assetPairId))
                return null;

            var assetPair = await _assetsService.GetAssetPairByIdAsync(assetPairId);

            if (assetPair == null)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.ItemNotFound,
                    Message = HftApiErrorMessages.AssetPairNotFound,
                    FieldName = nameof(assetPairId)
                };
            }

            return null;
        }

        public async Task<ValidationResult> ValidateAssetAsync(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
                return null;

            var asset = await _assetsService.GetAssetByIdAsync(assetId);

            if (asset == null)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.ItemNotFound,
                    Message = HftApiErrorMessages.AssetNotFound,
                    FieldName = nameof(assetId)
                };
            }

            return null;
        }
    }

    public class ValidationResult
    {
        public HftApiErrorCode Code { get; set; }
        public string Message { get; set; }
        public string FieldName { get; set; }
    }
}
