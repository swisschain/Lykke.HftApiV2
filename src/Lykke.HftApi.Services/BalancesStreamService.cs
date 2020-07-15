using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.Services
{
    public class BalancesStreamService : StreamServiceBase<BalanceUpdate>
    {
        public BalancesStreamService(ILogFactory logFactory, bool needPing = false) : base(logFactory, needPing)
        {
        }
    }
}
