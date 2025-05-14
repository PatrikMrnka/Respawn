using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Interfaces
{
    public interface IVoteRepository
    {
        Task AddAsync(PollVote vote);
        Task<bool> HasUserVotedAsync(string pollId, string userId);
        Task<IEnumerable<PollVote>> GetVotesByPollAsync(string pollId);
    }
}
