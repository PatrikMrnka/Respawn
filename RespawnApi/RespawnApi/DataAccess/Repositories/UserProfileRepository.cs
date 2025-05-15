using Microsoft.EntityFrameworkCore;
using RespawnApi.Data;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly RespawnDbContext _context;

        public UserProfileRepository(RespawnDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile?> GetByUserIdAsync(string userId)
        {
            return await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task AddAsync(UserProfile profile)
        {
            await _context.UserProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserProfile profile)
        {
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();
        }
    }
}
