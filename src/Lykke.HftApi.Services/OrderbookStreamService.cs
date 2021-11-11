using System;
using System.Collections.Generic;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Lykke.Common.Log;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain.Services;

namespace Lykke.HftApi.Services
{
    public class OrderbookStreamService : StreamServiceBase<Orderbook>, IDisposable
    {
        private readonly IOrderbooksService _orderbooksService;
        private readonly IMapper _mapper;

        public OrderbookStreamService(
            IOrderbooksService orderbooksService,
            IMapper mapper,
            ILogFactory logFactory,
            bool needPing = false) : base(logFactory, needPing)
        {
            _orderbooksService = orderbooksService;
            _mapper = mapper;
        }

        internal override Orderbook ProcessDataBeforeSend(Orderbook data, StreamData<Orderbook> streamData)
        {
            return GetOrderbook(data, streamData, false);
        }

        internal override Orderbook ProcessPingDataBeforeSend(Orderbook data, StreamData<Orderbook> streamData)
        {
            return GetOrderbook(data, streamData, true);
        }

        private Orderbook GetOrderbook(Orderbook data, StreamData<Orderbook> streamData, bool updateDate)
        {
            if (streamData.LastSentData == null)
                return data;

            var newOrderBook = _mapper.Map<Domain.Entities.Orderbook>(data);
            var oldOrderBook = _mapper.Map<Domain.Entities.Orderbook>(streamData.LastSentData);
            Domain.Entities.Orderbook update = _orderbooksService.GetOrderbookUpdates(oldOrderBook, newOrderBook);

            var result = _mapper.Map<Orderbook>(update);

            if (updateDate)
                result.Timestamp = Timestamp.FromDateTime(DateTime.UtcNow);

            return result;

        }
    }
}
