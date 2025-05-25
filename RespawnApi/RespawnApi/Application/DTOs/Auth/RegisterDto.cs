using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Auth
{
    /// <summary>
    /// Represents the data required for user registration operations.
    /// </summary>
    public class RegisterDto
    {
        /// <summary>
        /// The nickname of the user registering for the application.
        /// </summary>
        [Required(ErrorMessage = "Přezdívka je povinná!")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Přezdívka musí mít 3 až 50 znaků.")]
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the user registering for the application.
        /// </summary>
        [Required(ErrorMessage = "Email je povinný!")]
        [EmailAddress(ErrorMessage = "Neplatný formát emailu.")]
        [StringLength(100, ErrorMessage = "Email nesmí být delší než 100 znaků.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The password of the user registering for the application.
        /// </summary>
        [Required(ErrorMessage = "Heslo je povinné!")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Heslo musí mít alespoň 5 znaků.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// The confirmation of the password for the user registering for the application.
        /// </summary>
        [Required(ErrorMessage = "Potvrzení hesla je povinné.")]
        [Compare("Password", ErrorMessage = "Hesla se neshodují.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
