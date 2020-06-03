using System.Threading;
using Grpc.Core;

namespace Lykke.HftApi.Domain
{
    public class StreamInfo<T>
    {
        public IServerStreamWriter<T> Stream { get; set; }
        public CancellationToken? CancelationToken { get; set; }
        public string Key { get; set; }
        public string Peer { get; set; }
    }
}
