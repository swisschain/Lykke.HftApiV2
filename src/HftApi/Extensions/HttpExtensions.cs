using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace HftApi.Extensions
{
    public static class HttpExtensions
    {
        public static async Task<string> GetBodyAsync(this HttpRequest request)
        {
            if (request.Method != "POST" || request.Protocol == "HTTP/2")
                return null;

            request.EnableBuffering();
            string body;

            using (var reader = new StreamReader(request.BodyReader.AsStream(true), Encoding.UTF8, true, 1024, true))
            {
                body = await reader.ReadToEndAsync();
            }

            request.Body.Seek(0, SeekOrigin.Begin);

            return body;
        }

        public static ILogger GetEnrichLogger(this HttpContext context, string body)
        {
            var clientId = context.User.GetClientId();
            var walletId = context.User.GetWalletId();
            var keyId = context.User.GetKeyId();

            var logger = Log
                .ForContext("KeyId", keyId)
                .ForContext("ClientId", clientId)
                .ForContext("WalletId", walletId);

            if (!string.IsNullOrEmpty(body))
                logger = logger.ForContext("RequestBody", body);

            return logger;
        }
    }
}
