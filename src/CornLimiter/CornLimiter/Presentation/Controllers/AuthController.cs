using CornLimiter.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CornLimiter.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController( IOptions<JwtTokenOptions> options) : ControllerBase
    {
        [HttpGet("Token")]
        public IActionResult GetToken()
        {
            var token = GenerateJwtToken();

            return Ok(new { Token = token });
        }

        private string GenerateJwtToken()
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, "fakeUser"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: options.Value.Issuer,
                audience: options.Value.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(options.Value.ExpiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
