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

        /// <summary>
        /// Registers a new user with the provided registration data.
        /// </summary>
        /// <param name="registerDto">The registration data including nickname, email, password, and password confirmation.</param>
        /// <returns>
        /// Returns <see cref="AuthResponseDto"/> with authentication details if registration is successful.
        /// Returns <see cref="AuthResponseDto"/> with error message if registration fails.
        /// </returns>
        /// <response code="200">Registration was successful. Returns authentication details.</response>
        /// <response code="400">Registration failed due to invalid data or user already exists.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new AuthResponseDto { IsSuccess = false, Message = "Neplatná data.", UserInfo = null });
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUserByEmail != null)
            {
                return BadRequest(new AuthResponseDto
                    { IsSuccess = false, Message = "Uživatel s tímto emailem již existuje.", UserInfo = null });
            }

            var existingUserByNickname = await _userManager.FindByNameAsync(registerDto.Nickname);
            if (existingUserByNickname != null)
            {
                return BadRequest(new AuthResponseDto
                    { IsSuccess = false, Message = "Uživatel s touto přezdívkou již existuje.", UserInfo = null });
            }

            var newUser = new IdentityUser
            {
                UserName = registerDto.Nickname,
                Email = registerDto.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogWarning("Neuspesna registrace pro email {UserEmail}. Chyby: {Errors}", registerDto.Email,
                    string.Join(", ", errors));
                return BadRequest(new AuthResponseDto
                    { IsSuccess = false, Message = string.Join(", ", errors), UserInfo = null });
            }

            // add role Uzivatel to the new user
            var roleResult = await _userManager.AddToRoleAsync(newUser, RespawnApi.Domain.Enums.UserRoles.Uzivatel);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Nepodarilo se priradit roli Uzivatel uzivateli {UserName}.", newUser.UserName);
            }


            var userProfile = new UserProfile
            {
                UserId = newUser.Id,
                Nickname = newUser.UserName,
                AvatarUrl = "https://gravatar.com/avatar/fd8b6f7225afbec5a72e6b1519303e46?s=400&d=robohash&r=x"
            };
            await _userProfileRepository.AddAsync(userProfile);


            _logger.LogInformation("Uzivatel {UserName} byl uspesne zaregistrovan a byla mu prirazena role Uzivatel.",
                newUser.UserName);

            var tokenResponse = await _tokenService.GenerateTokenAsync(newUser, userProfile);
            tokenResponse.Message = "Registrace byla úspěšná.";

            return Ok(tokenResponse);
        }

        /// <summary>
        /// Authenticates a user with the provided login credentials.
        /// </summary>
        /// <param name="loginDto">The login data including nickname and password.</param>
        /// <returns>
        /// Returns <see cref="AuthResponseDto"/> with authentication details if login is successful.
        /// Returns <see cref="AuthResponseDto"/> with error message if login fails.
        /// </returns>
        /// <response code="200">Login was successful. Returns authentication details.</response>
        /// <response code="400">Login failed due to invalid data.</response>
        /// <response code="401">Login failed due to invalid credentials.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new AuthResponseDto { IsSuccess = false, Message = "Neplatná data.", UserInfo = null });
            }

            var user = await _userManager.FindByNameAsync(loginDto
                .Nickname);
            if (user == null)
            {
                _logger.LogWarning($"Pokus o přihlášení pro neexistujícího uživatele: {loginDto.Nickname}");
                return Unauthorized(new AuthResponseDto
                    { IsSuccess = false, Message = "Neplatné přihlašovací údaje.", UserInfo = null });
            }

            var result =
                await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Neúspěšný pokus o přihlášení pro uživatele: {loginDto.Nickname}");
                return Unauthorized(new AuthResponseDto
                    { IsSuccess = false, Message = "Neplatné přihlašovací údaje.", UserInfo = null });
            }

            var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);
            var tokenResponse = await _tokenService.GenerateTokenAsync(user, userProfile);

            _logger.LogInformation($"Uživatel {user.UserName} se úspěšně přihlásil.");
            tokenResponse.Message = "Přihlášení bylo úspěšné.";

            return Ok(tokenResponse);
        }
    }
}