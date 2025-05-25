namespace RespawnApi.Application.DTOs.GameServer
{
    public class GameServerDetailDto : GameServerDto
    {
        public string? GameName { get; set; } // Název hry, např. "Counter-Strike: Global Offensive"
        public string? MapName { get; set; } // Název mapy, např. "de_dust2"
        public int MaxPlayers { get; set; } // Maximální počet hráčů, který server podporuje
        public int CurrentPlayers { get; set; } // Aktuální počet hráčů na serveru
        public bool IsVacSecured { get; set; } // Indikuje, zda je server zabezpečený VAC (Valve Anti-Cheat)
        public List<PlayerDetailDto> Players { get; set; } = new List<PlayerDetailDto>(); // Seznam hráčů na serveru
    }
}
