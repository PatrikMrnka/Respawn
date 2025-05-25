using Microsoft.EntityFrameworkCore;
using RespawnApi.Data;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.DataAccess.Interfaces;

namespace RespawnApi.DataAccess.Repositories
{
    public class GameServerRepository : IGameServerRepository
    {
        private readonly RespawnDbContext _context;

        public GameServerRepository(RespawnDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<GameServer?> GetByIdAsync(Guid gameServerId)
        {
            return await _context.GameServers.FindAsync(gameServerId);
        }

        public async Task<IEnumerable<GameServer>> GetAllAsync()
        {
            return await _context.GameServers.AsNoTracking().ToListAsync();
        }

        public async Task AddAsync(GameServer gameServer)
        {
            if (gameServer == null)
            {
                throw new ArgumentNullException(nameof(gameServer));
            }

            await _context.GameServers.AddAsync(gameServer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GameServer gameServer)
        {
            if (gameServer == null)
            {
                throw new ArgumentNullException(nameof(gameServer));
            }

            var existingServer = await _context.GameServers.FindAsync(gameServer.GameServerId);
            if (existingServer != null)
            {
                _context.Entry(existingServer).CurrentValues.SetValues(gameServer);
            }
            else
            {
                _context.GameServers.Update(gameServer); // if it was not found, update it
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid gameServerId)
        {
            var gameServer = await GetByIdAsync(gameServerId); // use GetByIdAsync to ensure we have the entity
            if (gameServer != null)
            {
                _context.GameServers.Remove(gameServer);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<GameServer>> GetServersByStatusesAsync(IEnumerable<ServerStatus> statuses)
        {
            if (statuses == null || !statuses.Any())
            {
                return Enumerable.Empty<GameServer>();
            }

            return await _context.GameServers
                .Where(s => statuses.Contains(s.Status))
                .AsNoTracking() // Přidáno AsNoTracking
                .ToListAsync();
        }
    }
}