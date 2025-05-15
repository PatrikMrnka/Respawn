using Microsoft.AspNetCore.Identity;
using RespawnApi.Application.DTOs.Auth;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Application.Interfaces
{
    public interface ITokenService
    {
        Task<AuthResponseDto> GenerateTokenAsync(IdentityUser user, UserProfile? userProfile);
    }
}
