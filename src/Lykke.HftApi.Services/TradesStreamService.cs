using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.Services
{
    public class TradesStreamService : StreamServiceBase<TradeUpdate>
    {
        public TradesStreamService(ILogFactory logFactory, bool needPing = false) : base(logFactory, needPing)
        {
        }
    }
}
