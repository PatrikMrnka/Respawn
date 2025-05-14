using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Domain.Entities
{
    public class UserProfile
    {
        [Key] 
        public string UserId { get; set; } // primary key + foreign key to AspNetUsers

        [Required]
        [MaxLength(100)]
        public string Nickname { get; set; }

        [MaxLength(500)]
        public string AvatarUrl { get; set; } // URL to the avatar image

        // navigation properties

        public ICollection<Poll> CreatedPolls { get; set; } = new List<Poll>(); // navigation property to Poll
        public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>(); // navigation property to PollVote
        public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>(); // navigation property to GameSession
        public ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>(); // navigation property to PlayerStats

        // UserProfile : IdentityUser navigation property defined in RespawnDbContext via Fluent API
    }
}
