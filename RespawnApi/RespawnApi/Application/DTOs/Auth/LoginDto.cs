using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Přezdívka je povinná.")]
        public string Nickname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Heslo je povinné.")]
        public string Password { get; set; } = string.Empty;
    }
}
