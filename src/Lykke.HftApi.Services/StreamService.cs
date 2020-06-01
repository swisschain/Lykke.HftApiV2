using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Lykke.HftApi.Domain.Services;

namespace Lykke.HftApi.Services
{
    public class StreamService<T>: IStreamService<T>
    {
        private readonly List<(TaskCompletionSource<int>, string key, IServerStreamWriter<T>)> _streamList = new List<(TaskCompletionSource<int>, string, IServerStreamWriter<T>)>();

        public void WriteToStream(T data, string key = null)
        {
            var items = string.IsNullOrEmpty(key)
                ? _streamList.ToArray()
                : _streamList.Where(x => x.key == key).ToArray();

            foreach (var pair in items)
            {
                pair.Item3.WriteAsync(data)
                    .ContinueWith(t => HandleError(pair), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private void HandleError((TaskCompletionSource<int>, string key, IServerStreamWriter<T>) pair)
        {
            pair.Item1.TrySetResult(1);
            _streamList.Remove(pair);
            Console.WriteLine("Remove stream connect");
        }

        public Task RegisterStream(IServerStreamWriter<T> stream, string key = null, bool reuseStream = false)
        {
            if (!string.IsNullOrEmpty(key) && reuseStream)
            {
                var item = _streamList.FirstOrDefault(x => x.key == key);

                if (item.Item1 != null)
                {
                    item.Item1.TrySetResult(1);
                    _streamList.Remove(item);
                }
            }

            (TaskCompletionSource<int>, string, IServerStreamWriter<T>) record;
            record.Item1 = new TaskCompletionSource<int>();
            record.Item2 = key;
            record.Item3 = stream;

            _streamList.Add(record);

            return record.Item1.Task;
        }

        public void Dispose()
        {
            foreach (var pair in _streamList)
            {
                pair.Item1.TrySetResult(1);
                Console.WriteLine("Remove stream connect");
            }
        }

        public void Stop()
        {
            foreach (var pair in _streamList)
            {
                pair.Item1.TrySetResult(1);
                Console.WriteLine("Remove stream connect");
            }
        }
    }
}
