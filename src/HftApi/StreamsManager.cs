using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Services;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Balance = Lykke.HftApi.ApiContract.Balance;
using Orderbook = Lykke.HftApi.ApiContract.Orderbook;

namespace HftApi
{
    public class StreamsManager
    {
        private readonly MyNoSqlTcpClient _noSqlTcpClient;
        private readonly IMyNoSqlServerDataReader<PriceEntity> _pricesReader;
        private readonly IMyNoSqlServerDataReader<TickerEntity> _tickerReader;
        private readonly IMyNoSqlServerDataReader<OrderbookEntity> _orderbookReader;
        private readonly IMyNoSqlServerDataReader<BalanceEntity> _balanceReader;
        private readonly PricesStreamService _priceStraem;
        private readonly TickersStreamService _tickerStream;
        private readonly OrderbookStreamService _orderbookStream;
        private readonly BalancesStreamService _balanceStream;
        private readonly IMapper _mapper;

        public StreamsManager(
            MyNoSqlTcpClient noSqlTcpClient,
            IMyNoSqlServerDataReader<PriceEntity> pricesReader,
            IMyNoSqlServerDataReader<TickerEntity> tickerReader,
            IMyNoSqlServerDataReader<OrderbookEntity> orderbookReader,
            IMyNoSqlServerDataReader<BalanceEntity> balanceReader,
            PricesStreamService priceStraem,
            TickersStreamService tickerStream,
            OrderbookStreamService orderbookStream,
            BalancesStreamService balanceStream,
            IMapper mapper
            )
        {
            _noSqlTcpClient = noSqlTcpClient;
            _pricesReader = pricesReader;
            _tickerReader = tickerReader;
            _orderbookReader = orderbookReader;
            _balanceReader = balanceReader;
            _priceStraem = priceStraem;
            _tickerStream = tickerStream;
            _orderbookStream = orderbookStream;
            _balanceStream = balanceStream;
            _mapper = mapper;
        }

        public void Start()
        {
            _pricesReader.SubscribeToUpdateEvents(prices =>
            {
                var tasks = prices.Select(price => _priceStraem.WriteToStreamAsync(_mapper.Map<PriceUpdate>(price), price.AssetPairId)).ToList();
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }, deleted => { });

            _tickerReader.SubscribeToUpdateEvents(tickers =>
            {
                var tasks = tickers.Select(ticker => _tickerStream.WriteToStreamAsync(_mapper.Map<TickerUpdate>(ticker))).ToList();
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }, deleted => { });

            _orderbookReader.SubscribeToUpdateEvents(orderbooks =>
            {
                var tasks = new List<Task>();

                foreach (var orderbook in orderbooks)
                {
                    var item = _mapper.Map<Orderbook>(orderbook);
                    item.Asks.AddRange(_mapper.Map<List<Orderbook.Types.PriceVolume>>(orderbook.Asks));
                    item.Bids.AddRange(_mapper.Map<List<Orderbook.Types.PriceVolume>>(orderbook.Bids));
                    tasks.Add(_orderbookStream.WriteToStreamAsync(item, orderbook.AssetPairId));
                }

                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }, deleted => { });

            _balanceReader.SubscribeToUpdateEvents(balances =>
            {
                var balancesByWallet = balances.GroupBy(x => x.WalletId);
                var tasks = new List<Task>();

                foreach (var walletBalanes in balancesByWallet)
                {
                    var balanceUpdate = new BalanceUpdate();
                    balanceUpdate.Balances.AddRange( _mapper.Map<List<Balance>>(walletBalanes.ToList()));
                    tasks.Add(_balanceStream.WriteToStreamAsync(balanceUpdate, walletBalanes.Key));
                }

                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }, deleted => { });

            Console.WriteLine("Stream services started.");
        }

        public void Stop()
        {
            _priceStraem.Stop();
            _tickerStream.Stop();
            _noSqlTcpClient.Stop();
            Console.WriteLine("Stream services stopped.");
        }
    }
}
