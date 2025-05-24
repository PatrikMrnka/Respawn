// Domain/Entities/GameServer.cs
using RespawnApi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RespawnApi.Domain.Entities
{
    public class GameServer
    {
        [Key]
        public Guid GameServerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public GameType GameType { get; set; }

        [Required]
        public ServerStatus Status { get; set; } // Celkový/kontejnerový stav

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        public int? Port { get; set; }

        [MaxLength(255)]
        public string? ContainerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? StatusDetails { get; set; } // Doplňující informace, např. chybové hlášky

        // Navigation properties
        public virtual ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
        public virtual ICollection<PlayerStats> PlayerStats { get; set; } = new List<PlayerStats>();
    }
}