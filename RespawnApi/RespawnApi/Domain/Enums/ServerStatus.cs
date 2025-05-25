namespace RespawnApi.Domain.Enums
{
    /// <summary>
    /// Represents the status of a game server container.
    /// </summary>
    public enum ServerStatus
    {
        /// <summary>
        /// Indicates the current status of a game server container.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Indicates that the game server container is not running.
        /// </summary>
        Offline = 1,

        /// <summary>
        /// Indicates that the game server container is running and available for connections.
        /// </summary>
        Online = 2,

        /// <summary>
        /// Indicates that the game server container is in a transitional state, such as starting or stopping.
        /// </summary>
        Starting = 3,

        /// <summary>
        /// Indicates that the game server container is in the process of stopping.
        /// </summary>
        Stopping = 4,

        /// <summary>
        /// Indicates that the game server container is restarting, which may occur due to a crash or manual intervention.
        /// </summary>
        Restarting = 5,

        /// <summary>
        /// Indicates that the game server container is paused, meaning it is temporarily halted but can be resumed.
        /// </summary>
        Error = 8,

        /// <summary>
        /// Indicates that the game server container is pending creation, meaning it exists in the database but the container is being created.
        /// </summary>
        PendingCreation = 9
    }
}