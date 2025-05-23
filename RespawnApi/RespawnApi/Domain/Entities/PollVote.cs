using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    public class PollVote
    {
        [Key]
        [MaxLength(36)]
        public string VoteId { get; set; } = Guid.NewGuid().ToString(); // primary key

        [Required]
        public string UserId { get; set; } // foreign key to UserProfile

        [Required]
        public string OptionId { get; set; } // foreign key to PollOption

        [Required]
        public string PollId { get; set; } // foreign key to Poll

        [Required] public DateTime TimeStamp { get; set; } = DateTime.UtcNow; // timestamp of the vote

        // navigation properties
        [ForeignKey(nameof(UserId))]
        public UserProfile? User { get; set; } // navigation property to UserProfile

        [ForeignKey(nameof(OptionId))]
        public PollOption? Option { get; set; } // navigation property to PollOption

        [ForeignKey(nameof(PollId))]
        public Poll? Poll { get; set; } // navigation property to Poll
    }
}
