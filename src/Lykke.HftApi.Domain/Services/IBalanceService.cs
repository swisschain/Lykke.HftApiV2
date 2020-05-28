using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.HftApi.Domain.Entities;

namespace Lykke.HftApi.Domain.Services
{
    public interface IBalanceService
    {
        Task<IReadOnlyCollection<Balance>> GetBalancesAsync(string walletId);
    }
}
