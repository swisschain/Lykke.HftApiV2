using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.HftApi.Domain;

namespace Lykke.HftApi.Services
{
    internal class StreamData<T> : StreamInfo<T> where T : class
    {
        /// <summary>
        /// This TaskCompletionSource is used to keep stream open
        /// </summary>
        public TaskCompletionSource<int> CompletionTask { get; set; }
        public T LastSentData { get; set; }
        public bool KeepLastData { get; set; }

        public static StreamData<T> Create(StreamInfo<T> streamInfo, List<T> initData = null)
        {
            return new StreamData<T>
            {
                CompletionTask = new TaskCompletionSource<int>(),
                CancelationToken = streamInfo.CancelationToken,
                Stream = streamInfo.Stream,
                Keys = streamInfo.Keys,
                Peer = streamInfo.Peer,
                LastSentData = initData?.Last(),
                KeepLastData = initData != null
            };
        }
    }
}
