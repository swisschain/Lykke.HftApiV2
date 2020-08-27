using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;

namespace Lykke.HftApi.Services
{
    public class PublicTradesStreamService: StreamServiceBase<PublicTradeUpdate>
    {
        public PublicTradesStreamService(ILogFactory logFactory, bool needPing = false) : base(logFactory, needPing)
        {
        }

        internal override PublicTradeUpdate ProcessPingDataBeforeSend(PublicTradeUpdate data, StreamData<PublicTradeUpdate> streamData)
        {
            return new PublicTradeUpdate();
        }
    }
}
