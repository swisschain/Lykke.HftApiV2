using System.Linq;

namespace GrpcStreamReader
{
    internal class AppArguments
    {
        public string GrpcUrl { get; set; }
        public string Token { get; set; }
        public StreamName StreamName { get; set; }
        public string StreamKey { get; set; }
    }

    internal enum StreamName
    {
        Prices,
        Tickers,
        Orderbooks,
        Balances,
        Orders,
        Trades
    }

    internal static class Extensions
    {
        internal static bool OneOf(this StreamName stream, params StreamName[] names)
        {
            return names.Contains(stream);
        }
    }
}
