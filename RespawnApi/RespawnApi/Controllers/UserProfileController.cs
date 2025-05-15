using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RespawnApi.Application.DTOs.Auth;
using RespawnApi.Application.DTOs.UserProfile;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Všechny akce v tomto kontroleru vyžadují autorizaci
    public class UserProfileController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            UserManager<IdentityUser> userManager,
            IUserProfileRepository userProfileRepository,
            ILogger<UserProfileController> logger)
        {
            _userManager = userManager;
            _userProfileRepository = userProfileRepository;
            _logger = logger;
        }

        // GET: api/userprofile/me
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Uživatel není autorizován." });
            }

            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                _logger.LogWarning($"GetMyProfile: IdentityUser s ID '{userId}' nebyl nalezen.");
                return NotFound(new { Message = "Uživatel nebyl nalezen." });
            }

            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);
            if (userProfile == null)
            {
                // Toto by se nemělo stát, pokud je UserProfile vytvářen při registraci
                _logger.LogWarning($"GetMyProfile: UserProfile pro IdentityUser ID '{userId}' nebyl nalezen. Vytvářím nový.");
                userProfile = new UserProfile
                {
                    UserId = identityUser.Id,
                    Nickname = identityUser.UserName ?? "Neznámý", // Mělo by být vždy nastaveno
                    AvatarUrl = null // Nebo nějaká výchozí hodnota
                };
                await _userProfileRepository.AddAsync(userProfile);
            }

            var userRoles = await _userManager.GetRolesAsync(identityUser);

            var userDto = new UserDto
            {
                Id = identityUser.Id,
                Nickname = userProfile.Nickname, // Bereme aktuální nickname z UserProfile
                Email = identityUser.Email ?? string.Empty,
                AvatarUrl = userProfile.AvatarUrl,
                Roles = userRoles
            };

            return Ok(userDto);
        }

        // PUT: api/userprofile/me
        [HttpPut("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "Uživatel není autorizován." });
            }

            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                _logger.LogWarning($"UpdateMyProfile: IdentityUser s ID '{userId}' nebyl nalezen.");
                return NotFound(new { Message = "Uživatel nebyl nalezen." });
            }

            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);
            if (userProfile == null)
            {
                _logger.LogError($"UpdateMyProfile: UserProfile pro IdentityUser ID '{userId}' nebyl nalezen. Toto by se nemělo stát.");
                return NotFound(new { Message = "Profil uživatele nebyl nalezen." });
            }

            // Aktualizace UserProfile
            userProfile.Nickname = updateDto.Nickname;
            userProfile.AvatarUrl = updateDto.AvatarUrl;
            await _userProfileRepository.UpdateAsync(userProfile);

            // Volitelně: Aktualizace IdentityUser.UserName, pokud se Nickname má synchronizovat
            // if (identityUser.UserName != updateDto.Nickname)
            // {
            //     var setUserNameResult = await _userManager.SetUserNameAsync(identityUser, updateDto.Nickname);
            //     if (!setUserNameResult.Succeeded)
            //     {
            //          _logger.LogError($"UpdateMyProfile: Nepodařilo se aktualizovat UserName pro ID '{userId}'. Chyby: {string.Join(", ", setUserNameResult.Errors.Select(e => e.Description))}");
            //          // Můžete zvážit vrácení chyby, nebo pokračovat jen s aktualizací UserProfile
            //     }
            // }
            // Pro jednoduchost zatím neaktualizujeme UserName v IdentityUser, pouze v UserProfile.
            // Pokud byste chtěli, aby se přihlašovalo novým Nickname, museli byste aktualizovat i IdentityUser.UserName.

            _logger.LogInformation($"Profil pro uživatele ID '{userId}' byl aktualizován.");

            var userRoles = await _userManager.GetRolesAsync(identityUser);
            var updatedUserDto = new UserDto
            {
                Id = identityUser.Id,
                Nickname = userProfile.Nickname,
                Email = identityUser.Email ?? string.Empty,
                AvatarUrl = userProfile.AvatarUrl,
                Roles = userRoles
            };

            return Ok(new { Message = "Profil byl úspěšně aktualizován.", UserInfo = updatedUserDto });
        }
    }
}
