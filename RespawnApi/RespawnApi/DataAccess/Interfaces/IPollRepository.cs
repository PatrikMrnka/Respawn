using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Interfaces
{
    public interface IPollRepository
    {
        Task<Poll?> GetByIdAsync(string pollId);
        Task<Poll?> GetByIdWithOptionsAndVotesAsync(string pollId);
        Task<IEnumerable<Poll>> GetAllActiveAsync();
        Task AddAsync(Poll poll);
        Task UpdateAsync(Poll poll);
        Task DeleteAsync(string pollId);
    }
}
