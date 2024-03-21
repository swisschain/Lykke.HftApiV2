using System.Threading.Tasks;

namespace Lykke.HftApi.Domain.Services
{
    public interface IBlockedClientsService
    {
        Task<bool> IsClientBlocked(string clientId);
    }
}
