// Domain/Entities/GameSession.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    public class GameSession
    {
        [Key]
        public Guid GameSessionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid GameServerId { get; set; } // Cizí klíč pro GameServer - MUSÍ BÝT Guid

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public DateTime? EndTime { get; set; }

        // Další vlastnosti session, např. název mapy, počet hráčů atd.
        [MaxLength(100)]
        public string? MapName { get; set; }

        public int MaxPlayers { get; set; }

        public int CurrentPlayers { get; set; }

        // Navigation properties
        [ForeignKey(nameof(GameServerId))] // Atribut ForeignKey odkazuje na vlastnost GameServerId
        public virtual GameServer? Server { get; set; } // Název navigační vlastnosti může být 'Server' nebo 'GameServer'
        public virtual ICollection<PlayerStats> PlayerStatsInSession { get; set; } = new List<PlayerStats>();
    }
}