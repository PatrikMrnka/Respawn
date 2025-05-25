using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    /// <summary>
    /// Represents a session of a game on a specific server, including session timing, map, and player statistics.
    /// </summary>
    public class GameSession
    {
        /// <summary>
        /// Primary key identifier for the game session.
        /// </summary>
        [Key]
        public Guid GameSessionId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Foreign key referencing the associated game server.
        /// </summary>
        [Required]
        public Guid GameServerId { get; set; }

        /// <summary>
        /// The UTC date and time when the session started.
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The UTC date and time when the session ended, or null if still active.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Name of the map played during the session (optional, max 100 chars).
        /// </summary>
        [MaxLength(100)]
        public string? MapName { get; set; }

        /// <summary>
        /// Maximum number of players allowed in the session.
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// Current number of players in the session.
        /// </summary>
        public int CurrentPlayers { get; set; }

        // Navigation properties

        /// <summary>
        /// Navigation property to the associated game server.
        /// </summary>
        [ForeignKey(nameof(GameServerId))]
        public virtual GameServer? Server { get; set; }

        /// <summary>
        /// Collection of player statistics for players in this session.
        /// </summary>
        public virtual ICollection<PlayerStats> PlayerStatsInSession { get; set; } = new List<PlayerStats>();
    }
}