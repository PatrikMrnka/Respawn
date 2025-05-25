using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;

namespace RespawnApi.DataAccess.Interfaces
{
    /// <summary>
    /// Repository interface for managing GameServer entities.
    /// Provides methods for CRUD operations and querying by server status.
    /// </summary>
    public interface IGameServerRepository
    {
        /// <summary>
        /// Retrieves a GameServer by its unique identifier.
        /// </summary>
        /// <param name="gameServerId">The unique identifier of the GameServer.</param>
        /// <returns>The GameServer entity if found; otherwise, null.</returns>
        Task<GameServer?> GetByIdAsync(Guid gameServerId);

        /// <summary>
        /// Retrieves all GameServer entities.
        /// </summary>
        /// <returns>An enumerable of all GameServer entities.</returns>
        Task<IEnumerable<GameServer>> GetAllAsync();

        /// <summary>
        /// Adds a new GameServer entity to the data store.
        /// </summary>
        /// <param name="gameServer">The GameServer entity to add.</param>
        Task AddAsync(GameServer gameServer);

        /// <summary>
        /// Updates an existing GameServer entity in the data store.
        /// </summary>
        /// <param name="gameServer">The GameServer entity to update.</param>
        Task UpdateAsync(GameServer gameServer);

        /// <summary>
        /// Deletes a GameServer entity by its unique identifier.
        /// </summary>
        /// <param name="gameServerId">The unique identifier of the GameServer to delete.</param>
        Task DeleteAsync(Guid gameServerId);

        /// <summary>
        /// Retrieves GameServer entities filtered by their statuses.
        /// </summary>
        /// <param name="statuses">A collection of ServerStatus values to filter by.</param>
        /// <returns>An enumerable of GameServer entities matching the specified statuses.</returns>
        Task<IEnumerable<GameServer>> GetServersByStatusesAsync(IEnumerable<ServerStatus> statuses);
    }
}