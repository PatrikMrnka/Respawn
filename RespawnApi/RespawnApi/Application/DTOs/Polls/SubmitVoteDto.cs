using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    public class SubmitVoteDto
    {
        [Required(ErrorMessage = "Musíte vybrat alespoň jednu možnost.")]
        [MinLength(1, ErrorMessage = "Musíte vybrat alespoň jednu možnost.")]
        public List<string> OptionIds { get; set; } = new List<string>(); // Id možností, které uživatel vybral
    }
}
