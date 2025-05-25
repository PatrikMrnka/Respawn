using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Application.DTOs.Presence;
using RespawnApi.Data;
using RespawnApi.Application.Interfaces;

namespace RespawnApi.Controllers
{
    /// <summary>
    /// Controller for managing user presence and retrieving user online status information.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PresenceController : ControllerBase
    {
        private readonly RespawnDbContext _context;
        private readonly IUserPresenceService _presenceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresenceController"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing user profiles.</param>
        /// <param name="presenceService">The service for managing user presence.</param>
        public PresenceController(RespawnDbContext context, IUserPresenceService presenceService)
        {
            _context = context;
            _presenceService = presenceService;
        }

        /// <summary>
        /// Retrieves all users with their online status and last seen information.
        /// </summary>
        /// <returns>A list of <see cref="UserStatusDto"/> representing each user's status.</returns>
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserStatusDto>>> GetAllUsersWithStatus()
        {
            var allUserProfiles = await _context.UserProfiles.ToListAsync();
            var onlineUserIds = _presenceService.GetOnlineUserIds().ToHashSet(); // for fast lookup

            var userStatuses = allUserProfiles.Select(up => new UserStatusDto
            {
                UserId = up.UserId,
                Nickname = up.Nickname,
                AvatarUrl = up.AvatarUrl,
                IsOnline = onlineUserIds.Contains(up.UserId),
                LastSeen = _presenceService.GetLastSeen(up.UserId)
            })
                .OrderByDescending(u => u.IsOnline) // online users first
                .ThenBy(u => u.Nickname) // then by nickname
                .ToList();

            return Ok(userStatuses);
        }
    }
}