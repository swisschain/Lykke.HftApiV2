using System.Linq;
using System.Security.Claims;

namespace HftApi.Extensions
{
    public static class UserExxtensions
    {
        public static string GetClientId(this ClaimsPrincipal user)
        {
            return user.Identities
                .SelectMany(x => x.Claims)
                .Where(c => c.Type == "client-id")
                .Select(x => x.Value)
                .SingleOrDefault();
        }

        public static string GetWalletId(this ClaimsPrincipal user)
        {
            return user.Identities
                .SelectMany(x => x.Claims)
                .Where(c => c.Type == "wallet-id")
                .Select(x => x.Value)
                .SingleOrDefault();
        }

        public static string GetKeyId(this ClaimsPrincipal user)
        {
            return user.Identities
                .SelectMany(x => x.Claims)
                .Where(c => c.Type == "key-id")
                .Select(x => x.Value)
                .SingleOrDefault();
        }
    }
}
