using System.Threading.Tasks;

namespace Lykke.HftApi.Domain.Services
{
    public interface ITokenService
    {
        Task InitAsync();
        bool IsValid(string id);
        void Add(string id);
        void Remove(string id);
    }
}
