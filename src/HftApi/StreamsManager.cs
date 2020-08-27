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
        private readonly IMyNoSqlServerDataReader<OrderEntity> _orderReader;
        private readonly IMyNoSqlServerDataReader<PublicTradeEntity> _publicTradesReader;
        private readonly PricesStreamService _priceStraem;
        private readonly TickersStreamService _tickerStream;
        private readonly OrderbookStreamService _orderbookStream;
        private readonly BalancesStreamService _balanceStream;
        private readonly OrdersStreamService _orderStream;
        private readonly PublicTradesStreamService _publicTradesStream;
        private readonly IMapper _mapper;

        public StreamsManager(
            MyNoSqlTcpClient noSqlTcpClient,
            IMyNoSqlServerDataReader<PriceEntity> pricesReader,
            IMyNoSqlServerDataReader<TickerEntity> tickerReader,
            IMyNoSqlServerDataReader<OrderbookEntity> orderbookReader,
            IMyNoSqlServerDataReader<BalanceEntity> balanceReader,
            IMyNoSqlServerDataReader<OrderEntity> orderReader,
            IMyNoSqlServerDataReader<PublicTradeEntity> publicTradesReader,
            PricesStreamService priceStraem,
            TickersStreamService tickerStream,
            OrderbookStreamService orderbookStream,
            BalancesStreamService balanceStream,
            OrdersStreamService orderStream,
            PublicTradesStreamService publicTradesStream,
            IMapper mapper
            )
        {
            _noSqlTcpClient = noSqlTcpClient;
            _pricesReader = pricesReader;
            _tickerReader = tickerReader;
            _orderbookReader = orderbookReader;
            _balanceReader = balanceReader;
            _orderReader = orderReader;
            _publicTradesReader = publicTradesReader;
            _priceStraem = priceStraem;
            _tickerStream = tickerStream;
            _orderbookStream = orderbookStream;
            _balanceStream = balanceStream;
            _orderStream = orderStream;
            _publicTradesStream = publicTradesStream;
            _mapper = mapper;
        }

        public void Start()
        {
            _pricesReader.SubscribeToChanges(prices =>
            {
                var tasks = prices.Select(price => _priceStraem.WriteToStreamAsync(_mapper.Map<PriceUpdate>(price), price.AssetPairId)).ToList();
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            });

            _tickerReader.SubscribeToChanges(tickers =>
            {
                var tasks = tickers.Select(ticker => _tickerStream.WriteToStreamAsync(_mapper.Map<TickerUpdate>(ticker))).ToList();
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            });

            _orderbookReader.SubscribeToChanges(orderbooks =>
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
            });

            _balanceReader.SubscribeToChanges(balances =>
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
            });

            _orderReader.SubscribeToChanges(ordersEntities =>
            {
                var ordersByWallet = ordersEntities.GroupBy(x => x.WalletId);
                var tasks = new List<Task>();

                foreach (var walletOrders in ordersByWallet)
                {
                    var orderUpdate = new OrderUpdate();
                    orderUpdate.Orders.AddRange(_mapper.Map<List<Order>>(walletOrders.ToList()));
                    tasks.Add(_orderStream.WriteToStreamAsync(orderUpdate, walletOrders.Key));
                }

                Task.WhenAll(tasks).GetAwaiter().GetResult();
            });

            _publicTradesReader.SubscribeToChanges(trades =>
            {
                var tradesByAssetId = trades.GroupBy(x => x.AssetPairId);
                var tasks = new List<Task>();

                foreach (var tradeByAsset in tradesByAssetId)
                {
                    var tradesUpdate = new PublicTradeUpdate();
                    tradesUpdate.Trades.AddRange( _mapper.Map<List<PublicTrade>>(tradeByAsset.ToList()));
                    tasks.Add(_publicTradesStream.WriteToStreamAsync(tradesUpdate, tradeByAsset.Key));
                }

                Task.WhenAll(tasks).GetAwaiter().GetResult();
            });

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
