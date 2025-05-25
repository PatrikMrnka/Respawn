using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.DTOs.GameServer
{
    /// <summary>
    /// Represents the data transfer object for basic information about a game server.
    /// </summary>
    public class GameServerDto
    {
        /// <summary>
        /// The unique identifier for the game server, typically a GUID.
        /// </summary>
        public Guid GameServerId { get; set; }

        /// <summary>
        /// The name of the game server, which is typically a human-readable identifier.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The type of game associated with the server, such as "Counter-Strike: Global Offensive" or "Minecraft".
        /// </summary>
        public GameType GameType { get; set; }

        /// <summary>
        /// The status of the game server, indicating whether it is running, stopped, or in an error state.
        /// </summary>
        public ServerStatus Status { get; set; } // container status

        /// <summary>
        /// The IP address of the game server, which are used to connect to the server.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// The port number on which the game server is listening for connections.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// The unique identifier of the Docker container associated with the game server, typically a GUID.
        /// </summary>
        public string? ContainerId { get; set; }

        /// <summary>
        /// The date and time when the game server was created, typically in UTC format.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The specific details about the status of the game server, which may include error messages or additional information.
        /// </summary>
        public string? StatusDetails { get; set; }
    }
}