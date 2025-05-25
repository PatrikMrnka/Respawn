using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Interfaces
{
    /// <summary>
    /// Repository interface for managing user profiles in the data store.
    /// </summary>
    public interface IUserProfileRepository
    {
        /// <summary>
        /// Retrieves a user profile by the associated user ID.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The user profile if found; otherwise, null.</returns>
        Task<UserProfile?> GetByUserIdAsync(string userId);

        /// <summary>
        /// Adds a new user profile to the data store.
        /// </summary>
        /// <param name="profile">The user profile to add.</param>
        Task AddAsync(UserProfile profile);

        /// <summary>
        /// Updates an existing user profile in the data store.
        /// </summary>
        /// <param name="profile">The user profile to update.</param>
        Task UpdateAsync(UserProfile profile);
    }
}