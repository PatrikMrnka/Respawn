// Controllers/PollsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RespawnApi.Application.DTOs.Polls;
using RespawnApi.Data;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
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

        public PollsController(RespawnDbContext context, UserManager<IdentityUser> userManager, ILogger<PollsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ... (GetPolls, GetPoll, CreatePoll - zůstávají stejné jako v předchozí verzi) ...
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
                bool isEffectivelyClosed = poll.IsClosed || poll.EndTime <= DateTime.UtcNow;

                var userVotesForThisPoll = poll.PollVotes
                                               .Where(pv => pv.UserId == userId)
                                               .Select(pv => pv.OptionId)
                                               .ToList();

                pollDtos.Add(new PollDto
                {
                    PollId = poll.PollId,
                    Question = poll.Question,
                    EndTime = poll.EndTime,
                    IsClosed = isEffectivelyClosed,
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
                    UserVotedOptionIds = userVotesForThisPoll,
                    TotalVotes = poll.PollVotes.Count
                });
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

            if (!poll.IsClosed && poll.EndTime <= DateTime.UtcNow)
            {
                poll.IsClosed = true;
                _context.Update(poll);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Anketa {PollId} byla automaticky uzavřena při načítání detailu.", poll.PollId);
            }

            var userVotesForThisPoll = poll.PollVotes
                                           .Where(pv => pv.UserId == userId)
                                           .Select(pv => pv.OptionId)
                                           .ToList();

            return new PollDto
            {
                PollId = poll.PollId,
                Question = poll.Question,
                EndTime = poll.EndTime,
                IsClosed = poll.IsClosed,
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
                UserVotedOptionIds = userVotesForThisPoll,
                TotalVotes = poll.PollVotes.Count
            };
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

            var creatorProfile = await _context.UserProfiles.FindAsync(userId);

            return CreatedAtAction(nameof(GetPoll), new { id = poll.PollId }, new PollDto
            {
                PollId = poll.PollId,
                Question = poll.Question,
                EndTime = poll.EndTime,
                IsClosed = poll.IsClosed,
                ImageUrl = poll.ImageUrl,
                CreatorUserId = poll.CreatorUserId,
                CreatorNickname = creatorProfile?.Nickname ?? "Neznámý",
                IsMultipleChoice = poll.IsMultipleChoice,
                Options = poll.PollOptions.Select(opt => new PollOptionDto { OptionId = opt.OptionId, Text = opt.Text, ImageUrl = opt.ImageUrl, VoteCount = 0 }).ToList(),
                TotalVotes = 0
            });
        }


        // PUT: api/polls/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePoll(string id, UpdatePollDto updatePollDto)
        {
            var poll = await _context.Polls
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes) // Načteme i hlasy pro kontrolu
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

            // Kontrola, zda se pokouší měnit možnosti u ankety, která již má hlasy
            bool optionsChanged = OptionsHaveChanged(poll.PollOptions, updatePollDto.Options);
            if (optionsChanged && poll.PollVotes.Any() && !canManageFully) // Běžný uživatel (tvůrce) nemůže měnit možnosti po prvním hlasu
            {
                return BadRequest(new { message = "Možnosti ankety nelze měnit poté, co již bylo hlasováno." });
            }
            // Administrátor/Správce může měnit možnosti, ale pokud jsou hlasy, měly by se resetovat (prozatím to neděláme, pouze povolíme změnu)
            // Pokud by se měly hlasy resetovat:
            if (optionsChanged && poll.PollVotes.Any() && canManageFully)
            {
                _logger.LogWarning("Administrátor/Správce {AdminUserId} mění možnosti ankety {PollId}, která má hlasy. Hlasy by měly být resetovány (není implementováno).", currentUserId, poll.PollId);
                // Zde by byla logika pro smazání existujících hlasů:
                // _context.PollVotes.RemoveRange(poll.PollVotes);
                // poll.PollVotes.Clear(); // Vyčistit kolekci v paměti
                // Prozatím pouze povolíme změnu bez resetu, což může vést k nekonzistenci, pokud nejsou hlasy smazány.
                // Ideální by bylo buď zakázat, nebo implementovat reset. Pro tuto fázi zakážeme i adminům.
                return BadRequest(new { message = "Změna možností u ankety s existujícími hlasy není aktuálně podporována ani pro administrátory (vyžadovalo by reset hlasů)." });
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
            // IsMultipleChoice se nemění, aby se nenarušila logika hlasování

            // Zpracování změn v PollOptions (pouze pokud anketa nemá hlasy nebo je to admin/manager a implementujeme reset)
            if (optionsChanged && !poll.PollVotes.Any()) // Povolíme změnu možností jen pokud nejsou hlasy
            {
                // 1. Možnosti k odstranění
                var optionsToRemove = poll.PollOptions
                    .Where(existingOpt => !updatePollDto.Options.Any(dtoOpt => dtoOpt.OptionId == existingOpt.OptionId))
                    .ToList();
                _context.PollOptions.RemoveRange(optionsToRemove);

                // 2. Možnosti k aktualizaci nebo přidání
                foreach (var dtoOption in updatePollDto.Options)
                {
                    if (!string.IsNullOrEmpty(dtoOption.OptionId)) // Aktualizace existující
                    {
                        var existingOption = poll.PollOptions.FirstOrDefault(opt => opt.OptionId == dtoOption.OptionId);
                        if (existingOption != null)
                        {
                            existingOption.Text = dtoOption.Text;
                            existingOption.ImageUrl = dtoOption.ImageUrl;
                            _context.PollOptions.Update(existingOption);
                        }
                        // Pokud OptionId existuje v DTO, ale ne v DB, je to chyba - ignorujeme nebo logujeme
                    }
                    else // Přidání nové možnosti
                    {
                        poll.PollOptions.Add(new PollOption
                        {
                            PollId = poll.PollId, // Důležité přiřadit k aktuální anketě
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

            // Načtení aktualizované ankety s novými/změněnými daty pro odpověď
            var updatedPollEntity = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .AsNoTracking() // Použijeme AsNoTracking, protože jsme právě uložili změny
                .FirstOrDefaultAsync(p => p.PollId == id);

            if (updatedPollEntity == null) return NotFound(new { message = "Aktualizovaná anketa nebyla nalezena po uložení." });


            var userIdForVoteCheck = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userVotesForThisPoll = updatedPollEntity.PollVotes
                                           .Where(pv => pv.UserId == userIdForVoteCheck)
                                           .Select(pv => pv.OptionId)
                                           .ToList();
            var updatedPollDto = new PollDto
            {
                PollId = updatedPollEntity.PollId,
                Question = updatedPollEntity.Question,
                EndTime = updatedPollEntity.EndTime,
                IsClosed = updatedPollEntity.IsClosed,
                ImageUrl = updatedPollEntity.ImageUrl,
                CreatorUserId = updatedPollEntity.CreatorUserId,
                CreatorNickname = updatedPollEntity.Creator?.Nickname ?? "Neznámý",
                IsMultipleChoice = updatedPollEntity.IsMultipleChoice,
                Options = updatedPollEntity.PollOptions.Select(opt => new PollOptionDto
                {
                    OptionId = opt.OptionId,
                    Text = opt.Text,
                    ImageUrl = opt.ImageUrl,
                    VoteCount = updatedPollEntity.PollVotes.Count(v => v.OptionId == opt.OptionId)
                }).ToList(),
                UserVotedOptionIds = userVotesForThisPoll,
                TotalVotes = updatedPollEntity.PollVotes.Count
            };

            return Ok(updatedPollDto);
        }

        // Pomocná metoda pro porovnání, zda se možnosti změnily
        private bool OptionsHaveChanged(ICollection<PollOption> existingOptions, List<UpdatePollOptionDto> newOptionsDto)
        {
            if (existingOptions.Count != newOptionsDto.Count) return true;

            var existingOptionsDict = existingOptions.ToDictionary(o => o.OptionId);

            foreach (var dtoOpt in newOptionsDto)
            {
                if (string.IsNullOrEmpty(dtoOpt.OptionId)) return true; // Nová možnost
                if (!existingOptionsDict.TryGetValue(dtoOpt.OptionId, out var existingOpt)) return true; // Možnost byla smazána a nahrazena jinou bez ID (což by nemělo nastat, pokud frontend posílá ID)

                if (existingOpt.Text != dtoOpt.Text || existingOpt.ImageUrl != dtoOpt.ImageUrl) return true; // Změna textu nebo obrázku
            }
            return false;
        }


        // ... (DeletePoll, SubmitVote - zůstávají stejné jako v předchozí verzi) ...
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

            _context.Polls.Remove(poll);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Anketa {PollId} byla smazána uživatelem {UserId}", id, currentUserId);

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

            if (!poll.IsMultipleChoice && existingVotes.Any() && (existingVotes.Count > 1 || existingVotes.First().OptionId != submitVoteDto.OptionIds.First()))
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

            var updatedPollAfterVote = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.PollOptions)
                .Include(p => p.PollVotes)
                .FirstOrDefaultAsync(p => p.PollId == pollId);

            if (updatedPollAfterVote == null) return NotFound();

            var userVotesForThisPoll = updatedPollAfterVote.PollVotes.Where(pv => pv.UserId == userId).Select(pv => pv.OptionId).ToList();

            return Ok(new PollDto
            {
                PollId = updatedPollAfterVote.PollId,
                Question = updatedPollAfterVote.Question,
                EndTime = updatedPollAfterVote.EndTime,
                IsClosed = updatedPollAfterVote.IsClosed,
                ImageUrl = updatedPollAfterVote.ImageUrl,
                CreatorUserId = updatedPollAfterVote.CreatorUserId,
                CreatorNickname = updatedPollAfterVote.Creator?.Nickname ?? "Neznámý",
                IsMultipleChoice = updatedPollAfterVote.IsMultipleChoice,
                Options = updatedPollAfterVote.PollOptions.Select(opt => new PollOptionDto
                {
                    OptionId = opt.OptionId,
                    Text = opt.Text,
                    ImageUrl = opt.ImageUrl,
                    VoteCount = updatedPollAfterVote.PollVotes.Count(v => v.OptionId == opt.OptionId)
                }).ToList(),
                UserVotedOptionIds = userVotesForThisPoll,
                TotalVotes = updatedPollAfterVote.PollVotes.Count
            });
        }
    }
}
