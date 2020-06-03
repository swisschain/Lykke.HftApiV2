using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace Lykke.HftApi.Domain.Services
{
    public interface IStreamService<T> : IDisposable
    {
        Task RegisterStream(StreamInfo<T> streamInfo);
        void WriteToStream(T data, string key = null);
        void Stop();
    }
}
