namespace RespawnApi.Application.DTOs.Presence
{
    /// <summary>
    /// Represents the data transfer object for user status in the presence system.
    /// </summary>
    public class UserStatusDto
    {
        /// <summary>
        /// The unique identifier for the user, typically a GUID or a string.
        /// </summary>
        public required string UserId { get; set; }

        /// <summary>
        /// The nickname of the user, which is used to display the user's name in a user-friendly format.
        /// </summary>
        public required string Nickname { get; set; }

        /// <summary>
        /// The URL of the user's avatar, which can be used to visually represent the user in the presence system.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Indicates whether the user is currently online (connected to the server) or offline (not connected).
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// The date and time when the user was last seen online, typically in UTC format.
        /// </summary>
        public DateTime? LastSeen { get; set; }
    }
}