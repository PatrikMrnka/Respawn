using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    /// <summary>
    /// Represents an option within a poll, including its text, optional image, and related votes.
    /// </summary>
    public class PollOption
    {
        /// <summary>
        /// Primary key identifier for the poll option.
        /// </summary>
        [Key]
        [MaxLength(36)]
        public string OptionId { get; set; } = Guid.NewGuid().ToString(); // primary key

        /// <summary>
        /// The display text for the poll option.
        /// </summary>
        [Required]
        public string Text { get; set; }

        /// <summary>
        /// Optional URL to an image associated with the poll option.
        /// </summary>
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Foreign key referencing the poll to which this option belongs.
        /// </summary>
        [Required]
        public string PollId { get; set; } // foreign key to Poll

        // navigation properties

        /// <summary>
        /// Navigation property to the parent poll.
        /// </summary>
        [ForeignKey(nameof(PollId))]
        public Poll Poll { get; set; }

        /// <summary>
        /// Collection of votes associated with this poll option.
        /// </summary>
        public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>(); // navigation property to PollVote
    }
}
