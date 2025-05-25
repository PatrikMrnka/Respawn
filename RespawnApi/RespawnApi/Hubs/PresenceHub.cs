// Hubs/PresenceHub.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.Presence;
using RespawnApi.Application.Services;
using RespawnApi.DataAccess.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using RespawnApi.Application.Interfaces;

namespace RespawnApi.Hubs
{
    /// <summary>
    /// SignalR hub for managing user presence (online/offline status) in real-time.
    /// </summary>
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly IUserPresenceService _presenceService;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ILogger<PresenceHub> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresenceHub"/> class.
        /// </summary>
        /// <param name="presenceService">Service for managing user presence state.</param>
        /// <param name="userProfileRepository">Repository for accessing user profile data.</param>
        /// <param name="logger">Logger instance for logging hub events.</param>
        public PresenceHub(
            IUserPresenceService presenceService,
            IUserProfileRepository userProfileRepository,
            ILogger<PresenceHub> logger)
        {
            _presenceService = presenceService;
            _userProfileRepository = userProfileRepository;
            _logger = logger;
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// Registers the user as online and notifies other clients.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning(
                    "Nepřihlášený uživatel se pokusil připojit k PresenceHub. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                Context.Abort();
                return;
            }

            // Register the user's connection
            await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);
            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);

            if (userProfile != null)
            {
                // Create user status DTO to broadcast
                var userStatus = new UserStatusDto
                {
                    UserId = userId,
                    Nickname = userProfile.Nickname,
                    AvatarUrl = userProfile.AvatarUrl,
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow
                };
                // Notify all other clients that this user is online
                await Clients.Others.SendAsync("UserOnline", userStatus);
                _logger.LogInformation(
                    "Uživatel {UserId} ({Nickname}) se připojil k PresenceHub. ConnectionId: {ConnectionId}", userId,
                    userProfile.Nickname, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("Nepodařilo se najít profil pro uživatele {UserId} při připojení k PresenceHub.",
                    userId);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// Marks the user as disconnected and logs the event.
        /// </summary>
        /// <param name="exception">The exception that occurred during disconnect, if any.</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                // Unregister the user's connection
                await _presenceService.UserDisconnectedAsync(userId, Context.ConnectionId);
                _logger.LogInformation(
                    "Uživatel {UserId} se odpojil z PresenceHub (zahájena grace period). ConnectionId: {ConnectionId}. Důvod: {ExceptionMessage}",
                    userId, Context.ConnectionId, exception?.Message);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}