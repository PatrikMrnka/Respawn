using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Domain.Entities
{
    /// <summary>
    /// Represents a user profile containing user-specific information and navigation properties to related entities.
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Primary key and foreign key to AspNetUsers table.
        /// </summary>
        [Key]
        public string UserId { get; set; } // primary key + foreign key to AspNetUsers

        /// <summary>
        /// The user's display nickname.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Nickname { get; set; }

        /// <summary>
        /// URL to the user's avatar image (optional).
        /// </summary>
        [MaxLength(500)]
        public string? AvatarUrl { get; set; } // URL to the avatar image

        // navigation properties

        /// <summary>
        /// Collection of polls created by the user.
        /// </summary>
        public ICollection<Poll> CreatedPolls { get; set; } = new List<Poll>(); // navigation property to Poll

        /// <summary>
        /// Collection of poll votes cast by the user.
        /// </summary>
        public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>(); // navigation property to PollVote

        /// <summary>
        /// Collection of game sessions the user has participated in.
        /// </summary>
        public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>(); // navigation property to GameSession

        /// <summary>
        /// Collection of player statistics associated with the user.
        /// </summary>
        public ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>(); // navigation property to PlayerStats
    }
}
