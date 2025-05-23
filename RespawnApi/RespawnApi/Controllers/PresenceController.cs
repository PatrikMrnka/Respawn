// Controllers/PresenceController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Application.DTOs.Presence;
using RespawnApi.Application.Services;
using RespawnApi.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Vyžaduje přihlášení pro přístup k seznamu uživatelů
    public class PresenceController : ControllerBase
    {
        private readonly RespawnDbContext _context;
        private readonly IUserPresenceService _presenceService;

        public PresenceController(RespawnDbContext context, IUserPresenceService presenceService)
        {
            _context = context;
            _presenceService = presenceService;
        }

        // GET: api/presence/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserStatusDto>>> GetAllUsersWithStatus()
        {
            var allUserProfiles = await _context.UserProfiles.ToListAsync();
            var onlineUserIds = _presenceService.GetOnlineUserIds().ToHashSet(); // Pro rychlé vyhledávání

            var userStatuses = allUserProfiles.Select(up => new UserStatusDto
                {
                    UserId = up.UserId,
                    Nickname = up.Nickname,
                    AvatarUrl = up.AvatarUrl,
                    IsOnline = onlineUserIds.Contains(up.UserId),
                    LastSeen = _presenceService.GetLastSeen(up.UserId)
                })
                .OrderByDescending(u => u.IsOnline) // Online uživatelé první
                .ThenBy(u => u.Nickname) // Pak seřadit podle přezdívky
                .ToList();

            return Ok(userStatuses);
        }
    }
}