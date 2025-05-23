// Application/DTOs/Presence/UserStatusDto.cs
namespace RespawnApi.Application.DTOs.Presence
{
    public class UserStatusDto
    {
        public required string UserId { get; set; }
        public required string Nickname { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; } // Kdy byl uživatel naposledy aktivní/online
    }
}