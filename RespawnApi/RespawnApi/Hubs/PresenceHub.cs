// Hubs/PresenceHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.Presence;
using RespawnApi.Application.Services;
using RespawnApi.DataAccess.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RespawnApi.Hubs
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly IUserPresenceService _presenceService;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ILogger<PresenceHub> _logger;

        public PresenceHub(
            IUserPresenceService presenceService,
            IUserProfileRepository userProfileRepository,
            ILogger<PresenceHub> logger)
        {
            _presenceService = presenceService;
            _userProfileRepository = userProfileRepository;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Nepřihlášený uživatel se pokusil připojit k PresenceHub. ConnectionId: {ConnectionId}", Context.ConnectionId);
                Context.Abort();
                return;
            }

            await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);
            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);

            if (userProfile != null)
            {
                var userStatus = new UserStatusDto
                {
                    UserId = userId,
                    Nickname = userProfile.Nickname,
                    AvatarUrl = userProfile.AvatarUrl,
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow
                };
                // Odeslat všem ostatním klientům, že tento uživatel je online
                await Clients.Others.SendAsync("UserOnline", userStatus);
                _logger.LogInformation("Uživatel {UserId} ({Nickname}) se připojil k PresenceHub. ConnectionId: {ConnectionId}", userId, userProfile.Nickname, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("Nepodařilo se najít profil pro uživatele {UserId} při připojení k PresenceHub.", userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                // UserPresenceService nyní sám zařídí odeslání zprávy UserOffline po grace period
                await _presenceService.UserDisconnectedAsync(userId, Context.ConnectionId);
                _logger.LogInformation("Uživatel {UserId} se odpojil z PresenceHub (zahájena grace period). ConnectionId: {ConnectionId}. Důvod: {ExceptionMessage}", userId, Context.ConnectionId, exception?.Message);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
