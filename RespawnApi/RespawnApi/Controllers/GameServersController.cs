// Controllers/GameServersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs;
using RespawnApi.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
    public class GameServersController : ControllerBase
    {
        private readonly IDockerService _dockerService;
        private readonly IHubContext<GameServerHub> _gameServerHubContext;
        private readonly ILogger<GameServersController> _logger;
        private readonly IGameServerRepository _gameServerRepository;
        private readonly IServiceScopeFactory _scopeFactory;

        public GameServersController(
            IDockerService dockerService,
            IHubContext<GameServerHub> gameServerHubContext,
            ILogger<GameServersController> logger,
            IGameServerRepository gameServerRepository,
            IServiceScopeFactory scopeFactory)
        {
            _dockerService = dockerService;
            _gameServerHubContext = gameServerHubContext;
            _logger = logger;
            _gameServerRepository = gameServerRepository;
            _scopeFactory = scopeFactory;
        }

        private string GetGameImageTag(GameType gameType) => gameType switch
        {
            GameType.CounterStrike => "cs",
            GameType.TeamFortress2 => "tf2",
            GameType.GarrysMod => "gmod",
            _ => throw new ArgumentOutOfRangeException(nameof(gameType), $"Nepodporovaný typ hry: {gameType}")
        };
        private string GetGameIdentifier(GameType gameType) => gameType switch
        {
            GameType.CounterStrike => "csserver",
            GameType.TeamFortress2 => "tf2server",
            GameType.GarrysMod => "gmodserver",
            _ => $"unknownserver-{Guid.NewGuid().ToString().Substring(0, 4)}"
        };

        private GameServerDto MapToDto(GameServer s) => new GameServerDto
        {
            GameServerId = s.GameServerId,
            Name = s.Name,
            GameType = s.GameType,
            Status = s.Status,
            LgsmServerStatus = s.LgsmServerStatus, // Přidáno LgsmServerStatus
            IpAddress = s.IpAddress,
            Port = s.Port,
            ContainerId = s.ContainerId,
            CreatedAt = s.CreatedAt,
            StatusDetails = s.StatusDetails
        };

        // GET: api/gameservers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameServerDto>>> GetGameServers()
        {
            var servers = await _gameServerRepository.GetAllAsync();
            return Ok(servers.Select(MapToDto));
        }

        // POST: api/gameservers
        [HttpPost]
        public async Task<ActionResult<GameServerDto>> CreateGameServer(CreateGameServerDto createDto)
        {
            _logger.LogInformation("Požadavek na vytvoření herního serveru: {ServerName}, Typ: {GameType}", createDto.Name, createDto.GameType);

            var gameServer = new GameServer
            {
                GameServerId = Guid.NewGuid(),
                Name = createDto.Name,
                GameType = createDto.GameType,
                Status = ServerStatus.PendingCreation,
                LgsmServerStatus = "PENDING", // Počáteční LGSM stav
                CreatedAt = DateTime.UtcNow,
            };

            await _gameServerRepository.AddAsync(gameServer);
            _logger.LogInformation("Herní server {GameServerId} uložen do DB se stavem PendingCreation.", gameServer.GameServerId);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(gameServer));


            _ = Task.Run(async () => {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                    var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<GameServersController>>();
                    GameServer? serverToUpdate = null;
                    try
                    {
                        scopedLogger.LogInformation("Zahajuji vytváření Docker kontejneru pro server {GameServerId} na pozadí.", gameServer.GameServerId);
                        serverToUpdate = await scopedRepo.GetByIdAsync(gameServer.GameServerId);
                        if (serverToUpdate == null) { scopedLogger.LogError("Server {GameServerId} nenalezen pro update.", gameServer.GameServerId); return; }

                        serverToUpdate.Status = ServerStatus.Installing;
                        serverToUpdate.LgsmServerStatus = "INSTALLING";
                        await scopedRepo.UpdateAsync(serverToUpdate);
                        await scopedHub.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto { GameServerId = serverToUpdate.GameServerId, NewOverallStatus = serverToUpdate.Status, NewLgsmServerStatus = serverToUpdate.LgsmServerStatus, StatusDetails = "Zahájení instalace..." });

                        var imageTag = GetGameImageTag(createDto.GameType);
                        var gameIdentifier = GetGameIdentifier(createDto.GameType);
                        var (containerId, errorMessage) = await _dockerService.CreateContainerAsync(serverToUpdate, imageTag, gameIdentifier, createDto.AdditionalGsParams);

                        serverToUpdate = await scopedRepo.GetByIdAsync(gameServer.GameServerId); // Znovu načteme
                        if (serverToUpdate == null) { scopedLogger.LogError("Server {GameServerId} nenalezen po pokusu o vytvoření kontejneru.", gameServer.GameServerId); return; }

                        if (containerId != null)
                        {
                            serverToUpdate.ContainerId = containerId;
                            serverToUpdate.IpAddress = "localhost";
                            serverToUpdate.Status = ServerStatus.Starting; // Kontejner běží, LGSM se bude instalovat/startovat
                            serverToUpdate.LgsmServerStatus = "STARTING/INSTALLING"; // Indikace, že LGSM proces běží
                            serverToUpdate.StatusDetails = "Kontejner vytvořen, čeká se na dokončení LGSM instalace/startu.";
                            scopedLogger.LogInformation("Docker kontejner {ContainerId} pro server {GameServerId} vytvořen. Stav: {Status}, LGSM: {LgsmStatus}", containerId, serverToUpdate.GameServerId, serverToUpdate.Status, serverToUpdate.LgsmServerStatus);
                        }
                        else
                        {
                            serverToUpdate.Status = ServerStatus.Error;
                            serverToUpdate.LgsmServerStatus = "ERROR";
                            serverToUpdate.StatusDetails = errorMessage ?? "Neznámá chyba při vytváření kontejneru.";
                            scopedLogger.LogError("Chyba při vytváření Docker kontejneru pro server {GameServerId}: {ErrorMessage}", serverToUpdate.GameServerId, serverToUpdate.StatusDetails);
                        }
                        await scopedRepo.UpdateAsync(serverToUpdate);
                        await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(serverToUpdate));
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex, "Výjimka při asynchronním vytváření kontejneru pro server {ServerId}.", gameServer.GameServerId);
                        var serverToUpdateOnError = await scopedRepo.GetByIdAsync(gameServer.GameServerId);
                        if (serverToUpdateOnError != null)
                        {
                            serverToUpdateOnError.Status = ServerStatus.Error;
                            serverToUpdateOnError.LgsmServerStatus = "ERROR";
                            serverToUpdateOnError.StatusDetails = $"Výjimka při vytváření: {ex.Message}";
                            try
                            {
                                await scopedRepo.UpdateAsync(serverToUpdateOnError);
                                await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(serverToUpdateOnError));
                            }
                            catch (Exception innerEx) { scopedLogger.LogError(innerEx, "Výjimka při ukládání chybového stavu serveru {GameServerId}.", gameServer.GameServerId); }
                        }
                    }
                }
            });
            return CreatedAtAction(nameof(GetGameServer), new { id = gameServer.GameServerId }, MapToDto(gameServer));
        }

        // GET: api/gameservers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GameServerDto>> GetGameServer(Guid id)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null) return NotFound();
            return Ok(MapToDto(server));
        }

        // GET: api/gameservers/{id}/logs
        [HttpGet("{id}/logs")]
        public async Task<ActionResult<string>> GetGameServerLogs(Guid id, [FromQuery] uint tail = 200)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId))
            {
                return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            }
            _logger.LogInformation("Požadavek na logy pro server {ServerId}, kontejner {ContainerId}, tail {Tail}", id, server.ContainerId, tail);
            var logs = await _dockerService.GetContainerLogsAsync(server.ContainerId, tail);
            return Ok(logs);
        }

        // POST: api/gameservers/{id}/start
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartGameServer(Guid id)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            if (server.Status == ServerStatus.Online || server.Status == ServerStatus.Starting || server.Status == ServerStatus.Installing)
                return BadRequest(new { message = "Server již běží nebo se spouští/instaluje." });

            var success = await _dockerService.StartContainerAsync(server.ContainerId);
            if (success)
            {
                server.Status = ServerStatus.Starting;
                server.LgsmServerStatus = "STARTING"; // Předpokládaný počáteční stav po startu kontejneru
                await _gameServerRepository.UpdateAsync(server);
                await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
                return Ok(new { message = "Příkaz ke spuštění serveru odeslán." });
            }
            server.Status = ServerStatus.Error;
            server.LgsmServerStatus = "ERROR";
            server.StatusDetails = "Nepodařilo se spustit Docker kontejner.";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
            return StatusCode(500, new { message = "Nepodařilo se spustit server." });
        }

        // POST: api/gameservers/{id}/stop
        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopGameServer(Guid id)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            if (server.Status == ServerStatus.Offline || server.Status == ServerStatus.Stopping)
                return BadRequest(new { message = "Server již neběží nebo se zastavuje." });

            server.Status = ServerStatus.Stopping;
            server.LgsmServerStatus = "STOPPING";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));

            _ = Task.Run(async () => {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                    var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<GameServersController>>();
                    GameServer? finalServerState = null;
                    try
                    {
                        var success = await _dockerService.StopContainerAsync(server.ContainerId);
                        finalServerState = await scopedRepo.GetByIdAsync(id);
                        if (finalServerState != null)
                        {
                            finalServerState.Status = success ? ServerStatus.Offline : ServerStatus.Error;
                            finalServerState.LgsmServerStatus = success ? "OFFLINE" : "ERROR";
                            finalServerState.StatusDetails = success ? "Server úspěšně zastaven." : "Chyba při zastavování serveru.";
                            await scopedRepo.UpdateAsync(finalServerState);
                            await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(finalServerState));
                        }
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex, "Výjimka při asynchronním zastavování kontejneru pro server {ServerId}.", id);
                        if (finalServerState == null) finalServerState = await scopedRepo.GetByIdAsync(id);
                        if (finalServerState != null)
                        {
                            finalServerState.Status = ServerStatus.Error; finalServerState.LgsmServerStatus = "ERROR";
                            finalServerState.StatusDetails = $"Výjimka při zastavování: {ex.Message}";
                            await scopedRepo.UpdateAsync(finalServerState);
                            await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(finalServerState));
                        }
                    }
                }
            });
            return Ok(new { message = "Příkaz k zastavení serveru odeslán." });
        }

        // DELETE: api/gameservers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGameServer(Guid id)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null) return NotFound();
            if (!string.IsNullOrEmpty(server.ContainerId))
            {
                if (server.Status != ServerStatus.Offline && server.Status != ServerStatus.Error)
                {
                    await _dockerService.StopContainerAsync(server.ContainerId);
                }
                await _dockerService.RemoveContainerAsync(server.ContainerId, true);
            }
            await _gameServerRepository.DeleteAsync(id);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerRemoval", id);
            return NoContent();
        }
    }
}
