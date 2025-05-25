namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Service for managing user presence in the application.
    /// </summary>
    public interface IUserPresenceService
    {
        /// <summary>
        /// Registers a user's connection to the application.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="connectionId">The unique identifier of the connection.</param>
        Task UserConnectedAsync(string userId, string connectionId);

        /// <summary>
        /// Handles the disconnection of a user's connection from the application.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="connectionId">The unique identifier of the connection.</param>
        Task UserDisconnectedAsync(string userId, string connectionId);

        /// <summary>
        /// Checks if a user is currently online.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>True if the user is online; otherwise, false.</returns>
        bool IsUserOnline(string userId);

        /// <summary>
        /// Retrieves the list of user IDs that are currently online.
        /// </summary>
        /// <returns>An enumerable collection of online user IDs.</returns>
        IEnumerable<string> GetOnlineUserIds();

        /// <summary>
        /// Gets the last seen timestamp for a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The DateTime the user was last seen online, or null if never seen.</returns>
        DateTime? GetLastSeen(string userId);
    }
}
