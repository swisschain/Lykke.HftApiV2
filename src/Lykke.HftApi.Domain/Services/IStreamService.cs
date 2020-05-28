using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Lykke.HftApi.Domain.Services
{
    public interface IStreamService<T> : IDisposable
    {
        void WriteToStream(T data);
        Task RegisterStream(IServerStreamWriter<T> stream);
        void Stop();
    }
}
