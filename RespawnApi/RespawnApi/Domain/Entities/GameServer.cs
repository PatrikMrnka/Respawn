using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RespawnApi.Domain.Enums;

namespace RespawnApi.Domain.Entities
{
    public class GameServer
    {
        [Key]
        [MaxLength(36)]
        public string ServerId { get; set; } = Guid.NewGuid().ToString(); // primary key

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // name of the server

        [Required]
        public GameType GameType { get; set; } // type of the game

        [Required]
        [MaxLength(100)]
        public string DockerImage { get; set; } // Docker image for the server

        [Required]
        [MaxLength(100)]
        public string? ContainerId { get; set; } // Docker container ID

        [Required]
        public ServerStatus Status { get; set; } = ServerStatus.NOTINSTALLED; // status of the server

        [Column(TypeName = "json")]
        public string? Metrics { get; set; } // JSON metrics data

        [Column(TypeName = "json")]
        public string? PortMappings { get; set; } // JSON port mappings

        [Column(TypeName = "json")]
        public string? Configuration { get; set; } // JSON configuration data

        public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>(); // navigation property to GameSession
        public ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>(); // navigation property to PlayerStats
    }
}
