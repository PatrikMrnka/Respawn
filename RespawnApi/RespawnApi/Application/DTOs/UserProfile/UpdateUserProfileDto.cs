using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.UserProfile
{
    /// <summary>
    /// Represents the data transfer object for updating a user's profile.
    /// </summary>
    public class UpdateUserProfileDto
    {
        /// <summary>
        /// The nickname of the user, which is required and must be between 3 and 50 characters long.
        /// </summary>
        [Required(ErrorMessage = "Přezdívka je povinná.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Přezdívka musí mít 3 až 50 znaků.")]
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// The avatar URL of the user, which is optional and must be a valid URL if provided.
        /// </summary>
        [StringLength(500, ErrorMessage = "URL avataru nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL avataru.")] // Základní validace URL
        public string? AvatarUrl { get; set; }
    }
}
