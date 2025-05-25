namespace RespawnApi.Application.DTOs.GameServer
{
    /// <summary>
    /// Represents the data transfer object for detailed information about a game server.
    /// </summary>
    public class GameServerDetailDto : GameServerDto
    {
        /// <summary>
        /// The name of the game associated with the server, e.g., "Counter-Strike: Global Offensive".
        /// </summary>
        public string? GameName { get; set; }

        /// <summary>
        /// The name of the map currently being played on the server, e.g., "de_dust2".
        /// </summary>
        public string? MapName { get; set; }

        /// <summary>
        /// The maximum number of players allowed on the server.
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// The current number of players on the server.
        /// </summary>
        public int CurrentPlayers { get; set; }

        /// <summary>
        /// Indicates whether the server is secured with VAC (Valve Anti-Cheat).
        /// </summary>
        public bool IsVacSecured { get; set; }

        /// <summary>
        /// The date and time when the server was last updated, typically in UTC format.
        /// </summary>
        public List<PlayerDetailDto> Players { get; set; } = new List<PlayerDetailDto>();
    }
}
