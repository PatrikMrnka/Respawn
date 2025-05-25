using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Application.DTOs.Auth;
using RespawnApi.Domain.Enums;
using System.Security.Claims;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<IdentityUser> userManager,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Returns a list of all users in the system.
        /// </summary>
        /// <remarks>
        /// Only users with the Administrator or Spravce role can access this endpoint.
        /// </remarks>
        /// <response code="200">Returns the list of users.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("users")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ListUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userDtos = new List<UserDto>();

                foreach (var user in users)
                {
                    userDtos.Add(new UserDto
                    {
                        Id = user.Id,
                        Nickname = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        Roles = await _userManager.GetRolesAsync(user)
                    });
                }

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při načítání seznamu uživatelů.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new AuthResponseDto
                        { IsSuccess = false, Message = "Interní chyba serveru při načítání uživatelů." });
            }
        }

        /// <summary>
        /// Updates the roles of a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose roles are to be updated.</param>
        /// <param name="newRoles">A list of new roles to assign to the user.</param>
        /// <remarks>
        /// Only users with the Administrator or Spravce role can access this endpoint.
        /// </remarks>
        /// <response code="200">If the roles were successfully updated.</response>
        /// <response code="400">If the request is invalid or roles cannot be updated.</response>
        /// <response code="404">If the user is not found.</response>
        [HttpPut("user/{userId}/roles")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRoles(string userId, [FromBody] List<string> newRoles)
        {
            if (newRoles == null || !newRoles.Any())
            {
                return BadRequest(
                    new AuthResponseDto { IsSuccess = false, Message = "Seznam rolí nemůže být prázdný." });
            }

            var allSystemRoles = new List<string> { UserRoles.Administrator, UserRoles.Spravce, UserRoles.Uzivatel };
            if (newRoles.Any(nr => !allSystemRoles.Contains(nr)))
            {
                var invalidRoles = newRoles.Where(nr => !allSystemRoles.Contains(nr));
                return BadRequest(new AuthResponseDto
                {
                    IsSuccess = false, Message = $"Následující role jsou neplatné: {string.Join(", ", invalidRoles)}"
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new AuthResponseDto { IsSuccess = false, Message = "Uživatel nebyl nalezen." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isCurrentUserAdmin = User.IsInRole(UserRoles.Administrator);

            // Spravce cannot change roles of Administrators or themselves if they are not an admin
            if (!isCurrentUserAdmin && (await _userManager.IsInRoleAsync(user, UserRoles.Administrator) ||
                                        user.Id == currentUserId))
            {
                if (await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
                {
                    return Forbid(); // Spravce cannot change roles of Administrators
                }

                // Spravce cannot change their own roles if they are not an admin
                if (newRoles.Contains(UserRoles.Administrator) || !newRoles.Contains(UserRoles.Spravce))
                {
                    return Forbid();
                }
            }


            // deny removing Administrator role from the last admin
            if (user.Id == currentUserId
                && (await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
                && !newRoles.Contains(UserRoles.Administrator))
            {
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Administrator);

                if (admins.Count(a => a.Id != user.Id) == 0)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message =
                            "Nemůžete odebrat roli Administrátor poslednímu administrátorovi v systému (sám sobě)."
                    });
                }
            }

            // deny removing Administrator role from the last admin if the user is not the current user
            if (user.Id != currentUserId
                && (await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
                && !newRoles.Contains(UserRoles.Administrator))
            {
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Administrator);
                if (admins.Count == 1 && admins.First().Id == user.Id)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Nemůžete odebrat roli Administrátor jedinému administrátorovi v systému."
                    });
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(newRoles).ToList();
            var rolesToAdd = newRoles.Except(currentRoles).ToList();

            // spravce cannot add Administrator role
            if (!isCurrentUserAdmin && rolesToAdd.Contains(UserRoles.Administrator))
            {
                return Forbid();
            }

            // spravce cannot remove Administrator role if they are not an admin
            if (!isCurrentUserAdmin && rolesToRemove.Contains(UserRoles.Administrator) &&
                currentRoles.Contains(UserRoles.Administrator))
            {
                return Forbid();
            }


            IdentityResult removeResult = IdentityResult.Success;
            if (rolesToRemove.Any())
            {
                removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            if (!removeResult.Succeeded)
            {
                var errors = removeResult.Errors.Select(e => e.Description);
                _logger.LogError(
                    $"UpdateUserRoles: Nepodařilo se odebrat role uživateli ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                return BadRequest(new AuthResponseDto
                    { IsSuccess = false, Message = $"Chyba při odebírání rolí: {string.Join(", ", errors)}" });
            }

            IdentityResult addResult = IdentityResult.Success;
            if (rolesToAdd.Any())
            {
                addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (!addResult.Succeeded)
            {
                var errors = addResult.Errors.Select(e => e.Description);
                _logger.LogError(
                    $"UpdateUserRoles: Nepodařilo se přidat role uživateli ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                if (rolesToRemove.Any()) await _userManager.AddToRolesAsync(user, rolesToRemove); // Rollback
                return BadRequest(new AuthResponseDto
                    { IsSuccess = false, Message = $"Chyba při přidávání rolí: {string.Join(", ", errors)}" });
            }

            _logger.LogInformation(
                $"Role pro uživatele ID '{userId}' (Přezdívka: {user.UserName}) byly aktualizovány na: {string.Join(", ", newRoles)} administrátorem ID '{currentUserId}'.");
            return Ok(new AuthResponseDto { IsSuccess = true, Message = "Role uživatele byly úspěšně aktualizovány." });
        }

        /// <summary>
        /// Deletes a user from the system.
        /// </summary>
        /// <param name="userId">The ID of the user to delete.</param>
        /// <remarks>
        /// Only users with the Administrator or Spravce role can access this endpoint.
        /// </remarks>
        /// <response code="200">If the user was successfully deleted.</response>
        /// <response code="400">If the request is invalid or the user cannot be deleted.</response>
        /// <response code="404">If the user is not found.</response>
        [HttpDelete("user/{userId}")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new AuthResponseDto { IsSuccess = false, Message = "Uživatel nebyl nalezen." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == userId)
            {
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Nemůžete smazat sám sebe." });
            }

            var isCurrentUserAdmin = User.IsInRole(UserRoles.Administrator);

            // Spravce cannot delete Administrators or themselves if they are not an admin
            if (!isCurrentUserAdmin && await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
            {
                return Forbid();
            }

            // Spravce cannot delete themselves if they are not an admin
            if (await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
            {
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Administrator);
                if (admins.Count <= 1)
                {
                    return BadRequest(new AuthResponseDto
                        { IsSuccess = false, Message = "Nemůžete smazat posledního administrátora." });
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogError(
                    $"DeleteUser: Nepodařilo se smazat uživatele ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                return BadRequest(new AuthResponseDto
                    { IsSuccess = false, Message = $"Chyba při mazání uživatele: {string.Join(", ", errors)}" });
            }

            _logger.LogInformation(
                $"Uživatel ID '{userId}' (Přezdívka: {user.UserName}) byl smazán administrátorem/správcem ID '{currentUserId}'.");

            return Ok(new AuthResponseDto { IsSuccess = true, Message = "Uživatel byl úspěšně smazán." });
        }
    }
}