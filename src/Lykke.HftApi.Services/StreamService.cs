using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Lykke.Common.Log;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Services;

namespace Lykke.HftApi.Services
{
    public class StreamService<T>: IStreamService<T>
    {
        private readonly List<StreamData<T>> _streamList = new List<StreamData<T>>();
        private readonly TimerTrigger _timerTrigger;

        public StreamService(ILogFactory logFactory, bool needPing = false)
        {
            if (needPing)
            {
                _timerTrigger = new TimerTrigger(nameof(StreamService<T>), TimeSpan.FromSeconds(10), logFactory);
                _timerTrigger.Triggered += Ping;
                _timerTrigger.Start();
            }
        }

        public void WriteToStream(T data, string key = null)
        {
            var items = string.IsNullOrEmpty(key)
                ? _streamList.ToArray()
                : _streamList.Where(x => x.Key == key).ToArray();

            foreach (var streamData in items)
            {
                streamData.Stream.WriteAsync(data)
                    .ContinueWith(t => RemoveStream(streamData), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public Task RegisterStream(StreamInfo<T> streamInfo)
        {
            var data = StreamData<T>.Create(streamInfo);

            _streamList.Add(data);

            return data.CompletionTask.Task;
        }

        public void Dispose()
        {
            foreach (var streamInfo in _streamList)
            {
                streamInfo.CompletionTask.TrySetResult(1);
                Console.WriteLine($"Remove stream connect (peer: {streamInfo.Peer}");
            }

            _timerTrigger.Stop();
            _timerTrigger.Dispose();
        }

        public void Stop()
        {
            foreach (var streamInfo in _streamList)
            {
                streamInfo.CompletionTask.TrySetResult(1);
                Console.WriteLine($"Remove stream connect (peer: {streamInfo.Peer}");
            }

            _timerTrigger.Stop();
        }

        private void RemoveStream(StreamData<T> streamData)
        {
            streamData.CompletionTask.TrySetResult(1);
            _streamList.Remove(streamData);
            Console.WriteLine($"Remove stream connect (peer: {streamData.Peer}");
        }

        private Task Ping(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationtoken)
        {
            var streamsToRemove = _streamList
                .Where(x => x.CancelationToken.HasValue && x.CancelationToken.Value.IsCancellationRequested)
                .ToList();

            foreach (var streamData in streamsToRemove)
            {
                RemoveStream(streamData);
            }

            return Task.CompletedTask;
        }
    }

    internal class StreamData<T> : StreamInfo<T>
    {
        public TaskCompletionSource<int> CompletionTask { get; set; }

        public static StreamData<T> Create(StreamInfo<T> streamInfo)
        {
            return new StreamData<T>
            {
                CompletionTask = new TaskCompletionSource<int>(),
                CancelationToken = streamInfo.CancelationToken,
                Stream = streamInfo.Stream,
                Key = streamInfo.Key,
                Peer = streamInfo.Peer
            };
        }
    }
}
