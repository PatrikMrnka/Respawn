using Microsoft.AspNetCore.Identity;
using RespawnApi.Application.DTOs.Auth;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Interface for token generation services.
    /// </summary>
    public interface ITokenService
    {

        /// <summary>
        /// Generates a JWT token for the specified user and user profile.
        /// </summary>
        /// <param name="user">The identity user for whom the token is generated.</param>
        /// <param name="userProfile">The user profile associated with the identity user, or null if not available.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an <see cref="AuthResponseDto"/> with the generated token and authentication details.
        /// </returns>
        Task<AuthResponseDto> GenerateTokenAsync(IdentityUser user, UserProfile? userProfile);
    }
}
