using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Domain.Entities
{
    public class PollOption
    {
        [Key]
        [MaxLength(36)]
        public string OptionId { get; set; } = Guid.NewGuid().ToString(); // primary key

        [Required]
        public string Text { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public string PollId { get; set; } // foreign key to Poll

        // navigation properties
        public Poll Poll { get; set; }
        public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>(); // navigation property to PollVote
    }
}
