using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Přezdívka je povinná!")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Přezdívka musí mít 3 až 50 znaků.")]
        public string Nickname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email je povinný!")]
        [EmailAddress(ErrorMessage = "Neplatný formát emailu.")]
        [StringLength(100, ErrorMessage = "Email nesmí být delší než 100 znaků.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Heslo je povinné!")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Heslo musí mít alespoň 5 znaků.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potvrzení hesla je povinné.")]
        [Compare("Password", ErrorMessage = "Hesla se neshodují.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
