namespace RespawnApi.Application.DTOs.GameServer
{
    public class GameServerDetailDto : GameServerDto // Inherits basic info
    {
        public string? GameName { get; set; } // Specific game name from A2S_INFO
        public string? MapName { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public bool IsVacSecured { get; set; }
        public List<PlayerDetailDto> Players { get; set; } = new List<PlayerDetailDto>();
        // Add other relevant details from A2S_INFO if needed
    }
}
