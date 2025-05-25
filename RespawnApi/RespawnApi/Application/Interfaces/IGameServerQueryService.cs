using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that orchestrates querying game server details using appropriate strategies.
    /// </summary>
    public interface IGameServerQueryService
    {
        /// <summary>
        /// Asynchronously gets detailed information about a game server by selecting and executing the appropriate strategy.
        /// </summary>
        /// <param name="serverEntity">The game server entity from the database.</param>
        /// <param name="basicDto">A basic DTO projection of the server entity.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="GameServerDetailDto"/> with detailed server information,
        /// or a DTO with basic info and error details if querying fails.
        /// </returns>
        Task<GameServerDetailDto?> GetServerDetailsAsync(GameServer serverEntity, GameServerDto basicDto);
    }
}