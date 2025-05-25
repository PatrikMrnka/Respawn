using Microsoft.EntityFrameworkCore;
using RespawnApi.Data;
using RespawnApi.Domain.Entities;
using RespawnApi.DataAccess.Interfaces;

namespace RespawnApi.DataAccess.Repositories
{
    /// <summary>
    /// Repository for managing Poll entities and their related data.
    /// </summary>
    public class PollRepository : IPollRepository
    {
        private readonly RespawnDbContext _context;
        private readonly ILogger<PollRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public PollRepository(RespawnDbContext context, ILogger<PollRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Poll?> GetByIdAsync(string pollId, string? currentUserId = null)
        {
            _logger.LogDebug("Attempting to retrieve poll with ID: {PollId}, for UserID: {UserId}", pollId, currentUserId ?? "N/A");
            var poll = await _context.Polls
                .Include(p => p.Creator) // Eager load the creator (UserProfile)
                .Include(p => p.PollOptions) // Eager load poll options
                .Include(p => p.PollVotes) // Eager load all votes for this poll
                .AsNoTracking() // Use AsNoTracking for read operations if entity is not modified here
                .FirstOrDefaultAsync(p => p.PollId == pollId);

            if (poll != null && !string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogDebug("Poll {PollId} found. User-specific vote info will be determined during DTO mapping.", pollId);
            }
            else if (poll == null)
            {
                _logger.LogWarning("Poll with ID: {PollId} not found.", pollId);
            }
            return poll;
        }

        public async Task<IEnumerable<Poll>> GetAllAsync(string? currentUserId = null)
        {
            _logger.LogDebug("Attempting to retrieve all polls, for UserID: {UserId}", currentUserId ?? "N/A");
            var polls = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .OrderByDescending(p => p.EndTime)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("Retrieved {PollCount} polls from database.", polls.Count);

            return polls;
        }

        public async Task<Poll> AddAsync(Poll poll)
        {
            if (poll == null) throw new ArgumentNullException(nameof(poll));
            _logger.LogInformation("Adding new poll: {PollQuestion}", poll.Question);

            foreach (var option in poll.PollOptions)
            {
                if (string.IsNullOrEmpty(option.PollId))
                {
                    option.PollId = poll.PollId;
                }
            }

            await _context.Polls.AddAsync(poll);
            return poll;
        }

        public Task UpdateAsync(Poll poll)
        {
            if (poll == null) throw new ArgumentNullException(nameof(poll));
            _logger.LogInformation("Updating poll with ID: {PollId}", poll.PollId);
            _context.Polls.Update(poll);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(string pollId)
        {
            _logger.LogInformation("Attempting to delete poll with ID: {PollId}", pollId);
            var poll = await _context.Polls.FindAsync(pollId);
            if (poll != null)
            {
                _context.Polls.Remove(poll);
                _logger.LogInformation("Poll {PollId} marked for deletion.", pollId);
            }
            else
            {
                _logger.LogWarning("Poll with ID: {PollId} not found for deletion.", pollId);
            }
        }

        public async Task<IEnumerable<PollVote>> GetUserVotesForPollAsync(string pollId, string userId)
        {
            _logger.LogDebug("Retrieving votes for UserID: {UserId} on PollID: {PollId}", userId, pollId);
            return await _context.PollVotes
                .Where(v => v.PollId == pollId && v.UserId == userId)
                .ToListAsync();
        }

        public async Task AddVoteAsync(PollVote vote)
        {
            if (vote == null) throw new ArgumentNullException(nameof(vote));
            _logger.LogDebug("Adding single vote for UserID: {UserId} on PollID: {PollId} for OptionID: {OptionId}", vote.UserId, vote.PollId, vote.OptionId);
            await _context.PollVotes.AddAsync(vote);
        }

        public async Task AddVotesAsync(IEnumerable<PollVote> votes)
        {
            if (votes == null || !votes.Any()) throw new ArgumentNullException(nameof(votes));
            _logger.LogDebug("Adding {VoteCount} votes.", votes.Count());
            await _context.PollVotes.AddRangeAsync(votes);
        }


        public Task RemoveVotesAsync(IEnumerable<PollVote> votes)
        {
            if (votes == null || !votes.Any()) throw new ArgumentNullException(nameof(votes));
            _logger.LogDebug("Removing {VoteCount} votes.", votes.Count());
            _context.PollVotes.RemoveRange(votes);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            _logger.LogInformation("Saving changes to the database.");
            return await _context.SaveChangesAsync();
        }
    }
}
