using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HftApi.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HftApi.WebApi
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthConfig _authConfig;

        public AuthController(AuthConfig authConfig)
        {
            _authConfig = authConfig;
        }

        [HttpGet]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        public IActionResult Get([FromQuery]string clientId, [FromQuery]string walletId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authConfig.JwtSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "wallet name"),
                    new Claim(JwtRegisteredClaimNames.Aud, _authConfig.LykkeAud),
                    new Claim("client-id", clientId),
                    new Claim("wallet-id", walletId),
                }),
                Expires = DateTime.UtcNow.AddYears(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new {token = tokenHandler.WriteToken(token)});
        }
    }
}
