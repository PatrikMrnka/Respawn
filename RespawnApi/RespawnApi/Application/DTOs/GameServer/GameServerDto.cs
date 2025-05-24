// Application/DTOs/GameServer/GameServerDto.cs
using System.ComponentModel.DataAnnotations;
using RespawnApi.Domain.Enums; // Pro GameType a ServerStatus

namespace RespawnApi.Application.DTOs.GameServer
{
    public class GameServerDto
    {
        public Guid GameServerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public GameType GameType { get; set; }
        public ServerStatus Status { get; set; } // Celkový/kontejnerový stav
        public string? LgsmServerStatus { get; set; } // Detailní stav z LinuxGSM
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public string? ContainerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StatusDetails { get; set; }
    }

    public class CreateGameServerDto
    {
        [Required(ErrorMessage = "Název serveru je povinný.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Název serveru musí mít 3-100 znaků.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Typ hry je povinný.")]
        public GameType GameType { get; set; }

        [StringLength(255, ErrorMessage = "Extra parametry nesmí být delší než 255 znaků.")]
        public string? AdditionalGsParams { get; set; }
    }
    public class GameServerStatusUpdateDto
    {
        public Guid GameServerId { get; set; }
        public ServerStatus NewOverallStatus { get; set; } // Přejmenováno pro srozumitelnost
        public string? NewLgsmServerStatus { get; set; } // Nový stav z LGSM
        public string? StatusDetails { get; set; }
        public string? ErrorMessage { get; set; }
    }
}