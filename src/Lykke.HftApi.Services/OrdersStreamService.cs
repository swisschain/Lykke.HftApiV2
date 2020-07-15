using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.Services
{
    public class OrdersStreamService : StreamServiceBase<OrderUpdate>
    {
        public OrdersStreamService(ILogFactory logFactory, bool needPing = false) : base(logFactory, needPing)
        {
        }
    }
}
