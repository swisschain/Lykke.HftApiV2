using System.Threading.Tasks;
using HftApi.Extensions;
using Lykke.HftApi.Domain.Services;
using Microsoft.AspNetCore.Http;

namespace HftApi.Middleware
{
    public class KeyCheckMiddleware
    {
        private readonly ITokenService _tokenService;
        private readonly RequestDelegate _next;

        public KeyCheckMiddleware(
            ITokenService tokenService,
            RequestDelegate next)
        {
            _tokenService = tokenService;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string id = context.User.GetKeyId();

            if (!string.IsNullOrEmpty(id) && !_tokenService.IsValid(id))
            {
                await UnauthorizedResponse(context);
                return;
            }

            await _next.Invoke(context);
        }

        private Task UnauthorizedResponse(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return ctx.Response.WriteAsync("");
        }
    }
}
