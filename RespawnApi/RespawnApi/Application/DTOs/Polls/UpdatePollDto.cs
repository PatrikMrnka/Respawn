using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    public class UpdatePollDto
    {
        [Required(ErrorMessage = "Otázka ankety je povinná.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Otázka musí mít 5-500 znaků.")]
        public string Question { get; set; } = string.Empty; // Otázka ankety

        [Required(ErrorMessage = "Čas ukončení je povinný.")]
        public DateTime EndTime { get; set; } // Čas ukončení ankety

        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; } // URL obrázku ankety (volitelné, může být null nebo prázdný)

        [Required(ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        [MinLength(2, ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        public List<UpdatePollOptionDto> Options { get; set; } = new List<UpdatePollOptionDto>(); // Seznam možností ankety
    }
}
