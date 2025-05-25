namespace RespawnApi.Application.DTOs.Polls
{
    /// <summary>
    /// Represents the data transfer object for opening a poll that has been closed.
    /// </summary>
    public class OpenPollRequestDto
    {
        /// <summary>
        /// The unique identifier for the poll that is being reopened, typically a GUID or a string.
        /// </summary>
        public DateTime NewEndTime { get; set; }
    }
}
