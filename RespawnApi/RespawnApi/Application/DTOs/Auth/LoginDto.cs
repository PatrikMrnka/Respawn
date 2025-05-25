using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Auth
{
    /// <summary>
    /// Represents the data required for user login operations.
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// The nickname of the user attempting to log in.
        /// </summary>
        [Required(ErrorMessage = "Přezdívka je povinná.")]
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// The password of the user attempting to log in.
        /// </summary>
        [Required(ErrorMessage = "Heslo je povinné.")]
        public string Password { get; set; } = string.Empty;
    }
}
