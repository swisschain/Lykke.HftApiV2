using System;
using AutoMapper;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain.Entities;
using Lykke.HftApi.Domain.Services;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Ticker = Lykke.HftApi.Domain.Entities.Ticker;

namespace HftApi
{
    public class ApplicationManager
    {
        private readonly MyNoSqlTcpClient _noSqlTcpClient;
        private readonly IMyNoSqlServerDataReader<Price> _pricesReader;
        private readonly IMyNoSqlServerDataReader<Ticker> _tickerReader;
        private readonly IStreamService<PriceUpdate> _priceStraem;
        private readonly IStreamService<TickerUpdate> _tickerStream;
        private readonly IMapper _mapper;

        public ApplicationManager(
            MyNoSqlTcpClient noSqlTcpClient,
            IMyNoSqlServerDataReader<Price> pricesReader,
            IMyNoSqlServerDataReader<Ticker> tickerReader,
            IStreamService<PriceUpdate> priceStraem,
            IStreamService<TickerUpdate> tickerStream,
            IMapper mapper
            )
        {
            _noSqlTcpClient = noSqlTcpClient;
            _pricesReader = pricesReader;
            _tickerReader = tickerReader;
            _priceStraem = priceStraem;
            _tickerStream = tickerStream;
            _mapper = mapper;
        }

        public void Start()
        {
            Console.WriteLine("Starting stream services...");

            _pricesReader.SubscribeToChanges(prices =>
            {
                foreach (var price in prices)
                {
                    _priceStraem.WriteToStream(_mapper.Map<PriceUpdate>(price));
                }
            });

            _tickerReader.SubscribeToChanges(tickers =>
            {
                foreach (var ticker in tickers)
                {
                    _tickerStream.WriteToStream(_mapper.Map<TickerUpdate>(ticker));
                }
            });
        }

        public void Stop()
        {
            Console.WriteLine("Stopping stream services...");

            _priceStraem.Stop();
            _tickerStream.Stop();
            _noSqlTcpClient.Stop();
        }
    }
}
