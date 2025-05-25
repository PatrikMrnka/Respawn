using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.Presence;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Hubs;
using System.Collections.Concurrent;
using Microsoft.IdentityModel.Tokens;
using RespawnApi.Application.Interfaces;

namespace RespawnApi.Application.Services
{
    /// <summary>
    /// Service for managing user presence in the application.
    /// </summary>
    public class UserPresenceService : IUserPresenceService
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>>
            OnlineUsersConnections = new(); // userId -> connectionIds

        private static readonly ConcurrentDictionary<string, DateTime> LastSeenUsers = new();

        // maybe implement grace period for refreshing presence?

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<PresenceHub> _presenceHubContext;
        private readonly ILogger<UserPresenceService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserPresenceService"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating service scopes.</param>
        /// <param name="presenceHubContext">The SignalR hub context for broadcasting presence updates.</param>
        /// <param name="logger">The logger instance for logging presence service events.</param>
        public UserPresenceService(
            IServiceScopeFactory scopeFactory,
            IHubContext<PresenceHub> presenceHubContext,
            ILogger<UserPresenceService> logger)
        {
            _scopeFactory = scopeFactory;
            _presenceHubContext = presenceHubContext;
            _logger = logger;
        }

        public Task UserConnectedAsync(string userId, string connectionId)
        {
            // immediate response on connection (grace period not implemented)
            OnlineUsersConnections.AddOrUpdate(userId,
                key => new HashSet<string> { connectionId },
                (key, existingSet) =>
                {
                    lock (existingSet)
                    {
                        existingSet.Add(connectionId);
                    }

                    return existingSet;
                });
            LastSeenUsers[userId] = DateTime.UtcNow;

            _logger.LogInformation(
                "User {UserId} connected with ConnectionId {ConnectionId}. Total connections for user: {Count}", userId,
                connectionId, OnlineUsersConnections.TryGetValue(userId, out var set) ? set.Count : 0);
            return Task.CompletedTask;
        }

        public async Task UserDisconnectedAsync(string userId, string connectionId)
        {
            bool wasLastConnection = false;
            if (OnlineUsersConnections.TryGetValue(userId, out var connectionIds))
            {
                lock (connectionIds) // lock for thread safety
                {
                    connectionIds.Remove(connectionId);
                    if (connectionIds.IsNullOrEmpty())
                    {
                        wasLastConnection = true; // last active connection removed
                        OnlineUsersConnections.TryRemove(userId,
                            out _); // remove the user from the online users list
                    }
                }

                _logger.LogInformation(
                    "User {UserId} disconnected ConnectionId {ConnectionId}. Remaining connections for user: {Count}",
                    userId, connectionId, connectionIds.Count);
            }
            else
            {
                _logger.LogWarning(
                    "UserDisconnectedAsync called for UserId {UserId} but user was not found in OnlineUsersConnections.",
                    userId);
            }

            if (wasLastConnection)
            {
                // if it was the last connection, mark user as offline immediately and broadcast
                LastSeenUsers[userId] = DateTime.UtcNow;
                _logger.LogInformation(
                    "User {UserId} is now offline (last connection closed). Broadcasting UserOffline.", userId);

                using (var scope = _scopeFactory.CreateScope()) // create a scope for database access
                {
                    var userProfileRepository = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
                    var userProfile = await userProfileRepository.GetByUserIdAsync(userId);
                    var userStatus = new UserStatusDto
                    {
                        UserId = userId,
                        Nickname = userProfile?.Nickname ?? userId, // Fallback
                        AvatarUrl = userProfile?.AvatarUrl,
                        IsOnline = false,
                        LastSeen = LastSeenUsers[userId]
                    };
                    await _presenceHubContext.Clients.All.SendAsync("UserOffline", userStatus);
                    _logger.LogInformation("Sent UserOffline for {UserId} via IHubContext immediately.", userId);
                }
            }
            else if (OnlineUsersConnections.ContainsKey(userId)) // still has active connections
            {
                LastSeenUsers[userId] = DateTime.UtcNow; // update last seen time
            }
        }

        public bool IsUserOnline(string userId)
        {
            // user is considered online if they have any active connections
            return OnlineUsersConnections.TryGetValue(userId, out var conns) && conns.Any();
        }

        public IEnumerable<string> GetOnlineUserIds()
        {
            // returns a list of user IDs that have at least one active connection
            return OnlineUsersConnections.Where(kvp => kvp.Value.Any()).Select(kvp => kvp.Key).ToList();
        }

        public DateTime? GetLastSeen(string userId)
        {
            // returns the last seen time for a user, or null if the user is not found
            return LastSeenUsers.TryGetValue(userId, out var lastSeen) ? lastSeen : null;
        }
    }
}