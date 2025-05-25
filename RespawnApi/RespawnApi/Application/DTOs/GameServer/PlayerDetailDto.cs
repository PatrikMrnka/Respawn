namespace RespawnApi.Application.DTOs.GameServer
{
    /// <summary>
    /// Represents the data transfer object for detailed information about a player on a game server.
    /// </summary>
    public class PlayerDetailDto
    {
        /// <summary>
        /// The unique nickname or identifier of the player.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The score or points achieved by the player in the game.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// The duration of the player's session on the server, measured in seconds.
        /// </summary>
        public float Duration { get; set; }
    }
}
