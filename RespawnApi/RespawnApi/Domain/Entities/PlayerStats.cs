// Domain/Entities/PlayerStats.cs
using System; // Přidáno pro Guid
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    public class PlayerStats
    {
        [Key]
        public Guid StatsId { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } // foreign key to UserProfile

        [Required]
        public Guid ServerId { get; set; } // foreign key to GameServer - Přejmenováno z GameServerId pro konzistenci s [ForeignKey(nameof(ServerId))]

        // [MaxLength(36)] // Odstraněno, protože Guid nemá MaxLength v tomto kontextu
        public Guid? GameSessionId { get; set; } // foreign key to GameSession - ZMĚNĚNO NA Guid?

        [Required]
        [Column(TypeName = "json")]
        public string Data { get; set; } = string.Empty; // Inicializace pro non-nullable string

        // navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual UserProfile? User { get; set; }

        [ForeignKey(nameof(ServerId))] // Odkazuje na vlastnost ServerId
        public virtual GameServer? Server { get; set; } // Navigační vlastnost k GameServer

        [ForeignKey(nameof(GameSessionId))]
        public virtual GameSession? GameSession { get; set; }
    }
}