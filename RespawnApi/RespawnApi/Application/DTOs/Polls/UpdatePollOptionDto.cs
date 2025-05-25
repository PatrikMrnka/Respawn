using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    public class UpdatePollOptionDto
    {
        public string? OptionId { get; set; }

        [Required(ErrorMessage = "Text možnosti je povinný.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Text možnosti musí mít 1-200 znaků.")]
        public string Text { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }
    }
}
