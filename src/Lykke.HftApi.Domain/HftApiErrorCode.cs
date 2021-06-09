namespace Lykke.HftApi.Domain
{
    public enum HftApiErrorCode
    {
        //1000 - general server netwrork
        //1001 - 500 RuntimeError
        //1100 - validation
        //2000 - logic errors, i.e. from ME
        Success = 0,
        RuntimeError = 1001,
        ItemNotFound = 1100,
        InvalidField = 1101,
        ActionForbidden = 1102,
        //ME errors
        MeBadRequest = 2000,
        MeLowBalance = 2001,
        MeAlreadyProcessed = 2002,
        MeDisabledAsset = 2003,
        MeUnknownAsset = 2004,
        MeNoLiquidity = 2005,
        MeNotEnoughFunds = 2006,
        MeDust = 2007,
        MeReservedVolumeHigherThanBalance = 2008,
        MeNotFound = 2009,
        MeBalanceLowerThanReserved = 2010,
        MeLeadToNegativeSpread = 2011,
        MeTooSmallVolume = 2012,
        MeInvalidFee = 2013,
        MeInvalidPrice = 2014,
        MeReplaced = 2015,
        MeNotFoundPrevious = 2016,
        MeDuplicate = 2017,
        MeInvalidVolumeAccuracy = 2018,
        MeInvalidPriceAccuracy = 2019,
        MeInvalidVolume = 2020,
        MeTooHighPriceDeviation = 2021,
        MeInvalidOrderValue = 2022,
        MeRuntime = 2023,
    }
}
