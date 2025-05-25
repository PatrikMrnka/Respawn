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
    /// <summary>
    /// Service for generating JWT tokens for user authentication and authorization.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenService"/> class with the specified configuration and user manager.
        /// </summary>
        /// <param name="configuration">The application configuration instance.</param>
        /// <param name="userManager">The user manager for handling identity users.</param>
        public TokenService(IConfiguration configuration, UserManager<IdentityUser> userManager)

        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<AuthResponseDto> GenerateTokenAsync(IdentityUser user, UserProfile? userProfile)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings"); // get JWT settings from configuraton
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] // get JWT key from configuration
                ?? throw new InvalidOperationException("JWT Key not found in configuration for TokenService."));

            var userRoles = await _userManager.GetRolesAsync(user); // get user roles from UserManager

            var claims = new List<Claim> // create a list of claims for the JWT token
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("uid", user.Id)
            };

            foreach (var userRole in userRoles) // add each user role as a claim
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var tokenDescriptor = new SecurityTokenDescriptor // create the token descriptor
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
