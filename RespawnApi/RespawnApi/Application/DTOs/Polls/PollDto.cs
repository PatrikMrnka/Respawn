namespace RespawnApi.Application.DTOs.Polls
{
    /// <summary>
    /// Represents the data transfer object for a poll, including its options and voting details.
    /// </summary>
    public class PollDto
    {
        /// <summary>
        /// The unique identifier for the poll, typically a GUID or a string.
        /// </summary>
        public string PollId { get; set; } = string.Empty;

        /// <summary>
        /// The title or question of the poll, which is the main topic that users will vote on.
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// The date and time when the poll ends, which is required for determining the voting period.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Indicates whether the poll is closed, meaning no further votes can be cast. This is typically set to true when the end time has passed.
        /// </summary>
        public bool IsClosed { get; set; }

        /// <summary>
        /// The URL of the image associated with the poll, which can be used to visually represent the poll question or theme.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// The unique identifier of the user who created the poll, typically a GUID or a string.
        /// </summary>
        public string CreatorUserId { get; set; } = string.Empty;

        /// <summary>
        /// The nickname of the user who created the poll, which is used to display the creator's name in a user-friendly format.
        /// </summary>
        public string CreatorNickname { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the poll allows multiple choices. If true, users can select more than one option; if false, users can only select one option.
        /// </summary>
        public bool IsMultipleChoice { get; set; }

        /// <summary>
        /// The list of options available in the poll, where each option represents a choice that users can vote for.
        /// </summary>
        public List<PollOptionDto> Options { get; set; } = new List<PollOptionDto>();

        /// <summary>
        /// The list of option IDs that the user has voted for. This is used to track the user's selections in the poll.
        /// </summary>
        public List<string>? UserVotedOptionIds { get; set; }

        /// <summary>
        /// The total number of votes cast in the poll, which is used to display the overall participation level.
        /// </summary>
        public int TotalVotes { get; set; }
    }
}
