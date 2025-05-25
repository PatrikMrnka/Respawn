using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    /// <summary>
    /// Represents a vote cast by a user for a specific option in a poll.
    /// </summary>
    public class PollVote
    {
        /// <summary>
        /// Primary key identifier for the poll vote.
        /// </summary>
        [Key]
        [MaxLength(36)]
        public string VoteId { get; set; } = Guid.NewGuid().ToString(); // primary key

        /// <summary>
        /// Foreign key referencing the user who cast the vote.
        /// </summary>
        [Required]
        public string UserId { get; set; } // foreign key to UserProfile

        /// <summary>
        /// Foreign key referencing the poll option selected by the user.
        /// </summary>
        [Required]
        public string OptionId { get; set; } // foreign key to PollOption

        /// <summary>
        /// Foreign key referencing the poll in which the vote was cast.
        /// </summary>
        [Required]
        public string PollId { get; set; } // foreign key to Poll

        /// <summary>
        /// The UTC timestamp when the vote was cast.
        /// </summary>
        [Required]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow; // timestamp of the vote

        // navigation properties

        /// <summary>
        /// Navigation property to the user who cast the vote.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public UserProfile? User { get; set; } // navigation property to UserProfile

        /// <summary>
        /// Navigation property to the poll option selected by the user.
        /// </summary>
        [ForeignKey(nameof(OptionId))]
        public PollOption? Option { get; set; } // navigation property to PollOption

        /// <summary>
        /// Navigation property to the poll in which the vote was cast.
        /// </summary>
        [ForeignKey(nameof(PollId))]
        public Poll? Poll { get; set; } // navigation property to Poll
    }
}
