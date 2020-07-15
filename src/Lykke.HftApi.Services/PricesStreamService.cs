using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.Services
{
    public class PricesStreamService : StreamServiceBase<PriceUpdate>
    {
        public PricesStreamService(ILogFactory logFactory, bool needPing = false) : base(logFactory, needPing)
        {
        }
    }
}
