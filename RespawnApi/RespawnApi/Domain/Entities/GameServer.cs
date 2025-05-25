using RespawnApi.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Domain.Entities
{
    /// <summary>
    /// Represents a game server entity, including its configuration, status, and related sessions and player stats.
    /// </summary>
    public class GameServer
    {
        /// <summary>
        /// Primary key identifier for the game server.
        /// </summary>
        [Key]
        public Guid GameServerId { get; set; }

        /// <summary>
        /// Name of the game server.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of game hosted on this server.
        /// </summary>
        [Required]
        public GameType GameType { get; set; }

        /// <summary>
        /// Current status of the server (e.g., Online, Offline, Starting).
        /// </summary>
        [Required]
        public ServerStatus Status { get; set; }

        /// <summary>
        /// IP address of the server (optional).
        /// </summary>
        [MaxLength(100)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Port number the server is running on (optional).
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Container ID if the server is running in a containerized environment (optional).
        /// </summary>
        [MaxLength(255)]
        public string? ContainerId { get; set; }

        /// <summary>
        /// Date and time when the server was created (in UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional details about the server's status (optional).
        /// </summary>
        [MaxLength(500)]
        public string? StatusDetails { get; set; }

        // Navigation properties

        /// <summary>
        /// Collection of game sessions associated with this server.
        /// </summary>
        public virtual ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();

        /// <summary>
        /// Collection of player statistics associated with this server.
        /// </summary>
        public virtual ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>();
    }
}