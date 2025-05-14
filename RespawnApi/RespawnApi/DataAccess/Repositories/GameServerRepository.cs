using Microsoft.EntityFrameworkCore;
using RespawnApi.Data;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Repositories
{
    public class GameServerRepository : IGameServerRepository
    {
        private readonly RespawnDbContext _context;

        public GameServerRepository(RespawnDbContext context)
        {
            _context = context;
        }

        public Task AddGameServerAsync(GameServer gameServer)
        {
            _context.GameServers.Add(gameServer);
            return _context.SaveChangesAsync();
        }

        public async Task DeleteGameServerAsync(string serverId)
        {
            var server = await GetGameServerByIdAsync(serverId);

            if (server != null)
            {
                _context.GameServers.Remove(server);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<GameServer>> GetAllGameServersAsync()
        {
            return await _context.GameServers
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<GameServer?> GetGameServerByIdAsync(string serverId)
        {
            return await _context.GameServers
                .FindAsync(serverId);
        }

        public Task<IEnumerable<PlayerStats>> GetPlayerStatsBySessionIdAsync(string sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<GameSession>> GetSessionsByServerIdAsync(string serverId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateGameServerAsync(GameServer gameServer)
        {
            throw new NotImplementedException();
        }
    }
}
