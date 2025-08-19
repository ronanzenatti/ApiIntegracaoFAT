// Controllers/AuthController.cs
using ApiIntegracao.DTOs.Login;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiIntegracao.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto loginRequest)
        {
            // --- Lógica de Validação ---
            // Em um cenário real, você validaria o ClientId e ClientSecret
            // consultando um banco de dados ou outra fonte segura.
            // Para este exemplo, vamos usar valores fixos do appsettings.json
            var validClientId = _configuration["PortalFatCredentials:ClientId"];
            var validClientSecret = _configuration["PortalFatCredentials:ClientSecret"];

            if (loginRequest.ClientId == validClientId && loginRequest.ClientSecret == validClientSecret)
            {
                var token = GenerateJwtToken();
                return Ok(token);
            }

            return Unauthorized(new LoginResponseDto
            {
                Authenticated = false,
                Message = "Credenciais inválidas."
            });
        }

        private LoginResponseDto GenerateJwtToken()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"]));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "PortalFAT_Client"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, "PortalFAT_Client")
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return new LoginResponseDto
            {
                Authenticated = true,
                Token = tokenHandler.WriteToken(token),
                Expiration = expiry,
                Message = "Token gerado com sucesso."
            };
        }
    }
}