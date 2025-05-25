namespace RespawnApi.Application.DTOs.Auth
{
    /// <summary>
    /// Represents the response data for authentication operations.
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// The JWT token for authentication.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the authentication was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// A message providing additional information about the authentication result.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Contains user information if the authentication was successful.
        /// </summary>
        public UserDto? UserInfo { get; set; }

        /// <summary>
        /// The date and time when the token expires, if applicable.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
