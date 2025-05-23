// Controllers/PollsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Application.DTOs.Polls;
using RespawnApi.Data;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs;
using System.Security.Claims;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PollsController : ControllerBase
    {
        private readonly RespawnDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PollsController> _logger;
        private readonly IHubContext<PollHub> _pollHubContext;

        public PollsController(
            RespawnDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<PollsController> logger,
            IHubContext<PollHub> pollHubContext)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _pollHubContext = pollHubContext;
        }

        private async Task<PollDto> MapPollToDto(Poll poll, string? currentUserId)
        {
            // Zajistíme, že navigační vlastnosti jsou načteny, pokud ještě nejsou
            if (poll.Creator == null && !string.IsNullOrEmpty(poll.CreatorUserId))
            {
                poll.Creator = await _context.UserProfiles.FindAsync(poll.CreatorUserId);
            }
            if (!poll.PollOptions.Any() && _context.Entry(poll).Collection(p => p.PollOptions).IsLoaded == false)
            {
                await _context.Entry(poll).Collection(p => p.PollOptions).LoadAsync();
            }
            if (!poll.PollVotes.Any() && _context.Entry(poll).Collection(p => p.PollVotes).IsLoaded == false)
            {
                await _context.Entry(poll).Collection(p => p.PollVotes).LoadAsync();
            }


            var userVotesForThisPoll = new List<string>();
            if (!string.IsNullOrEmpty(currentUserId)) // Pouze pokud máme ID konkrétního uživatele
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
                IsClosed = poll.IsClosed || poll.EndTime <= DateTime.UtcNow,
                ImageUrl = poll.ImageUrl,
                CreatorUserId = poll.CreatorUserId,
                CreatorNickname = poll.Creator?.Nickname ?? "Neznámý",
                IsMultipleChoice = poll.IsMultipleChoice,
                Options = poll.PollOptions.Select(opt => new PollOptionDto
                {
                    OptionId = opt.OptionId,
                    Text = opt.Text,
                    ImageUrl = opt.ImageUrl,
                    VoteCount = poll.PollVotes.Count(v => v.OptionId == opt.OptionId)
                }).ToList(),
                UserVotedOptionIds = userVotesForThisPoll, // Bude prázdné, pokud currentUserId je null
                TotalVotes = poll.PollVotes.Count
            };
        }

        // GET: api/polls
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PollDto>>> GetPolls()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pollsFromDb = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .OrderByDescending(p => p.EndTime)
                .ToListAsync();

            var pollDtos = new List<PollDto>();
            foreach (var poll in pollsFromDb)
            {
                pollDtos.Add(await MapPollToDto(poll, userId));
            }
            return Ok(pollDtos.OrderBy(p => p.IsClosed).ThenByDescending(p => p.EndTime).ToList());
        }

        // GET: api/polls/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PollDto>> GetPoll(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var poll = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .FirstOrDefaultAsync(p => p.PollId == id);

            if (poll == null)
            {
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            bool stateChanged = false;
            if (!poll.IsClosed && poll.EndTime <= DateTime.UtcNow)
            {
                poll.IsClosed = true;
                _context.Update(poll);
                await _context.SaveChangesAsync();
                stateChanged = true;
                _logger.LogInformation("Anketa {PollId} byla automaticky uzavřena při načítání detailu.", poll.PollId);
            }

            var pollDtoToReturn = await MapPollToDto(poll, userId); // Pro HTTP odpověď

            if (stateChanged) // Pokud se stav změnil, pošleme update všem
            {
                var broadcastDto = await MapPollToDto(poll, null); // Generic DTO pro broadcast
                await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", broadcastDto);
            }

            return Ok(pollDtoToReturn);
        }

        // POST: api/polls
        [HttpPost]
        public async Task<ActionResult<PollDto>> CreatePoll(CreatePollDto createPollDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Pro vytvoření ankety musíte být přihlášeni." });
            }
            if (createPollDto.EndTime <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(createPollDto.EndTime), "Čas ukončení ankety musí být v budoucnosti.");
            }
            if (createPollDto.Options.Count < 2)
            {
                ModelState.AddModelError(nameof(createPollDto.Options), "Anketa musí mít alespoň dvě možnosti.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var poll = new Poll
            {
                Question = createPollDto.Question,
                EndTime = createPollDto.EndTime,
                ImageUrl = createPollDto.ImageUrl,
                CreatorUserId = userId,
                IsMultipleChoice = createPollDto.IsMultipleChoice,
                PollOptions = createPollDto.Options.Select(optDto => new PollOption
                {
                    Text = optDto.Text,
                    ImageUrl = optDto.ImageUrl
                }).ToList()
            };

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Uživatel {UserId} vytvořil anketu {PollId}", userId, poll.PollId);

            var createdPollWithIncludes = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .AsNoTracking() // Důležité po SaveChanges, pokud chceme ihned mapovat
                .FirstAsync(p => p.PollId == poll.PollId);

            var pollDtoForResponse = await MapPollToDto(createdPollWithIncludes, userId); // Pro HTTP odpověď tvůrci
            var pollDtoForBroadcast = await MapPollToDto(createdPollWithIncludes, null); // Obecné pro ostatní

            await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", pollDtoForBroadcast);

            return CreatedAtAction(nameof(GetPoll), new { id = poll.PollId }, pollDtoForResponse);
        }

        // PUT: api/polls/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePoll(string id, UpdatePollDto updatePollDto)
        {
            var poll = await _context.Polls
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.PollId == id);

            if (poll == null)
            {
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole(UserRoles.Administrator);
            bool isManager = User.IsInRole(UserRoles.Spravce);
            bool canManageFully = isAdmin || isManager;
            bool isCreator = poll.CreatorUserId == currentUserId;

            if (!isCreator && !canManageFully)
            {
                return Forbid();
            }

            bool optionsActuallyChanged = OptionsHaveChanged(poll.PollOptions, updatePollDto.Options);
            if (optionsActuallyChanged && poll.PollVotes.Any()) // Úprava možností u ankety s hlasy je zakázána
            {
                return BadRequest(new { message = "Změna možností u ankety s existujícími hlasy není povolena." });
            }

            if (poll.IsClosed && updatePollDto.EndTime > DateTime.UtcNow && !canManageFully)
            {
                return BadRequest(new { message = "Nelze znovu otevřít již uzavřenou anketu změnou času." });
            }

            bool wasPreviouslyOpen = !poll.IsClosed && poll.EndTime > DateTime.UtcNow;
            bool isNowClosing = updatePollDto.EndTime <= DateTime.UtcNow;

            if (wasPreviouslyOpen && isNowClosing)
            {
                poll.IsClosed = true;
            }
            else if (updatePollDto.EndTime > DateTime.UtcNow)
            {
                if (canManageFully) poll.IsClosed = false;
                else if (poll.IsClosed) return BadRequest(new { message = "Běžný uživatel nemůže znovu otevřít uzavřenou anketu." });
            }

            poll.Question = updatePollDto.Question;
            poll.EndTime = updatePollDto.EndTime;
            poll.ImageUrl = updatePollDto.ImageUrl;

            if (optionsActuallyChanged && !poll.PollVotes.Any())
            {
                var optionsToRemove = poll.PollOptions
                    .Where(existingOpt => !updatePollDto.Options.Any(dtoOpt => dtoOpt.OptionId == existingOpt.OptionId))
                    .ToList();
                if (optionsToRemove.Any()) _context.PollOptions.RemoveRange(optionsToRemove);

                foreach (var dtoOption in updatePollDto.Options)
                {
                    if (!string.IsNullOrEmpty(dtoOption.OptionId))
                    {
                        var existingOption = poll.PollOptions.FirstOrDefault(opt => opt.OptionId == dtoOption.OptionId);
                        if (existingOption != null)
                        {
                            existingOption.Text = dtoOption.Text;
                            existingOption.ImageUrl = dtoOption.ImageUrl;
                            _context.PollOptions.Update(existingOption);
                        }
                    }
                    else
                    {
                        poll.PollOptions.Add(new PollOption
                        {
                            PollId = poll.PollId,
                            Text = dtoOption.Text,
                            ImageUrl = dtoOption.ImageUrl
                        });
                    }
                }
            }

            try
            {
                _context.Update(poll);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Anketa {PollId} byla aktualizována uživatelem {UserId}", id, currentUserId);

                // Načtení finální entity pro DTO po všech změnách
                var finalPollEntity = await _context.Polls
                    .Include(p => p.Creator).Include(p => p.PollOptions).Include(p => p.PollVotes)
                    .AsNoTracking().FirstOrDefaultAsync(p => p.PollId == id);

                if (finalPollEntity == null) return NotFound(new { message = "Anketa nebyla nalezena po uložení." });


                var pollDtoForResponse = await MapPollToDto(finalPollEntity, currentUserId);
                var pollDtoForBroadcast = await MapPollToDto(finalPollEntity, null);

                await _pollHubContext.Clients.All.SendAsync("ReceivePollUpdate", pollDtoForBroadcast);
                return Ok(pollDtoForResponse);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Polls.Any(e => e.PollId == id)) return NotFound(new { message = "Anketa mezitím byla smazána." });
                else
                {
                    _logger.LogError("Chyba souběhu při aktualizaci ankety {PollId}", id);
                    return Conflict(new { message = "Došlo ke konfliktu při úpravě ankety, zkuste to prosím znovu." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Neočekávaná chyba při aktualizaci ankety {PollId}", id);
                return StatusCode(500, new { message = "Interní chyba serveru při aktualizaci ankety." });
            }
        }

        private bool OptionsHaveChanged(ICollection<PollOption> existingOptions, List<UpdatePollOptionDto> newOptionsDto)
        {
            if (existingOptions.Count != newOptionsDto.Count) return true;
            var existingOptionsDict = existingOptions.ToDictionary(o => o.OptionId);
            foreach (var dtoOpt in newOptionsDto)
            {
                if (string.IsNullOrEmpty(dtoOpt.OptionId)) return true;
                if (!existingOptionsDict.TryGetValue(dtoOpt.OptionId, out var existingOpt)) return true;
                if (existingOpt.Text != dtoOpt.Text || existingOpt.ImageUrl != dtoOpt.ImageUrl) return true;
            }
            return false;
        }

        // DELETE: api/polls/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoll(string id)
        {
            var poll = await _context.Polls.FindAsync(id);
            if (poll == null)
            {
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole(UserRoles.Administrator);
            bool isManager = User.IsInRole(UserRoles.Spravce);

            if (poll.CreatorUserId != currentUserId && !isAdmin && !isManager)
            {
                return Forbid();
            }

            string pollIdToDelete = poll.PollId;
            _context.Polls.Remove(poll);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Anketa {PollId} byla smazána uživatelem {UserId}", id, currentUserId);

            await _pollHubContext.Clients.All.SendAsync("ReceivePollDelete", pollIdToDelete);

            return NoContent();
        }

        // POST: api/polls/{pollId}/vote
        [HttpPost("{pollId}/vote")]
        public async Task<ActionResult<PollDto>> SubmitVote(string pollId, SubmitVoteDto submitVoteDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Pro hlasování musíte být přihlášeni." });
            }

            var poll = await _context.Polls
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.PollId == pollId);

            if (poll == null)
            {
                return NotFound(new { message = "Anketa nebyla nalezena." });
            }

            bool stateChangedDueToEndTime = false;
            if (!poll.IsClosed && poll.EndTime <= DateTime.UtcNow)
            {
                poll.IsClosed = true;
                _context.Update(poll);
                stateChangedDueToEndTime = true;
            }

            if (poll.IsClosed)
            {
                if (stateChangedDueToEndTime) await _context.SaveChangesAsync();
                _logger.LogWarning("Pokus o hlasování v uzavřené anketě {PollId} uživatelem {UserId}", pollId, userId);
                return BadRequest(new { message = "Tato anketa je již uzavřena." });
            }

            foreach (var optionId in submitVoteDto.OptionIds)
            {
                if (!poll.PollOptions.Any(opt => opt.OptionId == optionId))
                {
                    return BadRequest(new { message = $"Neplatná možnost hlasování: {optionId}" });
                }
            }

            var existingVotes = await _context.PollVotes
                                    .Where(pv => pv.PollId == pollId && pv.UserId == userId)
                                    .ToListAsync();

            if (!poll.IsMultipleChoice && existingVotes.Any() && (existingVotes.Count > 1 || (submitVoteDto.OptionIds.Any() && existingVotes.First().OptionId != submitVoteDto.OptionIds.First())))
            {
                return BadRequest(new { message = "V této anketě můžete hlasovat pouze jednou pro jednu možnost." });
            }
            if (!poll.IsMultipleChoice && submitVoteDto.OptionIds.Count > 1)
            {
                return BadRequest(new { message = "V této anketě můžete vybrat pouze jednu možnost." });
            }

            _context.PollVotes.RemoveRange(existingVotes);

            foreach (var optionId in submitVoteDto.OptionIds)
            {
                _context.PollVotes.Add(new PollVote
                {
                    PollId = pollId,
                    OptionId = optionId,
                    UserId = userId,
                    TimeStamp = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Uživatel {UserId} hlasoval v anketě {PollId} pro možnosti: {OptionIds}", userId, pollId, string.Join(",", submitVoteDto.OptionIds));

            // Načteme znovu anketu s aktualizovanými počty hlasů a stavem userVotedOptionIds
            // Je důležité znovu načíst, aby se promítly změny v PollVotes
            var updatedPollAfterVote = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .AsNoTracking() // Pro čtení po uložení
                .FirstOrDefaultAsync(p => p.PollId == pollId);

            if (updatedPollAfterVote == null) return NotFound();

            var pollDtoForResponse = await MapPollToDto(updatedPollAfterVote, userId); // Pro HTTP odpověď hlasujícímu
            var pollDtoForBroadcast = await MapPollToDto(updatedPollAfterVote, null); // Obecné pro ostatní

            await _pollHubContext.Clients.All.SendAsync("ReceiveVoteUpdate", pollDtoForBroadcast);

            return Ok(pollDtoForResponse);
        }
    }
}
