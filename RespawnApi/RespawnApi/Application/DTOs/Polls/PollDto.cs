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
