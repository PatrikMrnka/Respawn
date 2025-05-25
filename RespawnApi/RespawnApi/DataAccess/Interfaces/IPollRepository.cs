using RespawnApi.Domain.Entities;

namespace RespawnApi.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for the Poll repository, defining data access operations for Polls.
    /// </summary>
    public interface IPollRepository
    {
        /// <summary>
        /// Retrieves a poll by its unique identifier, optionally including user-specific vote information.
        /// Related entities (Creator, Options, Votes) are expected to be included.
        /// </summary>
        /// <param name="pollId">The unique identifier of the poll.</param>
        /// <param name="currentUserId">Optional. The ID of the current user to fetch their specific vote information for this poll.</param>
        /// <returns>The <see cref="Poll"/> entity if found; otherwise, null.</returns>
        Task<Poll?> GetByIdAsync(string pollId, string? currentUserId = null);

        /// <summary>
        /// Retrieves all polls, optionally including user-specific vote information for each poll.
        /// Related entities (Creator, Options, Votes) are expected to be included.
        /// Polls are typically ordered by a default criterion (e.g., creation date or end time).
        /// </summary>
        /// <param name="currentUserId">Optional. The ID of the current user to fetch their specific vote information for all polls.</param>
        /// <returns>An enumerable collection of <see cref="Poll"/> entities.</returns>
        Task<IEnumerable<Poll>> GetAllAsync(string? currentUserId = null);

        /// <summary>
        /// Adds a new poll to the data store.
        /// </summary>
        /// <param name="poll">The <see cref="Poll"/> entity to add.</param>
        /// <returns>The added <see cref="Poll"/> entity, typically with an assigned ID.</returns>
        Task<Poll> AddAsync(Poll poll);

        /// <summary>
        /// Updates an existing poll in the data store.
        /// </summary>
        /// <param name="poll">The <see cref="Poll"/> entity to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(Poll poll);

        /// <summary>
        /// Deletes a poll from the data store by its unique identifier.
        /// </summary>
        /// <param name="pollId">The unique identifier of the poll to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string pollId);

        /// <summary>
        /// Retrieves all votes cast by a specific user for a specific poll.
        /// </summary>
        /// <param name="pollId">The ID of the poll.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An enumerable collection of <see cref="PollVote"/> entities.</returns>
        Task<IEnumerable<PollVote>> GetUserVotesForPollAsync(string pollId, string userId);

        /// <summary>
        /// Adds a new vote to the data store.
        /// </summary>
        /// <param name="vote">The <see cref="PollVote"/> entity to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddVoteAsync(PollVote vote);

        /// <summary>
        /// Adds multiple votes to the data store.
        /// </summary>
        /// <param name="votes">The collection of <see cref="PollVote"/> entities to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddVotesAsync(IEnumerable<PollVote> votes);

        /// <summary>
        /// Removes a collection of votes from the data store.
        /// </summary>
        /// <param name="votes">The collection of <see cref="PollVote"/> entities to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveVotesAsync(IEnumerable<PollVote> votes);

        /// <summary>
        /// Saves all changes made in the context to the database.
        /// </summary>
        /// <returns>A task representing the asynchronous save operation.</returns>
        Task<int> SaveChangesAsync();
    }
}