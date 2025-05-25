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

namespace RespawnApi.Controllers
{
    /// <summary>
    /// Controller for managing user profile operations such as retrieving and updating the current user's profile.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UserProfileController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfileController"/> class.
        /// </summary>
        /// <param name="userManager">The user manager for identity operations.</param>
        /// <param name="userProfileRepository">The repository for user profile data access.</param>
        /// <param name="tokenService">The service for generating authentication tokens.</param>
        /// <param name="logger">The logger instance.</param>
        public UserProfileController(
            UserManager<IdentityUser> userManager,
            IUserProfileRepository userProfileRepository,
            ITokenService tokenService,
            ILogger<UserProfileController> logger)
        {
            _userManager = userManager;
            _userProfileRepository = userProfileRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the profile of the currently authenticated user.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the user's profile information if found; otherwise, an error response.
        /// </returns>
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
                _logger.LogWarning(
                    $"GetMyProfile: UserProfile pro IdentityUser ID '{userId}' nebyl nalezen. Vytvářím nový, pokud uživatel existuje v Identity.");
                userProfile = new UserProfile
                {
                    UserId = identityUser.Id,
                    Nickname = identityUser.UserName ?? "NeznámýUživatel",
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

        /// <summary>
        /// Updates the profile of the currently authenticated user.
        /// </summary>
        /// <param name="updateDto">The DTO containing updated profile information.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the update operation.
        /// </returns>
        [HttpPut("me")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            if (!ModelState.IsValid)
            {
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
                // it should not happen, but if UserProfile is not found, we create a new one
                _logger.LogError(
                    $"UpdateMyProfile: UserProfile pro IdentityUser ID '{userId}' nebyl nalezen. Vytvářím nový.");
                userProfile = new UserProfile
                {
                    UserId = identityUser.Id,
                    Nickname = updateDto.Nickname,
                    AvatarUrl = updateDto.AvatarUrl
                };

                await _userProfileRepository.AddAsync(userProfile);
            }

            if (identityUser.UserName != updateDto.Nickname)
            {
                var existingUserWithNewNickname = await _userManager.FindByNameAsync(updateDto.Nickname);
                if (existingUserWithNewNickname != null && existingUserWithNewNickname.Id != userId)
                {
                    _logger.LogWarning(
                        $"UpdateMyProfile: Pokus o změnu přezdívky na existující: {updateDto.Nickname} pro uživatele ID '{userId}'.");
                    return BadRequest(new AuthResponseDto
                        { IsSuccess = false, Message = "Tato přezdívka je již obsazena." });
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(identityUser, updateDto.Nickname);
                if (!setUserNameResult.Succeeded)
                {
                    var errors = setUserNameResult.Errors.Select(e => e.Description);
                    _logger.LogError(
                        $"UpdateMyProfile: Nepodařilo se nastavit UserName pro ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message =
                            $"Nepodařilo se aktualizovat přezdívku v systému identity: {string.Join(", ", errors)}"
                    });
                }

                var updateIdentityResult = await _userManager.UpdateAsync(identityUser);
                if (!updateIdentityResult.Succeeded)
                {
                    var errors = updateIdentityResult.Errors.Select(e => e.Description);
                    _logger.LogError(
                        $"UpdateMyProfile: Nepodařilo se aktualizovat IdentityUser po změně UserName pro ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = $"Chyba při finalizaci změny přezdívky: {string.Join(", ", errors)}"
                    });
                }
            }

            userProfile.Nickname = updateDto.Nickname;
            userProfile.AvatarUrl = updateDto.AvatarUrl;

            try
            {
                await _userProfileRepository.UpdateAsync(userProfile);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"UpdateMyProfile: Chyba při ukládání UserProfile pro ID '{userId}'.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new AuthResponseDto { IsSuccess = false, Message = "Nastala chyba při ukládání profilu." });
            }

            _logger.LogInformation(
                $"Profil pro uživatele ID '{userId}' (Přezdívka: {updateDto.Nickname}) byl aktualizován.");

            // generate new token after profile update
            var tokenResponse = await _tokenService.GenerateTokenAsync(identityUser, userProfile);
            tokenResponse.Message = "Profil byl úspěšně aktualizován.";

            return Ok(tokenResponse);
        }
    }
}