using System;
using Lykke.HftApi.Domain;
using Lykke.MatchingEngine.Connector.Models.Api;

namespace HftApi.Extensions
{
    public static class MeStatusExtensions
    {
        public static (HftApiErrorCode code, string message) ToHftApiError(this MeStatusCodes meCode)
        {
            return meCode switch
            {
                MeStatusCodes.Ok => (HftApiErrorCode.Success, string.Empty),
                MeStatusCodes.BadRequest => (HftApiErrorCode.MeBadRequest, "Bad request"),
                MeStatusCodes.LowBalance => (HftApiErrorCode.MeLowBalance, "Low balance"),
                MeStatusCodes.AlreadyProcessed => (HftApiErrorCode.MeAlreadyProcessed, "Already processed"),
                MeStatusCodes.DisabledAsset => (HftApiErrorCode.MeDisabledAsset, "Asset is disabled"),
                MeStatusCodes.UnknownAsset => (HftApiErrorCode.MeUnknownAsset, "Unknown asset"),
                MeStatusCodes.NoLiquidity => (HftApiErrorCode.MeNoLiquidity, "No liquidity"),
                MeStatusCodes.NotEnoughFunds => (HftApiErrorCode.MeNotEnoughFunds, "Not enough funds"),
                MeStatusCodes.Dust => (HftApiErrorCode.MeDust, "Dust"),
                MeStatusCodes.ReservedVolumeHigherThanBalance => (HftApiErrorCode.MeReservedVolumeHigherThanBalance, "Reserved volume higher than balance"),
                MeStatusCodes.NotFound => (HftApiErrorCode.MeNotFound, "Not found"),
                MeStatusCodes.BalanceLowerThanReserved => (HftApiErrorCode.MeBalanceLowerThanReserved, "Balance lower than reserved"),
                MeStatusCodes.LeadToNegativeSpread => (HftApiErrorCode.MeLeadToNegativeSpread, "Lead to negative spread"),
                MeStatusCodes.TooSmallVolume => (HftApiErrorCode.MeTooSmallVolume, "Too small volume"),
                MeStatusCodes.InvalidFee => (HftApiErrorCode.MeInvalidFee, "Invalid fee"),
                MeStatusCodes.InvalidPrice => (HftApiErrorCode.MeInvalidPrice, "Invalid price"),
                MeStatusCodes.Replaced => (HftApiErrorCode.MeReplaced, "Replaced"),
                MeStatusCodes.NotFoundPrevious => (HftApiErrorCode.MeNotFoundPrevious, "Not found previous"),
                MeStatusCodes.Duplicate => (HftApiErrorCode.MeDuplicate, "Duplicate"),
                MeStatusCodes.InvalidVolumeAccuracy => (HftApiErrorCode.MeInvalidVolumeAccuracy, "Invalid volume accuracy"),
                MeStatusCodes.InvalidPriceAccuracy => (HftApiErrorCode.MeInvalidPriceAccuracy, "Invalid price accuracy"),
                MeStatusCodes.InvalidVolume => (HftApiErrorCode.MeInvalidVolume, "Invalid volume"),
                MeStatusCodes.TooHighPriceDeviation => (HftApiErrorCode.MeTooHighPriceDeviation, "Too high price deviation"),
                MeStatusCodes.InvalidOrderValue => (HftApiErrorCode.MeInvalidOrderValue, "Invalid order value"),
                MeStatusCodes.Runtime => (HftApiErrorCode.MeRuntime, "ME not available"),
                _ => throw new ArgumentOutOfRangeException(nameof(meCode), meCode, null)
            };
        }
    }
}
