namespace RespawnApi.Application.DTOs.Auth
{
    /// <summary>
    /// Represents the data transfer object for user information.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// The unique identifier of the user.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The nickname of the user.
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the user's avatar image.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// A list of roles assigned to the user, such as "Admin", "User", etc.
        /// </summary>
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
