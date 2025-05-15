using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RespawnApi.Application.DTOs.Auth;
using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ITokenService tokenService,
            IUserProfileRepository userProfileRepository,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _userProfileRepository = userProfileRepository;
            _logger = logger;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Neplatná data.", UserInfo = null });
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUserByEmail != null)
            {
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Uživatel s tímto emailem již existuje.", UserInfo = null });
            }

            var existingUserByNickname = await _userManager.FindByNameAsync(registerDto.Nickname);
            if (existingUserByNickname != null)
            {
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Uživatel s touto přezdívkou již existuje.", UserInfo = null });
            }

            var newUser = new IdentityUser
            {
                UserName = registerDto.Nickname, // Nickname bude UserName
                Email = registerDto.Email,
                EmailConfirmed = true // Prozatím potvrzujeme email automaticky
            };

            var result = await _userManager.CreateAsync(newUser, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = string.Join(", ", errors), UserInfo = null });
            }

            // Vytvoření UserProfile
            var userProfile = new UserProfile
            {
                UserId = newUser.Id,
                Nickname = newUser.UserName, // Nickname z IdentityUser
                AvatarUrl = "" // Výchozí nebo prázdný avatar
            };
            await _userProfileRepository.AddAsync(userProfile);

            // TODO: Přiřadit výchozí roli (např. "User")
            // await _userManager.AddToRoleAsync(newUser, "User");

            _logger.LogInformation($"Uživatel {newUser.UserName} byl úspěšně zaregistrován.");

            // Možnost automatického přihlášení po registraci a vrácení tokenu
            // Nebo jen zpráva o úspěchu a uživatel se musí přihlásit zvlášť
            var tokenResponse = await _tokenService.GenerateTokenAsync(newUser, userProfile);
            tokenResponse.Message = "Registrace byla úspěšná.";
            return Ok(tokenResponse);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto { IsSuccess = false, Message = "Neplatná data.", UserInfo = null });
            }

            var user = await _userManager.FindByNameAsync(loginDto.Nickname); // Hledání podle UserName (což je naše Nickname)
            if (user == null)
            {
                _logger.LogWarning($"Pokus o přihlášení pro neexistujícího uživatele: {loginDto.Nickname}");
                return Unauthorized(new AuthResponseDto { IsSuccess = false, Message = "Neplatné přihlašovací údaje.", UserInfo = null });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Neúspěšný pokus o přihlášení pro uživatele: {loginDto.Nickname}");
                return Unauthorized(new AuthResponseDto { IsSuccess = false, Message = "Neplatné přihlašovací údaje.", UserInfo = null });
            }

            var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);
            var tokenResponse = await _tokenService.GenerateTokenAsync(user, userProfile);

            _logger.LogInformation($"Uživatel {user.UserName} se úspěšně přihlásil.");
            tokenResponse.Message = "Přihlášení bylo úspěšné.";
            return Ok(tokenResponse);
        }
    }
}
