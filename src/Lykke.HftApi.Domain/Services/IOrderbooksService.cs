using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;

namespace Lykke.HftApi.Domain.Services
{
    public interface IOrderbooksService
    {
        Task<IReadOnlyCollection<Orderbook>> GetAsync(IEnumerable<string> assetPairIds, int? depth = 0);
        Orderbook GetOrderbookUpdates(Orderbook oldOrderbook, Orderbook newOrderbook);
    }
}
