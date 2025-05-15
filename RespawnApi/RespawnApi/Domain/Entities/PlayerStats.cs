using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    public class PlayerStats
    {
        [Key]
        [MaxLength(36)]
        public string StatsId { get; set; } = Guid.NewGuid().ToString(); // primary key

        [Required]
        public string UserId { get; set; } // foreign key to UserProfile

        [Required]
        public string ServerId { get; set; } // foreign key to GameServer

        [MaxLength(36)]
        public string? GameSessionId { get; set; } // foreign key to GameSession

        [Required]
        // JSON
        [Column(TypeName = "json")]
        public string Data { get; set; }

        // navigation properties

        [ForeignKey(nameof(UserId))]
        public UserProfile? User { get; set; } // navigation property to UserProfile

        [ForeignKey(nameof(ServerId))]
        public GameServer? Server { get; set; } // navigation property to GameServer

        [ForeignKey(nameof(GameSessionId))]
        public GameSession? GameSession { get; set; } // navigation property to GameSession
    }
}
