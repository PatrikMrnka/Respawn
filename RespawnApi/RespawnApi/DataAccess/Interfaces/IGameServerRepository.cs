// DataAccess/Interfaces/IGameServerRepository.cs
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RespawnApi.DataAccess.Interfaces
{
    public interface IGameServerRepository
    {
        Task<GameServer?> GetByIdAsync(Guid gameServerId);
        Task<IEnumerable<GameServer>> GetAllAsync();
        Task AddAsync(GameServer gameServer);
        Task UpdateAsync(GameServer gameServer);
        Task DeleteAsync(Guid gameServerId);
        Task<IEnumerable<GameServer>> GetServersByStatusesAsync(IEnumerable<ServerStatus> statuses);
    }
}