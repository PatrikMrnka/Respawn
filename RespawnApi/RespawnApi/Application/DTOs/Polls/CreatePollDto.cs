using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    public class CreatePollDto
    {
        [Required(ErrorMessage = "Otázka ankety je povinná.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Otázka musí mít 5-500 znaků.")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Čas ukončení je povinný.")]
        public DateTime EndTime { get; set; }

        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        [MinLength(2, ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        public List<CreatePollOptionDto> Options { get; set; } = new List<CreatePollOptionDto>();

        public bool IsMultipleChoice { get; set; } = false;
    }
}
