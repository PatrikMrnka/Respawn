namespace RespawnApi.Application.DTOs.GameServer
{
    public class PlayerDetailDto
    {
        public required string Name { get; set; }
        public int Score { get; set; }
        public float Duration { get; set; } // Duration in seconds player has been on server
    }
}
