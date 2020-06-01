using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Lykke.HftApi.Domain.Services
{
    public interface IStreamService<T> : IDisposable
    {
        void WriteToStream(T data, string key = null);
        Task RegisterStream(IServerStreamWriter<T> stream, string key = null);
        void Stop();
    }
}
