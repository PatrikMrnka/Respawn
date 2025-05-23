using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace RespawnApi.Domain.Entities
{
    public class Poll
    {
        [Key]
        public string PollId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Question { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public bool IsClosed { get; set; } = false; // poll is closed by default

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public string CreatorUserId { get; set; } // foreign key UserProfile.UserId

        [Required]
        public bool IsMultipleChoice { get; set; } = false; // Nová vlastnost

        // navigation properties
        [ForeignKey(nameof(CreatorUserId))]
        public UserProfile Creator { get; set; } // navigation property to UserProfile

        public ICollection<PollOption> PollOptions { get; set; } = new List<PollOption>(); // navigation property to PollOption
        public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>(); // navigation property to PollVote
    }
}
