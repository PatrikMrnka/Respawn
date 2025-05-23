// Soubor: code/RespawnApi/RespawnApi/Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Application.DTOs.Auth; // Pro UserDto
using RespawnApi.Domain.Enums; // Pro UserRoles
using System.Linq;
using System.Security.Claims; // Pro ClaimTypes
using System.Threading.Tasks;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Základní autorizace pro kontroler - přístupné pro Administrátora i Správce
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        // Případně IUserProfileRepository, pokud byste chtěli mazat i profilové záznamy explicitně
        // private readonly IUserProfileRepository _userProfileRepository;

        public AdminController(
            UserManager<IdentityUser> userManager,
            ILogger<AdminController> logger
            /* IUserProfileRepository userProfileRepository */)
        {
            _userManager = userManager;
            _logger = logger;
            // _userProfileRepository = userProfileRepository;
        }

        // GET: api/admin/users
        /// <summary>
        /// Získá seznam všech registrovaných uživatelů s jejich rolemi.
        /// Přístupné pro Administrátora a Správce.
        /// </summary>
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
                        Nickname = user.UserName ?? string.Empty, // UserName se používá jako Nickname
                        Email = user.Email ?? string.Empty,
                        Roles = await _userManager.GetRolesAsync(user)
                        // AvatarUrl by se mohl načítat z UserProfileRepository, pokud je potřeba
                        // a pokud UserDto obsahuje AvatarUrl
                    });
                }
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při načítání seznamu uživatelů.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new AuthResponseDto { IsSuccess = false, Message = "Interní chyba serveru při načítání uživatelů." });
            }
        }

        // PUT: api/admin/user/{userId}/roles
        /// <summary>
        /// Aktualizuje role pro zadaného uživatele.
        /// Přístupné pro Administrátora a Správce.
        /// </summary>
        /// <param name="userId">ID uživatele, jehož role se mají aktualizovat.</param>
        /// <param name="newRoles">Seznam nových rolí pro uživatele.</param>
        [HttpPut("user/{userId}/roles")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRoles(string userId, [FromBody] List<string> newRoles)
        {
            if (newRoles == null || !newRoles.Any())
            {
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Seznam rolí nemůže být prázdný." });
            }

            var allSystemRoles = new List<string> { UserRoles.Administrator, UserRoles.Spravce, UserRoles.Uzivatel };
            if (newRoles.Any(nr => !allSystemRoles.Contains(nr)))
            {
                var invalidRoles = newRoles.Where(nr => !allSystemRoles.Contains(nr));
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = $"Následující role jsou neplatné: {string.Join(", ", invalidRoles)}" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new AuthResponseDto { IsSuccess = false, Message = "Uživatel nebyl nalezen." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isCurrentUserAdmin = User.IsInRole(UserRoles.Administrator);

            // Správce nemůže měnit role Administrátorům ani sobě samotnému, pokud je také Administrátor
            if (!isCurrentUserAdmin && (await _userManager.IsInRoleAsync(user, UserRoles.Administrator) || user.Id == currentUserId))
            {
                if (await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
                {
                    return Forbid(); // Správce nemůže měnit role Administrátorům
                }
                // Správce si nemůže sám sobě měnit role, pokud by se snažil přidat/odebrat Administrátora
                // nebo pokud by si chtěl odebrat roli Správce (což by ho zbavilo přístupu)
                if (newRoles.Contains(UserRoles.Administrator) || !newRoles.Contains(UserRoles.Spravce))
                {
                    // Tento blok je pro případ, kdyby si správce zkoušel manipulovat s rolí Administrátor
                    // nebo si odebrat vlastní roli Správce.
                    // V praxi by správce neměl mít možnost si přidat roli Administrátor.
                    // A neměl by si odebrat roli Správce, pokud je to jeho jediná "vyšší" role.
                    // Pro jednoduchost zde můžeme obecně zakázat správci měnit role administrátorů.
                }
            }


            // Zabráníme odebrání role Administrátor poslednímu administrátorovi nebo sobě samému, pokud je to poslední admin
            if (user.Id == currentUserId && (await _userManager.IsInRoleAsync(user, UserRoles.Administrator)) && !newRoles.Contains(UserRoles.Administrator))
            {
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Administrator);
                if (admins.Count(a => a.Id != user.Id) == 0)
                {
                    return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Nemůžete odebrat roli Administrátor poslednímu administrátorovi v systému (sám sobě)." });
                }
            }

            if (user.Id != currentUserId && (await _userManager.IsInRoleAsync(user, UserRoles.Administrator)) && !newRoles.Contains(UserRoles.Administrator))
            {
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Administrator);
                if (admins.Count == 1 && admins.First().Id == user.Id)
                {
                    return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Nemůžete odebrat roli Administrátor jedinému administrátorovi v systému." });
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(newRoles).ToList();
            var rolesToAdd = newRoles.Except(currentRoles).ToList();

            // Správce nemůže přidat roli Administrátor
            if (!isCurrentUserAdmin && rolesToAdd.Contains(UserRoles.Administrator))
            {
                return Forbid();
            }
            // Správce nemůže odebrat roli Administrátor
            if (!isCurrentUserAdmin && rolesToRemove.Contains(UserRoles.Administrator) && currentRoles.Contains(UserRoles.Administrator))
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
                _logger.LogError($"UpdateUserRoles: Nepodařilo se odebrat role uživateli ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = $"Chyba při odebírání rolí: {string.Join(", ", errors)}" });
            }

            IdentityResult addResult = IdentityResult.Success;
            if (rolesToAdd.Any())
            {
                addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (!addResult.Succeeded)
            {
                var errors = addResult.Errors.Select(e => e.Description);
                _logger.LogError($"UpdateUserRoles: Nepodařilo se přidat role uživateli ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                if (rolesToRemove.Any()) await _userManager.AddToRolesAsync(user, rolesToRemove); // Rollback
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = $"Chyba při přidávání rolí: {string.Join(", ", errors)}" });
            }

            _logger.LogInformation($"Role pro uživatele ID '{userId}' (Přezdívka: {user.UserName}) byly aktualizovány na: {string.Join(", ", newRoles)} administrátorem ID '{currentUserId}'.");
            return Ok(new AuthResponseDto { IsSuccess = true, Message = "Role uživatele byly úspěšně aktualizovány." });
        }

        // DELETE: api/admin/user/{userId}
        /// <summary>
        /// Smaže zadaného uživatele.
        /// Přístupné pro Administrátora a Správce.
        /// </summary>
        /// <param name="userId">ID uživatele k smazání.</param>
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

            // Správce nemůže smazat Administrátora
            if (!isCurrentUserAdmin && await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
            {
                return Forbid();
            }

            if (await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
            {
                var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Administrator);
                if (admins.Count <= 1)
                {
                    return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Nemůžete smazat posledního administrátora." });
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogError($"DeleteUser: Nepodařilo se smazat uživatele ID '{userId}'. Chyby: {string.Join(", ", errors)}");
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = $"Chyba při mazání uživatele: {string.Join(", ", errors)}" });
            }

            _logger.LogInformation($"Uživatel ID '{userId}' (Přezdívka: {user.UserName}) byl smazán administrátorem/správcem ID '{currentUserId}'.");
            return Ok(new AuthResponseDto { IsSuccess = true, Message = "Uživatel byl úspěšně smazán." });
        }


        // Stávající metoda ResetDatabase - nyní přístupná pouze Administrátorovi
        /// <summary>
        /// Resetuje databázi (extrémně nebezpečná operace).
        /// Přístupné pouze pro Administrátora.
        /// </summary>
        [HttpDelete("database/reset")]
        [Authorize(Roles = UserRoles.Administrator)] // Specifická autorizace pouze pro Administrátora
        public async Task<IActionResult> ResetDatabase()
        {
            _logger.LogCritical("Pokus o reset databáze administrátorem: {AdminName} (ID: {AdminId})",
                User.Identity?.Name ?? "Neznámý",
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Neznámé ID");

            // TATO AKCE JE EXTRÉMNĚ NEBEZPEČNÁ A MĚLA BY BÝT ZABEZPEČENA DALŠÍMI KROKY!
            // V produkčním prostředí by tato metoda měla být odstraněna nebo velmi silně chráněna.
            // Implementace resetu databáze je specifická pro daný projekt a databázový systém.
            // Příklad: smazání všech dat, znovuvytvoření schématu, seedování...
            // Prozatím vrací pouze potvrzení o zaznamenání pokusu.
            return Ok(new AuthResponseDto { IsSuccess = true, Message = "Operace resetu databáze byla zaznamenána (skutečná implementace vyžaduje mimořádnou opatrnost a není zde provedena)." });
        }
    }
}
