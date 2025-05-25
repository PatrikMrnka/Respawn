using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.DTOs.GameServer
{
    public class GameServerDto
    {
        public Guid GameServerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public GameType GameType { get; set; }
        public ServerStatus Status { get; set; } // Celkový/kontejnerový stav
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public string? ContainerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StatusDetails { get; set; }
    }
}