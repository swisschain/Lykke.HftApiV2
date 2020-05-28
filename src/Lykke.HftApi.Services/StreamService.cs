using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Lykke.HftApi.Domain.Services;

namespace Lykke.HftApi.Services
{
    public class StreamService<T>: IStreamService<T>
    {
        private readonly List<(TaskCompletionSource<int>, IServerStreamWriter<T>)> _streamList = new List<(TaskCompletionSource<int>, IServerStreamWriter<T>)>();

        public void WriteToStream(T data)
        {
            foreach (var pair in _streamList.ToArray())
            {
                try
                {
                    pair.Item2.WriteAsync(data);
                }
                catch (InvalidOperationException ex) when (ex.Message == "Cannot write message after request is complete.")
                {
                    pair.Item1.TrySetResult(1);
                    _streamList.Remove(pair);
                    Console.WriteLine("Remove stream connect");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    pair.Item1.TrySetResult(1);
                    _streamList.Remove(pair);
                    Console.WriteLine("Remove stream connect");
                }
            }
        }

        public Task RegisterStream(IServerStreamWriter<T> stream)
        {
            (TaskCompletionSource<int>, IServerStreamWriter<T>) record;
            record.Item1 = new TaskCompletionSource<int>();
            record.Item2 = stream;

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
