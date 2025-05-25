using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.Polls;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs;
using RespawnApi.DataAccess.Interfaces;
using System.Security.Claims;

namespace RespawnApi.Controllers
{
    /// <summary>
    /// API controller for managing polls.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All actions require authentication by default
    public class PollsController : ControllerBase
    {
        private readonly IPollRepository _pollRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PollsController> _logger;
        private readonly IHubContext<PollHub> _pollHubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollsController"/> class.
        /// </summary>
        /// <param name="pollRepository">The poll repository for data access.</param>
        /// <param name="userManager">ASP.NET Core Identity UserManager.</param>
        /// <param name="logger">Logger for this controller.</param>
        /// <param name="pollHubContext">SignalR hub context for poll updates.</param>
        public PollsController(
            IPollRepository pollRepository,
            UserManager<IdentityUser> userManager,
            ILogger<PollsController> logger,
            IHubContext<PollHub> pollHubContext)
        {
            _pollRepository = pollRepository;
            _userManager = userManager;
            _logger = logger;
            _pollHubContext = pollHubContext;
        }

        /// <summary>
        /// Maps a Poll entity to a PollDto, including user-specific vote information.
        /// </summary>
        /// <param name="poll">The Poll entity.</param>
        /// <param name="currentUserId">The ID of the current user, or null if no user context.</param>
        /// <returns>A PollDto.</returns>
        private PollDto MapPollToDto(Poll poll, string? currentUserId)
        {
            if (poll == null)
            {
                _logger.LogError("MapPollToDto received a null Poll entity.");
                throw new ArgumentNullException(nameof(poll), "Cannot map a null Poll entity.");
            }

            var userVotesForThisPoll = new List<string>();
            if (!string.IsNullOrEmpty(currentUserId) && poll.PollVotes != null)
            {
                userVotesForThisPoll = poll.PollVotes
                    .Where(pv => pv.UserId == currentUserId)
                    .Select(pv => pv.OptionId)
                    .ToList();
            }

            return new PollDto
            {
                PollId = poll.PollId,
                Question = poll.Question,
                EndTime = poll.EndTime,
                IsClosed = poll.IsClosed || (poll.EndTime <= DateTime.UtcNow),
                ImageUrl = poll.ImageUrl,
                CreatorUserId = poll.CreatorUserId,
                CreatorNickname = poll.Creator?.Nickname ?? "Neznámý",
                IsMultipleChoice = poll.IsMultipleChoice,
                Options = poll.PollOptions?.Select(opt => new PollOptionDto
                {
                    OptionId = opt.OptionId,
                    Text = opt.Text,
                    ImageUrl = opt.ImageUrl,
                    VoteCount = poll.PollVotes?.Count(v => v.OptionId == opt.OptionId) ?? 0
                }).ToList() ?? new List<PollOptionDto>(),
                UserVotedOptionIds = userVotesForThisPoll,
                TotalVotes = poll.PollVotes?.Count ?? 0
            };
        }

        /// <summary>
        /// Retrieves all polls.
        /// </summary>
        /// <returns>A list of all polls.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PollDto>>> GetPolls()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} retrieving all polls.", currentUserId ?? "Anonymous");

            var pollsFromDb = await _pollRepository.GetAllAsync(currentUserId);

            var pollDtos = pollsFromDb.Select(p => MapPollToDto(p, currentUserId)).ToList();

            // Sort on the client-side DTOs after mapping
            return Ok(pollDtos.OrderBy(p => p.IsClosed).ThenByDescending(p => p.EndTime).ToList());
        }

        /// <summary>
        /// Retrieves a specific poll by its ID.
        /// </summary>
        /// <param name="id">The ID of the poll to retrieve.</param>
        /// <returns>The requested poll or NotFound.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<PollDto>> GetPoll(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} retrieving poll with ID: {PollId}.", currentUserId ?? "Anonymous",
                id);

            var poll = await _pollRepository.GetByIdAsync(id, currentUserId);

            if (poll == null)
            {
                _logger.LogWarning("Poll with ID {PollId} not found.", id);
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            bool stateChangedDueToExpiry = false;
            if (!poll.IsClosed && (poll.EndTime <= DateTime.UtcNow))
            {
                _logger.LogInformation("Poll {PollId} found to be expired. Marking as closed.", poll.PollId);
                poll.IsClosed = true;
                await _pollRepository.UpdateAsync(poll);
                await _pollRepository.SaveChangesAsync(); // Commit the change
                stateChangedDueToExpiry = true;
            }

            var pollDtoToReturn = MapPollToDto(poll, currentUserId);

            if (stateChangedDueToExpiry)
            {
                _logger.LogInformation("Broadcasting update for automatically closed poll {PollId}.", poll.PollId);
                var pollDtoForBroadcast = MapPollToDto(poll, null); // Null user ID for generic broadcast DTO
                await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", pollDtoForBroadcast);
            }

            return Ok(pollDtoToReturn);
        }

        /// <summary>
        /// Creates a new poll.
        /// </summary>
        /// <param name="createPollDto">The data for the new poll.</param>
        /// <returns>The created poll.</returns>
        [HttpPost]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")] // Only Admins/Managers can create
        public async Task<ActionResult<PollDto>> CreatePoll(CreatePollDto createPollDto)
        {
            var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(creatorUserId))
            {
                return Unauthorized(new { message = "Pro vytvoření ankety musíte být přihlášeni." });
            }

            _logger.LogInformation("User {UserId} attempting to create a new poll: {Question}", creatorUserId,
                createPollDto.Question);

            if (createPollDto.EndTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(createPollDto.EndTime), "Čas ukončení ankety musí být v budoucnosti.");
            }

            if (createPollDto.Options == null || createPollDto.Options.Count < 2)
            {
                ModelState.AddModelError(nameof(createPollDto.Options), "Anketa musí mít alespoň dvě možnosti.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var pollEntity = new Poll
            {
                PollId = Guid.NewGuid().ToString(),
                Question = createPollDto.Question,
                EndTime = createPollDto.EndTime,
                ImageUrl = createPollDto.ImageUrl,
                CreatorUserId = creatorUserId,
                IsMultipleChoice = createPollDto.IsMultipleChoice,
                IsClosed = false,
                PollOptions = (createPollDto.Options ?? new List<CreatePollOptionDto>()).Select(optDto => new PollOption
                {
                    OptionId = Guid.NewGuid().ToString(),
                    Text = optDto.Text,
                    ImageUrl = optDto.ImageUrl
                }).ToList()
            };

            var addedPoll = await _pollRepository.AddAsync(pollEntity);
            await _pollRepository.SaveChangesAsync();
            _logger.LogInformation("User {UserId} created poll {PollId}.", creatorUserId, addedPoll.PollId);

            // Fetch the complete entity with includes for DTO mapping and broadcast
            var createdPollWithIncludes = await _pollRepository.GetByIdAsync(addedPoll.PollId, creatorUserId);
            if (createdPollWithIncludes == null)
            {
                _logger.LogError("Failed to retrieve newly created poll {PollId} with includes.", addedPoll.PollId);
                return StatusCode(500, "Chyba při načítání vytvořené ankety.");
            }


            var pollDtoForResponse = MapPollToDto(createdPollWithIncludes, creatorUserId);
            var pollDtoForBroadcast = MapPollToDto(createdPollWithIncludes, null); // Generic DTO for broadcast

            await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", pollDtoForBroadcast);

            return CreatedAtAction(nameof(GetPoll), new { id = addedPoll.PollId }, pollDtoForResponse);
        }

        /// <summary>
        /// Updates an existing poll.
        /// </summary>
        /// <param name="id">The ID of the poll to update.</param>
        /// <param name="updatePollDto">The updated poll data.</param>
        /// <returns>The updated poll or relevant error response.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePoll(string id, UpdatePollDto updatePollDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} attempting to update poll {PollId}.", currentUserId, id);

            var poll = await _pollRepository.GetByIdAsync(id, currentUserId);

            if (poll == null)
            {
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            bool isAdmin = User.IsInRole(UserRoles.Administrator);
            bool isManager = User.IsInRole(UserRoles.Spravce);
            bool canManageFully = isAdmin || isManager;
            bool isCreator = poll.CreatorUserId == currentUserId;

            if (!isCreator && !canManageFully)
            {
                _logger.LogWarning("User {UserId} forbidden to update poll {PollId}.", currentUserId, id);
                return Forbid();
            }

            bool optionsActuallyChanged = OptionsHaveChanged(poll.PollOptions, updatePollDto.Options);
            if (optionsActuallyChanged && poll.PollVotes.Any())
            {
                return BadRequest(new { message = "Změna možností u ankety s existujícími hlasy není povolena." });
            }

            bool wasPreviouslyOpenAndActive = !poll.IsClosed && (poll.EndTime > DateTime.UtcNow);
            bool isNowClosingByTime = updatePollDto.EndTime <= DateTime.UtcNow;

            poll.Question = updatePollDto.Question;
            poll.EndTime = updatePollDto.EndTime;
            poll.ImageUrl = updatePollDto.ImageUrl;

            if (wasPreviouslyOpenAndActive && isNowClosingByTime)
            {
                poll.IsClosed = true;
            }
            else if (updatePollDto.EndTime > DateTime.UtcNow)
            {
                // If admin/manager is setting a future end time, ensure it's open
                if (canManageFully)
                {
                    poll.IsClosed = false;
                }
                // If creator is setting a future end time for an already closed poll, it's not allowed unless admin/manager
                else if (poll.IsClosed)
                    return BadRequest(new
                        { message = "Běžný uživatel nemůže znovu otevřít uzavřenou anketu změnou času." });
            }

            if (optionsActuallyChanged && !poll.PollVotes.Any())
            {
                poll.PollOptions.Clear();
                foreach (var optDto in updatePollDto.Options)
                {
                    poll.PollOptions.Add(new PollOption
                    {
                        OptionId = string.IsNullOrEmpty(optDto.OptionId) ? Guid.NewGuid().ToString() : optDto.OptionId,
                        PollId = poll.PollId,
                        Text = optDto.Text,
                        ImageUrl = optDto.ImageUrl
                    });
                }
            }

            await _pollRepository.UpdateAsync(poll);
            await _pollRepository.SaveChangesAsync();
            _logger.LogInformation("Poll {PollId} updated by user {UserId}.", id, currentUserId);

            var updatedPollWithIncludes = await _pollRepository.GetByIdAsync(id, currentUserId);
            if (updatedPollWithIncludes == null)
                return NotFound(new { message = "Anketa nebyla nalezena po uložení." });

            var pollDtoForResponse = MapPollToDto(updatedPollWithIncludes, currentUserId);
            var pollDtoForBroadcast = MapPollToDto(updatedPollWithIncludes, null);
            await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", pollDtoForBroadcast);

            return Ok(pollDtoForResponse);
        }

        /// <summary>
        /// Helper to check if poll options have substantially changed.
        /// </summary>
        private bool OptionsHaveChanged(ICollection<PollOption> existingOptions,
            List<UpdatePollOptionDto> newOptionsDto)
        {
            if (existingOptions.Count != newOptionsDto.Count) return true;

            var existingOptionsProcessed = existingOptions.OrderBy(o => o.OptionId).ToList();
            var newOptionsDtoProcessed = newOptionsDto
                .Select(dto => new { dto.OptionId, dto.Text, dto.ImageUrl }) // Select relevant fields for comparison
                .OrderBy(o => o.OptionId) // Order by ID if present, otherwise order might be unstable
                .ToList();

            for (int i = 0; i < existingOptionsProcessed.Count; i++)
            {
                var existing = existingOptionsProcessed[i];
                var updated = newOptionsDtoProcessed[i];

                // If new option has no ID, it's considered a change (likely replacing all options)
                if (string.IsNullOrEmpty(updated.OptionId))
                {
                    return true;
                }

                // If IDs don't match at the same position (after sorting), it's a change
                if (existing.OptionId != updated.OptionId)
                {
                    return true;
                }

                // If text or image URL changed for an existing option
                if (existing.Text != updated.Text || existing.ImageUrl != updated.ImageUrl)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deletes a specific poll.
        /// </summary>
        /// <param name="id">The ID of the poll to delete.</param>
        [HttpDelete("{id}")]
        [Authorize(Roles =
            $"{UserRoles.Administrator},{UserRoles.Spravce},{UserRoles.Uzivatel}")] // Allow creator to delete
        public async Task<IActionResult> DeletePoll(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} attempting to delete poll {PollId}.", currentUserId, id);

            var poll = await _pollRepository.GetByIdAsync(id); // No need for user votes here
            if (poll == null)
            {
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            bool isAdmin = User.IsInRole(UserRoles.Administrator);
            bool isManager = User.IsInRole(UserRoles.Spravce);

            if (poll.CreatorUserId != currentUserId && !isAdmin && !isManager)
            {
                _logger.LogWarning("User {UserId} forbidden to delete poll {PollId} (not creator or admin/manager).",
                    currentUserId, id);
                return Forbid();
            }

            await _pollRepository.DeleteAsync(id);
            await _pollRepository.SaveChangesAsync();
            _logger.LogInformation("Poll {PollId} deleted by user {UserId}.", id, currentUserId);

            await _pollHubContext.Clients.All.SendAsync("ReceivePollDelete", id);
            return NoContent();
        }

        /// <summary>
        /// Submits a vote for a poll.
        /// </summary>
        /// <param name="pollId">The ID of the poll.</param>
        /// <param name="submitVoteDto">The DTO containing selected option IDs.</param>
        [HttpPost("{pollId}/vote")]
        public async Task<ActionResult<PollDto>> SubmitVote(string pollId, SubmitVoteDto submitVoteDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Pro hlasování musíte být přihlášeni." });
            }

            _logger.LogInformation("User {UserId} attempting to vote in poll {PollId} for options: {OptionIds}",
                userId, pollId, string.Join(",", submitVoteDto.OptionIds));

            var poll = await _pollRepository.GetByIdAsync(pollId, userId);
            if (poll == null)
            {
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            bool autoClosed = false;
            if (!poll.IsClosed && poll.EndTime <= DateTime.UtcNow)
            {
                poll.IsClosed = true;
                await _pollRepository.UpdateAsync(poll); // Update IsClosed flag
                // SaveChangesAsync will be called later after vote processing
                autoClosed = true;
            }

            if (poll.IsClosed)
            {
                if (autoClosed) await _pollRepository.SaveChangesAsync(); // Save the auto-close change
                _logger.LogWarning("User {UserId} attempt to vote in closed poll {PollId}.", userId, pollId);
                return BadRequest(new { message = "Tato anketa je již uzavřena." });
            }

            if (submitVoteDto.OptionIds == null || !submitVoteDto.OptionIds.Any())
            {
                return BadRequest(new { message = "Musíte vybrat alespoň jednu možnost." });
            }

            foreach (var optionId in submitVoteDto.OptionIds)
            {
                if (!poll.PollOptions.Any(opt => opt.OptionId == optionId))
                {
                    return BadRequest(new { message = $"Neplatná možnost hlasování: {optionId}" });
                }
            }

            if (!poll.IsMultipleChoice && submitVoteDto.OptionIds.Count > 1)
            {
                return BadRequest(new { message = "V této anketě můžete vybrat pouze jednu možnost." });
            }

            // Remove previous votes by this user for this poll
            var existingVotes = await _pollRepository.GetUserVotesForPollAsync(pollId, userId);
            if (existingVotes.Any())
            {
                await _pollRepository.RemoveVotesAsync(existingVotes);
            }

            // Add new votes
            var newVotes = submitVoteDto.OptionIds.Select(optionId => new PollVote
            {
                PollId = pollId,
                OptionId = optionId,
                UserId = userId,
                TimeStamp = DateTime.UtcNow
            }).ToList();
            await _pollRepository.AddVotesAsync(newVotes);

            await _pollRepository
                .SaveChangesAsync(); // Commit all changes (auto-close, remove old votes, add new votes)
            _logger.LogInformation("User {UserId} voted in poll {PollId}.", userId, pollId);

            // Fetch the updated poll with all includes for the response
            var updatedPollEntity = await _pollRepository.GetByIdAsync(pollId, userId);
            if (updatedPollEntity == null)
            {
                _logger.LogError("Failed to retrieve poll {PollId} after voting.", pollId);
                return StatusCode(500, "Chyba při načítání ankety po hlasování.");
            }

            var pollDtoForResponse = MapPollToDto(updatedPollEntity, userId);
            var pollDtoForBroadcast = MapPollToDto(updatedPollEntity, null);

            await _pollHubContext.Clients.All.SendAsync("ReceiveVoteUpdate",
                pollDtoForBroadcast); // Use the generic DTO for broadcast
            // The frontend client that voted will also receive this and can update its state.

            return Ok(pollDtoForResponse);
        }

        /// <summary>
        /// Closes an active poll.
        /// </summary>
        /// <param name="id">The ID of the poll to close.</param>
        [HttpPost("{id}/close")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce},{UserRoles.Uzivatel}")] // Allow creator
        public async Task<IActionResult> ClosePoll(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} attempting to close poll {PollId}.", currentUserId, id);

            var poll = await _pollRepository.GetByIdAsync(id);
            if (poll == null) return NotFound(new { message = "Anketa nebyla nalezena." });

            bool isAdmin = User.IsInRole(UserRoles.Administrator);
            bool isManager = User.IsInRole(UserRoles.Spravce);

            if (poll.CreatorUserId != currentUserId && !isAdmin && !isManager)
            {
                _logger.LogWarning("User {UserId} forbidden to close poll {PollId}.", currentUserId, id);
                return Forbid();
            }

            if (poll.IsClosed) return BadRequest(new { message = "Anketa je již uzavřena." });

            poll.IsClosed = true;
            poll.EndTime = DateTime.UtcNow;
            await _pollRepository.UpdateAsync(poll);
            await _pollRepository.SaveChangesAsync();
            _logger.LogInformation("Poll {PollId} closed by user {UserId}.", id, currentUserId);

            var updatedPollEntity = await _pollRepository.GetByIdAsync(id, currentUserId); // Re-fetch for DTO
            if (updatedPollEntity == null) return StatusCode(500, "Chyba při načítání ankety po uzavření.");

            var pollDtoForBroadcast = MapPollToDto(updatedPollEntity, null);
            await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", pollDtoForBroadcast);

            return Ok(MapPollToDto(updatedPollEntity, currentUserId));
        }

        /// <summary>
        /// Opens a closed poll.
        /// </summary>
        /// <param name="id">The ID of the poll to open.</param>
        /// <param name="openPollDto">DTO containing the new end time for the poll.</param>
        [HttpPost("{id}/open")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")] // Only Admin/Manager can reopen
        public async Task<IActionResult> OpenPoll(string id, [FromBody] OpenPollRequestDto openPollDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} attempting to open poll {PollId} with new EndTime {EndTime}.",
                currentUserId, id, openPollDto.NewEndTime);

            if (openPollDto.NewEndTime <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Nový čas ukončení musí být v budoucnosti." });
            }

            var poll = await _pollRepository.GetByIdAsync(id);
            if (poll == null) return NotFound(new { message = "Anketa nebyla nalezena." });

            if (!poll.IsClosed) return BadRequest(new { message = "Anketa není uzavřena." });

            poll.IsClosed = false;
            poll.EndTime = openPollDto.NewEndTime; // Set new end time
            await _pollRepository.UpdateAsync(poll);
            await _pollRepository.SaveChangesAsync();
            _logger.LogInformation("Poll {PollId} re-opened by user {UserId} until {NewEndTime}.", id, currentUserId,
                poll.EndTime);

            var updatedPollEntity = await _pollRepository.GetByIdAsync(id, currentUserId);
            if (updatedPollEntity == null) return StatusCode(500, "Chyba při načítání ankety po znovuotevření.");

            var pollDtoForBroadcast = MapPollToDto(updatedPollEntity, null);
            await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", pollDtoForBroadcast);

            return Ok(MapPollToDto(updatedPollEntity, currentUserId));
        }
    }


}