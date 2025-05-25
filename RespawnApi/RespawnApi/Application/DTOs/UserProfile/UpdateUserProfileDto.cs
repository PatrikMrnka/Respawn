using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.UserProfile
{
    public class UpdateUserProfileDto
    {
        [Required(ErrorMessage = "Přezdívka je povinná.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Přezdívka musí mít 3 až 50 znaků.")]
        public string Nickname { get; set; } = string.Empty; // Přezdívka uživatele, např. "Player123"

        [StringLength(500, ErrorMessage = "URL avataru nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL avataru.")] // Základní validace URL
        public string? AvatarUrl { get; set; } // URL na avatar uživatele, např. "https://example.com/avatar.png"
    }
}
