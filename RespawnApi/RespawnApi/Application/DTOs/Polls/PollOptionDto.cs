namespace RespawnApi.Application.DTOs.Polls
{
    public class PollOptionDto
    {
        public string OptionId { get; set; } = string.Empty; // Id možnosti
        public string Text { get; set; } = string.Empty; // Text možnosti
        public string? ImageUrl { get; set; } // URL obrázku pro možnost (volitelné)
        public int VoteCount { get; set; } // Počet hlasů pro tuto možnost
    }
}