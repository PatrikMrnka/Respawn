namespace RespawnApi.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? UserInfo { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
