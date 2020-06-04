using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using HftApi.Common.Domain.MyNoSqlEntities;
using Lykke.HftApi.ApiContract;
using Lykke.HftApi.Domain.Services;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Balance = Lykke.HftApi.ApiContract.Balance;
using Orderbook = Lykke.HftApi.ApiContract.Orderbook;

namespace HftApi
{
    public class ApplicationManager
    {
        private readonly MyNoSqlTcpClient _noSqlTcpClient;
        private readonly IMyNoSqlServerDataReader<PriceEntity> _pricesReader;
        private readonly IMyNoSqlServerDataReader<TickerEntity> _tickerReader;
        private readonly IMyNoSqlServerDataReader<OrderbookEntity> _orderbookReader;
        private readonly IMyNoSqlServerDataReader<BalanceEntity> _balanceReader;
        private readonly IMyNoSqlServerDataReader<OrderEntity> _orderReader;
        private readonly IMyNoSqlServerDataReader<TradeEntity> _tradeReader;
        private readonly IStreamService<PriceUpdate> _priceStraem;
        private readonly IStreamService<TickerUpdate> _tickerStream;
        private readonly IStreamService<Orderbook> _orderbookStream;
        private readonly IStreamService<BalanceUpdate> _balanceStream;
        private readonly IStreamService<OrderUpdate> _orderStream;
        private readonly IStreamService<TradeUpdate> _tradeStream;
        private readonly IMapper _mapper;

        public ApplicationManager(
            MyNoSqlTcpClient noSqlTcpClient,
            IMyNoSqlServerDataReader<PriceEntity> pricesReader,
            IMyNoSqlServerDataReader<TickerEntity> tickerReader,
            IMyNoSqlServerDataReader<OrderbookEntity> orderbookReader,
            IMyNoSqlServerDataReader<BalanceEntity> balanceReader,
            IMyNoSqlServerDataReader<OrderEntity> orderReader,
            IMyNoSqlServerDataReader<TradeEntity> tradeReader,
            IStreamService<PriceUpdate> priceStraem,
            IStreamService<TickerUpdate> tickerStream,
            IStreamService<Orderbook> orderbookStream,
            IStreamService<BalanceUpdate> balanceStream,
            IStreamService<OrderUpdate> orderStream,
            IStreamService<TradeUpdate> tradeStream,
            IMapper mapper
            )
        {
            _noSqlTcpClient = noSqlTcpClient;
            _pricesReader = pricesReader;
            _tickerReader = tickerReader;
            _orderbookReader = orderbookReader;
            _balanceReader = balanceReader;
            _orderReader = orderReader;
            _tradeReader = tradeReader;
            _priceStraem = priceStraem;
            _tickerStream = tickerStream;
            _orderbookStream = orderbookStream;
            _balanceStream = balanceStream;
            _orderStream = orderStream;
            _tradeStream = tradeStream;
            _mapper = mapper;
        }

        public void Start()
        {
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

            _orderbookReader.SubscribeToChanges(orderbooks =>
            {
                foreach (var orderbook in orderbooks)
                {
                    var item = _mapper.Map<Orderbook>(orderbook);
                    item.Asks.AddRange(_mapper.Map<List<Orderbook.Types.PriceVolume>>(orderbook.Asks));
                    item.Bids.AddRange(_mapper.Map<List<Orderbook.Types.PriceVolume>>(orderbook.Bids));
                    _orderbookStream.WriteToStream(item, orderbook.AssetPairId);
                }
            });

            _balanceReader.SubscribeToChanges(balances =>
            {
                var balancesByWallet = balances.GroupBy(x => x.WalletId);

                foreach (var walletBalanes in balancesByWallet)
                {
                    var balanceUpdate = new BalanceUpdate();
                    balanceUpdate.Balances.AddRange( _mapper.Map<List<Balance>>(walletBalanes.ToList()));
                    _balanceStream.WriteToStream(balanceUpdate, walletBalanes.Key);
                }
            });

            _orderReader.SubscribeToChanges(ordersEntities =>
            {
                var ordersByWallet = ordersEntities.GroupBy(x => x.WalletId);

                foreach (var walletOrders in ordersByWallet)
                {
                    var orderUpdate = new OrderUpdate();
                    var orders = walletOrders.ToList();

                    foreach (var order in orders)
                    {
                        var updateOrder = _mapper.Map<Order>(order);
                        updateOrder.Trades.AddRange(_mapper.Map<List<Trade>>(order.Trades));
                        orderUpdate.Orders.Add(updateOrder);
                    }

                    _orderStream.WriteToStream(orderUpdate, walletOrders.Key);
                }
            });

            _tradeReader.SubscribeToChanges(tradeEntities =>
            {
                var tradesByWallet = tradeEntities.GroupBy(x => x.WalletId);

                foreach (var walletTrades in tradesByWallet)
                {
                    var tradeUpdate = new TradeUpdate();

                    tradeUpdate.Trades.AddRange(_mapper.Map<List<Trade>>(walletTrades.ToList()));
                    _tradeStream.WriteToStream(tradeUpdate, walletTrades.Key);
                }
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
