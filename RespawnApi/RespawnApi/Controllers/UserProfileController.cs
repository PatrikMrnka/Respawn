using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Application.DTOs.Auth;
using RespawnApi.Application.DTOs.UserProfile;
using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Entities;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Všechny akce v tomto kontroleru vyžadují autorizaci
    public class UserProfileController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ITokenService _tokenService; // Přidáno pro generování nového tokenu
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            UserManager<IdentityUser> userManager,
            IUserProfileRepository userProfileRepository,
            ITokenService tokenService, // Přidáno
            ILogger<UserProfileController> logger)
        {
            _userManager = userManager;
            _userProfileRepository = userProfileRepository;
            _tokenService = tokenService; // Přidáno
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
                return Unauthorized(new AuthResponseDto { IsSuccess = false, Message = "Uživatel není autorizován." });
            }

            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                _logger.LogWarning($"GetMyProfile: IdentityUser s ID '{userId}' nebyl nalezen.");
                return NotFound(new AuthResponseDto { IsSuccess = false, Message = "Uživatel nebyl nalezen." });
            }

            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);
            if (userProfile == null)
            {
                _logger.LogWarning($"GetMyProfile: UserProfile pro IdentityUser ID '{userId}' nebyl nalezen. Vytvářím nový, pokud uživatel existuje v Identity.");
                // Toto by se mělo dít jen pokud UserProfile nebyl vytvořen při registraci
                userProfile = new UserProfile
                {
                    UserId = identityUser.Id,
                    Nickname = identityUser.UserName ?? "NeznámýUživatel", // Mělo by být vždy nastaveno
                    AvatarUrl = null
                };
                await _userProfileRepository.AddAsync(userProfile);
            }

            var userRoles = await _userManager.GetRolesAsync(identityUser);

            var userDto = new UserDto
            {
                Id = identityUser.Id,
                Nickname = userProfile.Nickname,
                Email = identityUser.Email ?? string.Empty,
                AvatarUrl = userProfile.AvatarUrl,
                Roles = userRoles
            };

            return Ok(userDto);
        }

        // PUT: api/userprofile/me
        [HttpPut("me")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)] // Vracíme AuthResponseDto s novým tokenem
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)] // Pro validační chyby nebo jiné chyby
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                // Vracíme AuthResponseDto pro konzistenci chybových odpovědí
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = string.Join("; ", errors) });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponseDto { IsSuccess = false, Message = "Uživatel není autorizován." });
            }

            var identityUser = await _userManager.FindByIdAsync(userId);
            if (identityUser == null)
            {
                _logger.LogWarning($"UpdateMyProfile: IdentityUser s ID '{userId}' nebyl nalezen.");
                return NotFound(new AuthResponseDto { IsSuccess = false, Message = "Uživatel nebyl nalezen." });
            }

            var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);
            if (userProfile == null)
            {
                // Mělo by být nepravděpodobné, pokud profil vzniká při registraci
                _logger.LogError($"UpdateMyProfile: UserProfile pro IdentityUser ID '{userId}' nebyl nalezen. Vytvářím nový.");
                userProfile = new UserProfile
                {
                    UserId = identityUser.Id,
                    Nickname = updateDto.Nickname, // Použijeme nový nickname
                    AvatarUrl = updateDto.AvatarUrl
                };
                await _userProfileRepository.AddAsync(userProfile); // Uložíme nový profil
            }


            // Aktualizace Nickname v IdentityUser (UserName)
            if (identityUser.UserName != updateDto.Nickname)
            {
                var existingUserWithNewNickname = await _userManager.FindByNameAsync(updateDto.Nickname);
                if (existingUserWithNewNickname != null && existingUserWithNewNickname.Id != userId)
                {
                    _logger.LogWarning($"UpdateMyProfile: Pokus o změnu přezdívky na existující: {updateDto.Nickname} pro uživatele ID '{userId}'.");
                    return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Tato přezdívka je již obsazena." });
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(identityUser, updateDto.Nickname);
                if (!setUserNameResult.Succeeded)
                {
                    var errors = setUserNameResult.Errors.Select(e => e.Description);
                    _logger.LogError($"UpdateMyProfile: Nepodařilo se nastavit UserName pro ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                    return BadRequest(new AuthResponseDto { IsSuccess = false, Message = $"Nepodařilo se aktualizovat přezdívku v systému identity: {string.Join(", ", errors)}" });
                }

                // Je důležité zavolat UpdateAsync pro aktualizaci NormalizedUserName a SecurityStamp
                var updateIdentityResult = await _userManager.UpdateAsync(identityUser);
                if (!updateIdentityResult.Succeeded)
                {
                    var errors = updateIdentityResult.Errors.Select(e => e.Description);
                    _logger.LogError($"UpdateMyProfile: Nepodařilo se aktualizovat IdentityUser po změně UserName pro ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                    return BadRequest(new AuthResponseDto { IsSuccess = false, Message = $"Chyba při finalizaci změny přezdívky: {string.Join(", ", errors)}" });
                }
            }

            // Aktualizace UserProfile
            userProfile.Nickname = updateDto.Nickname; // Udržujte konzistentní s IdentityUser.UserName
            userProfile.AvatarUrl = updateDto.AvatarUrl; // Může být null

            try
            {
                await _userProfileRepository.UpdateAsync(userProfile);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"UpdateMyProfile: Chyba při ukládání UserProfile pro ID '{userId}'.");
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponseDto { IsSuccess = false, Message = "Nastala chyba při ukládání profilu." });
            }

            _logger.LogInformation($"Profil pro uživatele ID '{userId}' (Přezdívka: {updateDto.Nickname}) byl aktualizován.");

            // Vygenerovat nový token, protože UserName (součást claimů) a SecurityStamp se mohly změnit
            var tokenResponse = await _tokenService.GenerateTokenAsync(identityUser, userProfile);
            tokenResponse.Message = "Profil byl úspěšně aktualizován.";

            return Ok(tokenResponse);
        }
    }
}
