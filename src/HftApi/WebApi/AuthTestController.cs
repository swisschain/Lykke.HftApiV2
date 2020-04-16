using HftApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HftApi.WebApi
{
    [Authorize]
    [ApiController]
    [Route("api/auth-test")]
    public class AuthTestController : ControllerBase
    {
        [HttpGet]
        public ActionResult Test()
        {
            var clientId = User.GetClientId();
            var walletId = User.GetWalletId();

            return Ok(new {clientId, walletId});
        }
    }
}
