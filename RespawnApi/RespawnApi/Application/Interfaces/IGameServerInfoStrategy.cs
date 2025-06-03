using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for a strategy to get detailed information about a game server.
    /// </summary>
    public interface IGameServerInfoStrategy
    {
        /// <summary>
        /// Gets the game type supported by this strategy.
        /// </summary>
        GameType SupportedGameType { get; }

        /// <summary>
        /// Asynchronously gets detailed information about a game server.
        /// </summary>
        /// <param name="serverEntity">The game server entity from the database.</param>
        /// <param name="basicDto">A basic DTO of the server entity, can be used for initial data.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="GameServerDetailDto"/> with detailed server information,
        /// or a DTO with basic info and error details if querying fails.
        /// Returns null only if a fundamental error occurs in the strategy itself.
        /// </returns>
        Task<GameServerDetailDto?> GetServerDetailsAsync(GameServer serverEntity, GameServerDto basicDto);
    }
}
