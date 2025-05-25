using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.DTOs.GameServer
{
    /// <summary>
    /// Represents the data transfer object for updating the status of a game server.
    /// </summary>
    public class GameServerStatusUpdateDto
    {
        /// <summary>
        /// The unique identifier for the game server, typically a GUID.
        /// </summary>
        public Guid GameServerId { get; set; }

        /// <summary>
        /// The new overall status of the game server, indicating whether it is running, stopped, or in an error state.
        /// </summary>
        public ServerStatus NewOverallStatus { get; set; }

        /// <summary>
        /// The specific details about the status of the game server, which may include error messages or additional information.
        /// </summary>
        public string? StatusDetails { get; set; }

        /// <summary>
        /// The error message associated with the game server status update, if any.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
