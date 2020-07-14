using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;

namespace Lykke.HftApi.Domain.Services
{
    public interface IOrderbooksService
    {
        Task<IReadOnlyCollection<Orderbook>> GetAsync(string assetPairId = null, int? depth = 0);
        Orderbook GetOrderbookUpdates(Orderbook oldOrderbook, Orderbook newOrderbook);
    }
}
