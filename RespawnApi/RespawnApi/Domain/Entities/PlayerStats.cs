using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    /// <summary>
    /// Represents player statistics for a specific user on a game server, optionally within a game session.
    /// </summary>
    public class PlayerStats
    {
        /// <summary>
        /// Primary key identifier for the player stats entry.
        /// </summary>
        [Key]
        public Guid StatsId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Foreign key referencing the user profile.
        /// </summary>
        [Required]
        public string UserId { get; set; } // foreign key to UserProfile

        /// <summary>
        /// Foreign key referencing the game server.
        /// </summary>
        [Required]
        public Guid ServerId { get; set; }

        /// <summary>
        /// Optional foreign key referencing the game session.
        /// </summary>
        public Guid? GameSessionId { get; set; } 

        /// <summary>
        /// JSON-serialized string containing the player's statistics data.
        /// </summary>
        [Required]
        [Column(TypeName = "json")]
        public string Data { get; set; } = string.Empty;

        // navigation properties

        /// <summary>
        /// Navigation property to the associated user profile.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual UserProfile? User { get; set; }

        /// <summary>
        /// Navigation property to the associated game server.
        /// </summary>
        [ForeignKey(nameof(ServerId))]
        public virtual GameServer? Server { get; set; }

        /// <summary>
        /// Navigation property to the associated game session.
        /// </summary>
        [ForeignKey(nameof(GameSessionId))]
        public virtual GameSession? GameSession { get; set; }
    }
}