using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RespawnApi.Application.DTOs.Auth;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RespawnApi.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public TokenService(IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<AuthResponseDto> GenerateTokenAsync(IdentityUser user, UserProfile? userProfile)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]
                ?? throw new InvalidOperationException("JWT Key not found in configuration for TokenService."));

            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id), // Subject (user ID)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty), // Ukládáme Nickname jako Name
                new Claim("uid", user.Id) // Další identifikátor uživatele, pokud je potřeba
            };

            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"] ?? "60")),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var stringToken = tokenHandler.WriteToken(token);

            return new AuthResponseDto
            {
                Token = stringToken,
                IsSuccess = true,
                Message = "Token byl úspěšně vygenerován.",
                UserInfo = new UserDto
                {
                    Id = user.Id,
                    Nickname = user.UserName ?? string.Empty, // Nickname je UserName v IdentityUser
                    Email = user.Email ?? string.Empty,
                    AvatarUrl = userProfile?.AvatarUrl,
                    Roles = userRoles
                },
                ExpiresAt = tokenDescriptor.Expires
            };
        }
    }
}
