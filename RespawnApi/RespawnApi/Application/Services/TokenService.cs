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

            var userRoles = await _userManager.GetRolesAsync(user); // This line already exists and is correct

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("uid", user.Id)
            };

            foreach (var userRole in userRoles) // This loop already exists and is correct
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
                    Nickname = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    AvatarUrl = userProfile?.AvatarUrl,
                    Roles = userRoles
                },
                ExpiresAt = tokenDescriptor.Expires
            };
        }
    }
}
