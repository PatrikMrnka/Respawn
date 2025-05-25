namespace RespawnApi.Application.DTOs.Presence
{
    public class UserStatusDto
    {
        public required string UserId { get; set; } // ID uživatele
        public required string Nickname { get; set; } // Přezdívka uživatele
        public string? AvatarUrl { get; set; } // URL na avatar uživatele (může být null, pokud není nastaven)
        public bool IsOnline { get; set; } // Indikuje, zda je uživatel online (připojen k serveru)
        public DateTime? LastSeen { get; set; } // Kdy byl uživatel naposledy aktivní/online
    }
}