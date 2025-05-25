using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.DTOs.GameServer
{
    public class GameServerStatusUpdateDto
    {
        public Guid GameServerId { get; set; }
        public ServerStatus NewOverallStatus { get; set; }
        public string? StatusDetails { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
