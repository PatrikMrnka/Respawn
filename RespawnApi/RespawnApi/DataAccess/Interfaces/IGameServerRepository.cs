using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Interfaces
{
    public interface IGameServerRepository
    {
        Task<GameServer?> GetGameServerByIdAsync(string serverId);
        Task<IEnumerable<GameServer>> GetAllGameServersAsync();
        Task AddGameServerAsync(GameServer gameServer);
        Task UpdateGameServerAsync(GameServer gameServer);
        Task DeleteGameServerAsync(string serverId);
    }
}
