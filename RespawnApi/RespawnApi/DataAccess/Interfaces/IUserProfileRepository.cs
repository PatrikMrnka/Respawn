using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Interfaces
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> GetByUserIdAsync(string userId);
        Task AddAsync(UserProfile profile);
        Task UpdateAsync(UserProfile profile);
    }
}
