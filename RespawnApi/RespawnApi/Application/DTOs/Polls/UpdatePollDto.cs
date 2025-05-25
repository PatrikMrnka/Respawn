using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    /// <summary>
    /// Represents the data transfer object for updating an existing poll.
    /// </summary>
    public class UpdatePollDto
    {
        /// <summary>
        /// The question or title of the poll, which is required and must be between 5 and 500 characters long.
        /// </summary>
        [Required(ErrorMessage = "Otázka ankety je povinná.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Otázka musí mít 5-500 znaků.")]
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// The end time of the poll, which is required and must be a future date and time.
        /// </summary>
        [Required(ErrorMessage = "Čas ukončení je povinný.")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// The URL of the image associated with the poll, which is optional and must be a valid URL if provided.
        /// </summary>
        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// The list of options for the poll, which is required and must contain at least two options.
        /// </summary>
        [Required(ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        [MinLength(2, ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        public List<UpdatePollOptionDto> Options { get; set; } = new List<UpdatePollOptionDto>();
    }
}
