using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace RespawnApi.Domain.Entities
{
    /// <summary>
    /// Represents a poll entity with question, options, votes, and related metadata.
    /// </summary>
    public class Poll
    {
        /// <summary>
        /// Primary key identifier for the poll.
        /// </summary>
        [Key]
        public string PollId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The question being asked in the poll.
        /// </summary>
        [Required]
        public string Question { get; set; }

        /// <summary>
        /// The date and time when the poll ends.
        /// </summary>
        [Required]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Indicates whether the poll is closed.
        /// </summary>
        public bool IsClosed { get; set; } = false;

        /// <summary>
        /// Optional URL to an image associated with the poll.
        /// </summary>
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Foreign key referencing the user who created the poll.
        /// </summary>
        [Required]
        public string CreatorUserId { get; set; }

        /// <summary>
        /// Indicates if the poll allows multiple choices.
        /// </summary>
        [Required]
        public bool IsMultipleChoice { get; set; } = false;

        // navigation properties

        /// <summary>
        /// Navigation property to the creator's user profile.
        /// </summary>
        [ForeignKey(nameof(CreatorUserId))]
        public UserProfile Creator { get; set; }

        /// <summary>
        /// Collection of options available in the poll.
        /// </summary>
        public ICollection<PollOption> PollOptions { get; set; } = new List<PollOption>();

        /// <summary>
        /// Collection of votes cast in the poll.
        /// </summary>
        public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();
    }
}
