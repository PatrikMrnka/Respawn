// Application/Services/UserPresenceService.cs
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.Presence;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Hubs;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace RespawnApi.Application.Services
{
    public interface IUserPresenceService
    {
        Task UserConnectedAsync(string userId, string connectionId);
        Task UserDisconnectedAsync(string userId, string connectionId);
        bool IsUserOnline(string userId);
        IEnumerable<string> GetOnlineUserIds();
        DateTime? GetLastSeen(string userId);
    }

    public class UserPresenceService : IUserPresenceService
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> OnlineUsersConnections = new();
        private static readonly ConcurrentDictionary<string, DateTime> LastSeenUsers = new();
        // OfflineTimers a _offlineGracePeriod se již nebudou používat pro okamžitý broadcast
        // private static readonly ConcurrentDictionary<string, Timer> OfflineTimers = new();
        // private readonly TimeSpan _offlineGracePeriod = TimeSpan.FromSeconds(5); 

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<PresenceHub> _presenceHubContext;
        private readonly ILogger<UserPresenceService> _logger;

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
            // Logika pro zrušení časovače zde již není relevantní, pokud časovač nepoužíváme.
            // Pokud byste chtěli velmi krátkou grace period pro refreshe, musela by se logika obnovit.
            // Prozatím předpokládáme okamžitou reakci na připojení/odpojení.

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
            _logger.LogInformation("User {UserId} connected with ConnectionId {ConnectionId}. Total connections for user: {Count}", userId, connectionId, OnlineUsersConnections.TryGetValue(userId, out var set) ? set.Count : 0);
            return Task.CompletedTask;
        }

        public async Task UserDisconnectedAsync(string userId, string connectionId)
        {
            bool wasLastConnection = false;
            if (OnlineUsersConnections.TryGetValue(userId, out var connectionIds))
            {
                lock (connectionIds)
                {
                    connectionIds.Remove(connectionId);
                    if (connectionIds.IsNullOrEmpty())
                    {
                        wasLastConnection = true; // Toto bylo poslední aktivní spojení uživatele
                        OnlineUsersConnections.TryRemove(userId, out _); // Odstraníme uživatele ze seznamu online spojení
                    }
                }
                _logger.LogInformation("User {UserId} disconnected ConnectionId {ConnectionId}. Remaining connections for user: {Count}", userId, connectionId, connectionIds.Count);
            }
            else
            {
                _logger.LogWarning("UserDisconnectedAsync called for UserId {UserId} but user was not found in OnlineUsersConnections.", userId);
            }

            if (wasLastConnection)
            {
                // Pokud to bylo poslední spojení, okamžitě označíme uživatele jako offline a rozešleme informaci
                LastSeenUsers[userId] = DateTime.UtcNow;
                _logger.LogInformation("User {UserId} is now offline (last connection closed). Broadcasting UserOffline.", userId);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var userProfileRepository = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
                    var userProfile = await userProfileRepository.GetByUserIdAsync(userId);
                    var userStatus = new UserStatusDto
                    {
                        UserId = userId,
                        Nickname = userProfile?.Nickname ?? userId, // Fallback
                        AvatarUrl = userProfile?.AvatarUrl,
                        IsOnline = false, // Explicitně false
                        LastSeen = LastSeenUsers[userId]
                    };
                    await _presenceHubContext.Clients.All.SendAsync("UserOffline", userStatus);
                    _logger.LogInformation("Sent UserOffline for {UserId} via IHubContext immediately.", userId);
                }
            }
            else if (OnlineUsersConnections.ContainsKey(userId)) // Stále má jiná spojení
            {
                LastSeenUsers[userId] = DateTime.UtcNow; // Aktualizujeme last seen
            }
        }

        public bool IsUserOnline(string userId)
        {
            // Uživatel je online, pouze pokud má aktivní spojení
            return OnlineUsersConnections.TryGetValue(userId, out var conns) && conns.Any();
        }

        public IEnumerable<string> GetOnlineUserIds()
        {
            return OnlineUsersConnections.Where(kvp => kvp.Value.Any()).Select(kvp => kvp.Key).ToList();
        }
        public DateTime? GetLastSeen(string userId)
        {
            return LastSeenUsers.TryGetValue(userId, out var lastSeen) ? lastSeen : null;
        }
    }
}
