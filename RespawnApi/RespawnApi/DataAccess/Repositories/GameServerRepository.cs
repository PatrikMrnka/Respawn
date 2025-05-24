// DataAccess/Repositories/GameServerRepository.cs
using Microsoft.EntityFrameworkCore;
using RespawnApi.Data;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return await _context.GameServers.AsNoTracking().ToListAsync(); // Přidáno AsNoTracking pro operace pouze pro čtení
        }

        public async Task AddAsync(GameServer gameServer)
        {
            if (gameServer == null) throw new ArgumentNullException(nameof(gameServer));
            await _context.GameServers.AddAsync(gameServer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(GameServer gameServer)
        {
            if (gameServer == null) throw new ArgumentNullException(nameof(gameServer));
            // Ujistěte se, že entita je sledována, pokud byla načtena v jiném kontextu nebo AsNoTracking
            var existingServer = await _context.GameServers.FindAsync(gameServer.GameServerId);
            if (existingServer != null)
            {
                _context.Entry(existingServer).CurrentValues.SetValues(gameServer);
            }
            else
            {
                _context.GameServers.Update(gameServer); // Pokud není sledována, Update ji začne sledovat jako Modified
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid gameServerId)
        {
            var gameServer = await GetByIdAsync(gameServerId); // Použije sledovanou entitu, pokud je to možné
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
