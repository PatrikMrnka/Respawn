using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    /// <summary>
    /// Represents the data transfer object for creating a new poll option.
    /// </summary>
    public class CreatePollOptionDto
    {
        /// <summary>
        /// The text of the poll option, which is required and must be between 1 and 200 characters long.
        /// </summary>
        [Required(ErrorMessage = "Text možnosti je povinný.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Text možnosti musí mít 1-200 znaků.")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the image associated with the poll option, which is optional and must be a valid URL if provided.
        /// </summary>
        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }
    }
}
