// Application/DTOs/Polls/PollOptionDto.cs
using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    public class UpdatePollOptionDto
    {
        public string? OptionId { get; set; } // Null or empty for new options, existing ID for updates/keeps

        [Required(ErrorMessage = "Text možnosti je povinný.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Text možnosti musí mít 1-200 znaků.")]
        public string Text { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }
    }
}




namespace RespawnApi.Application.DTOs.Polls
{
    public class PollOptionDto
    {
        public string OptionId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int VoteCount { get; set; } // Počet hlasů pro tuto možnost
    }
}

// Application/DTOs/Polls/PollDto.cs
namespace RespawnApi.Application.DTOs.Polls
{
    public class PollDto
    {
        public string PollId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public DateTime EndTime { get; set; }
        public bool IsClosed { get; set; }
        public string? ImageUrl { get; set; }
        public string CreatorUserId { get; set; } = string.Empty;
        public string CreatorNickname { get; set; } = string.Empty; // Přezdívka tvůrce
        public bool IsMultipleChoice { get; set; }
        public List<PollOptionDto> Options { get; set; } = new List<PollOptionDto>();
        public List<string>? UserVotedOptionIds { get; set; } // Seznam ID možností, pro které uživatel hlasoval
        public int TotalVotes { get; set; } // Celkový počet hlasů v anketě
    }
}

// Application/DTOs/Polls/CreatePollOptionDto.cs
namespace RespawnApi.Application.DTOs.Polls
{
    public class CreatePollOptionDto
    {
        [Required(ErrorMessage = "Text možnosti je povinný.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Text možnosti musí mít 1-200 znaků.")]
        public string Text { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }
    }
}

// Application/DTOs/Polls/CreatePollDto.cs
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

namespace RespawnApi.Application.DTOs.Polls
{
    public class UpdatePollDto
    {
        [Required(ErrorMessage = "Otázka ankety je povinná.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Otázka musí mít 5-500 znaků.")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Čas ukončení je povinný.")]
        public DateTime EndTime { get; set; }

        [StringLength(500, ErrorMessage = "URL obrázku nesmí být delší než 500 znaků.")]
        [Url(ErrorMessage = "Neplatný formát URL obrázku.")]
        public string? ImageUrl { get; set; }

        // Seznam všech možností, jak mají vypadat po úpravě.
        // Pokud OptionId chybí (nebo je null/prázdný), jedná se o novou možnost.
        // Pokud OptionId existuje, jedná se o úpravu existující možnosti.
        // Možnosti, které byly v DB, ale nejsou v tomto seznamu (s platným OptionId), budou smazány (pokud anketa nemá hlasy).
        [Required(ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        [MinLength(2, ErrorMessage = "Anketa musí mít alespoň dvě možnosti.")]
        public List<UpdatePollOptionDto> Options { get; set; } = new List<UpdatePollOptionDto>();

        // IsMultipleChoice se typicky nemění po vytvoření ankety, aby se nenarušila logika hlasování.
        // Pokud by se mělo měnit, vyžadovalo by to reset hlasů. Prozatím ponecháváme neměnné.
        // public bool IsMultipleChoice { get; set; }
    }
}

// Application/DTOs/Polls/SubmitVoteDto.cs
namespace RespawnApi.Application.DTOs.Polls
{
    public class SubmitVoteDto
    {
        [Required(ErrorMessage = "Musíte vybrat alespoň jednu možnost.")]
        [MinLength(1, ErrorMessage = "Musíte vybrat alespoň jednu možnost.")]
        public List<string> OptionIds { get; set; } = new List<string>();
    }
}
