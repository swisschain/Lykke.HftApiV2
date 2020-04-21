namespace Lykke.HftApi.Domain
{
    public enum HftApiErrorCode
    {
        //1000 - general server netwrork
        //1001 - 500 RuntimeError
        //1100 - validation
        //2000 - logic errors, i.e. from ME
        RuntimeError = 1001,
        ItemNotFound = 1100
    }
}
