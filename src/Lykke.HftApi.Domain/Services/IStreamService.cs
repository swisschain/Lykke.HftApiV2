using System;
using System.Threading.Tasks;

namespace Lykke.HftApi.Domain.Services
{
    public interface IStreamService<T> : IDisposable where T : class
    {
        Task RegisterStream(StreamInfo<T> streamInfo, T initData = null);
        void WriteToStream(T data, string key = null);
        void Stop();
    }
}
