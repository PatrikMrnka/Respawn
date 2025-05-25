using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.Polls
{
    /// <summary>
    /// Represents the data transfer object for submitting a vote in a poll.
    /// </summary>
    public class SubmitVoteDto
    {
        /// <summary>
        /// The unique identifiers of the poll options that the user has selected.
        /// </summary>
        [Required(ErrorMessage = "Musíte vybrat alespoň jednu možnost.")]
        [MinLength(1, ErrorMessage = "Musíte vybrat alespoň jednu možnost.")]
        public List<string> OptionIds { get; set; } = new List<string>();
    }
}
