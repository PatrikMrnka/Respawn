using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    /// <summary>
    /// Represents the data transfer object for updating an existing poll option.
    /// </summary>
    public class UpdatePollOptionDto
    {
        /// <summary>
        /// The unique identifier for the poll option, typically a GUID or a string.
        /// </summary>
        public string? OptionId { get; set; }

        /// <summary>
        /// The text of the poll option, which is the choice that users can vote for.
        /// </summary>
        [Required(ErrorMessage = "Text možnosti je povinný.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Text možnosti musí mít 1-200 znaků.")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the image associated with the poll option, which can be used to visually represent the option.
        /// </summary>
        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }
    }
}
