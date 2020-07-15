using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.Services
{
    public class TickersStreamService : StreamServiceBase<TickerUpdate>
    {
        public TickersStreamService(ILogFactory logFactory, bool needPing = false) : base(logFactory, needPing)
        {
        }
    }
}
