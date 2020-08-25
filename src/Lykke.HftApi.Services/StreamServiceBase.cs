using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.HftApi.Domain;

namespace Lykke.HftApi.Services
{
    public class StreamServiceBase<T> where T : class
    {
        private readonly List<StreamData<T>> _streamList = new List<StreamData<T>>();
        private readonly TimerTrigger _checkTimer;
        private readonly TimerTrigger _pingTimer;
        private readonly ILog _log;

        public StreamServiceBase(ILogFactory logFactory, bool needPing = false)
        {
            _log = logFactory.CreateLog(this);
            _checkTimer = new TimerTrigger($"StreamService<{nameof(T)}>", TimeSpan.FromSeconds(10), logFactory);
            _checkTimer.Triggered += CheckStreams;
            _checkTimer.Start();

            if (needPing)
            {
                _pingTimer = new TimerTrigger($"StreamService<{nameof(T)}>", TimeSpan.FromSeconds(30), logFactory);
                _pingTimer.Triggered += Ping;
                _pingTimer.Start();
            }
        }

        internal virtual T ProcessDataBeforeSend(T data, StreamData<T> streamData)
        {
            return data;
        }

        internal virtual T ProcessPingDataBeforeSend(T data, StreamData<T> streamData)
        {
            return data;
        }

        public Task WriteToStreamAsync(T data, string key = null)
        {
            var items = string.IsNullOrEmpty(key)
                ? _streamList.ToArray()
                : _streamList.Where(x => x.Keys.Contains(key, StringComparer.InvariantCultureIgnoreCase) || x.Keys.Length == 0).ToArray();

            items = items.Where(x => !x.CancelationToken?.IsCancellationRequested ?? true).ToArray();

            var tasks = new List<Task>();

            foreach (var streamData in items)
            {
                var processedData = ProcessDataBeforeSend(data, streamData);
                streamData.LastSentData = streamData.KeepLastData ? data : null;
                tasks.Add(WriteStreamAsync(streamData, processedData));
            }

            return Task.WhenAll(tasks);
        }

        public Task RegisterStreamAsync(StreamInfo<T> streamInfo, List<T> initData = null)
        {
            var data = StreamData<T>.Create(streamInfo, initData);

            _streamList.Add(data);

            if (initData == null)
                return data.CompletionTask.Task;

            var tasks = initData.Select(value => WriteStreamAsync(data, value)).ToList();

            Task.WhenAll(tasks).GetAwaiter().GetResult();

            return data.CompletionTask.Task;
        }

        public void Dispose()
        {
            foreach (var streamInfo in _streamList)
            {
                streamInfo.CompletionTask.TrySetResult(1);
                Console.WriteLine($"Remove stream connect (peer: {streamInfo.Peer}");
            }

            _checkTimer.Stop();
            _checkTimer.Dispose();

            _pingTimer.Stop();
            _pingTimer.Dispose();
        }

        public void Stop()
        {
            foreach (var streamInfo in _streamList)
            {
                streamInfo.CompletionTask.TrySetResult(1);
                Console.WriteLine($"Remove stream connect (peer: {streamInfo.Peer})");
            }

            _checkTimer.Stop();
            _pingTimer.Stop();
        }

        private void RemoveStream(StreamData<T> streamData)
        {
            streamData.CompletionTask.TrySetResult(1);
            _streamList.Remove(streamData);
            Console.WriteLine($"Remove stream connect (peer: {streamData.Peer})");
        }

        private Task CheckStreams(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationtoken)
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

        private async Task Ping(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationtoken)
        {
            var tasks = new List<Task>();

            if (_streamList.Count == 0)
                return;

            for (var i = _streamList.Count - 1; i >= 0; i--)
            {
                var streamData = _streamList[i];
                var instance = streamData.LastSentData ?? Activator.CreateInstance<T>();

                var data = ProcessPingDataBeforeSend(instance, streamData);
                tasks.Add(WriteStreamAsync(streamData, data));
            }

            if (tasks.Any())
                await Task.WhenAll(tasks);
        }

        private async Task WriteStreamAsync(StreamData<T> streamData, T data)
        {
            try
            {
                await streamData.Stream.WriteAsync(data);
            }
            catch (InvalidOperationException)
            {
                RemoveStream(streamData);
            }
            catch (Exception e)
            {
                _log.Error(e, "Can't write to stream", context: streamData.Peer);
                RemoveStream(streamData);
            }
        }
    }
}
