using System.Globalization;
using Lykke.HftApi.Domain;

namespace Lykke.HftApi.Services
{
    public class ValidationService
    {
        private const int MaxPageSize = 500;

        public ValidationResult ValidateLimitOrder(decimal price, decimal volume, decimal minVolume)
        {
            if (price <= 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero("Price"),
                    FieldName = "Price"
                };
            }

            if (volume <= 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero("Volume"),
                    FieldName = "Volume"
                };
            }

            if (volume < minVolume)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.MustBeGreaterThan("Volume", minVolume.ToString(CultureInfo.InvariantCulture)),
                    FieldName = "Volume"
                };
            }

            return null;
        }

        public ValidationResult ValidateMarketOrder(decimal volume, decimal minVolume)
        {
            if (volume <= 0)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.LessThanZero("Volume"),
                    FieldName = "Volume"
                };
            }

            if (volume < minVolume)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.MustBeGreaterThan("Volume", minVolume.ToString(CultureInfo.InvariantCulture)),
                    FieldName = "Volume"
                };
            }

            return null;
        }

        public ValidationResult ValidateOrdersRequest(int? offset, int? take)
        {
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

            if (take.HasValue && take > MaxPageSize)
            {
                return new ValidationResult
                {
                    Code = HftApiErrorCode.InvalidField,
                    Message = HftApiErrorMessages.TooBig(nameof(take), take.Value.ToString(), MaxPageSize.ToString()),
                    FieldName = nameof(take)
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
