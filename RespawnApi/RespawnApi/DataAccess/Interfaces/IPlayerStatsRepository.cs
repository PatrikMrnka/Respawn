using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Interfaces
{
    public interface IPlayerStatsRepository
    {
        Task AddAsync(PlayerStats stats);
        Task<IEnumerable<PlayerStats>> GetStatsByUserAsync(string userId, string? serverId = null);
        Task<IEnumerable<PlayerStats>> GetLeaderboardAsync(string serverId, int topN);
    }
}
