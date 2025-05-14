using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    public class GameSession
    {
        [Key]
        [MaxLength(36)]
        public string SessionId { get; set; } = Guid.NewGuid().ToString(); // primary key

        [Required]
        public string ServerId { get; set; } // foreign key to GameServer

        [Required]
        public DateTime StartTime { get; set; } = DateTime.UtcNow; // start time of the session

        [Required]
        public DateTime? EndTime { get; set; } // end time of the session

        [ForeignKey(nameof(ServerId))]
        public GameServer Server { get; set; } // navigation property to GameServer

        public ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>(); // navigation property to PlayerStats
    }
}
