namespace RespawnApi.Application.DTOs.GameServer
{
    public class PlayerDetailDto
    {
        public required string Name { get; set; } // Jméno hráče
        public int Score { get; set; } // Skóre hráče
        public float Duration { get; set; } // Čas, který hráč strávil na serveru v sekundách
    }
}
