namespace RespawnApi.Application.DTOs.Polls
{
    /// <summary>
    /// Represents the data transfer object for a poll option.
    /// </summary>
    public class PollOptionDto
    {
        /// <summary>
        /// The unique identifier for the poll option, typically a GUID or a string.
        /// </summary>
        public string OptionId { get; set; } = string.Empty;

        /// <summary>
        /// The text of the poll option, which is the choice that users can vote for.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the image associated with the poll option, which can be used to visually represent the option.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// The number of votes that this option has received, which is used to display the popularity of the option in the poll results.
        /// </summary>
        public int VoteCount { get; set; }
    }
}