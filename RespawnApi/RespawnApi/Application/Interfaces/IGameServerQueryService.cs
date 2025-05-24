using RespawnApi.Application.DTOs.GameServer;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that queries game server details.
    /// </summary>
    public interface IGameServerQueryService
    {
        /// <summary>
        /// Asynchronously gets detailed information about a game server using the A2S protocol.
        /// </summary>
        /// <param name="ipAddress">The IP address of the game server.</param>
        /// <param name="queryPort">The query port of the game server.</param>
        /// <param name="basicServerInfo">Basic information about the server, potentially used for context or fallback.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="GameServerDetailDto"/> with detailed server information,
        /// or null if the query fails or the server does not respond.
        /// </returns>
        Task<GameServerDetailDto?> GetServerDetailsA2SAsync(string ipAddress, int queryPort, GameServerDto basicServerInfo);
    }
}
