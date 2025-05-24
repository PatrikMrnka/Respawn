// File: haha/RespawnApi/RespawnApi/Controllers/GameServersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
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


namespace RespawnApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Class-level authorization removed, will be applied per-method
    public class GameServersController : ControllerBase
    {
        private readonly IDockerService _dockerService;
        private readonly IHubContext<GameServerHub> _gameServerHubContext;
        private readonly ILogger<GameServersController> _logger;
        private readonly IGameServerRepository _gameServerRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IGameServerQueryService _gameServerQueryService;

        public GameServersController(
            IDockerService dockerService,
            IHubContext<GameServerHub> gameServerHubContext,
            ILogger<GameServersController> logger,
            IGameServerRepository gameServerRepository,
            IServiceScopeFactory scopeFactory,
            IGameServerQueryService gameServerQueryService)
        {
            _dockerService = dockerService;
            _gameServerHubContext = gameServerHubContext;
            _logger = logger;
            _gameServerRepository = gameServerRepository;
            _scopeFactory = scopeFactory;
            _gameServerQueryService = gameServerQueryService;
        }

        private GameServerDto MapToDto(GameServer s)
        {
            if (s == null)
            {
                _logger.LogWarning("MapToDto received a null GameServer object.");
                return new GameServerDto
                {
                    GameServerId = Guid.Empty,
                    Name = "[CHYBA: Server data jsou null]",
                    Status = ServerStatus.Unknown,
                    CreatedAt = DateTime.MinValue
                };
            }
            return new GameServerDto
            {
                GameServerId = s.GameServerId,
                Name = s.Name,
                GameType = s.GameType,
                Status = s.Status,
                IpAddress = s.IpAddress,
                Port = s.Port,
                ContainerId = s.ContainerId,
                CreatedAt = s.CreatedAt,
                StatusDetails = s.StatusDetails
            };
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

        // GET: api/gameservers
        [HttpGet]
        [Authorize] // Accessible to all authenticated users
        public async Task<ActionResult<IEnumerable<GameServerDto>>> GetGameServers()
        {
            _logger.LogInformation("Endpoint GetGameServers byl zavolán.");
            try
            {
                var servers = await _gameServerRepository.GetAllAsync();
                var dtos = new List<GameServerDto>();
                if (servers != null && servers.Any())
                {
                    dtos = servers.Where(s => s != null).Select(MapToDto).ToList();
                    _logger.LogInformation("Načteno a zmapováno {Count} serverů.", dtos.Count);
                }
                else
                {
                    _logger.LogInformation("Nebyly nalezeny žádné servery nebo GetAllAsync vrátil prázdný seznam/null.");
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kritická chyba v GetGameServers při dotazu nebo mapování.");
                return StatusCode(500, new { message = "Interní chyba serveru při načítání herních serverů.", details = ex.Message });
            }
        }

        // POST: api/gameservers
        [HttpPost]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")] // Restricted
        public async Task<ActionResult<GameServerDto>> CreateGameServer(CreateGameServerDto createDto)
        {
            _logger.LogInformation("Požadavek na vytvoření herního serveru: {ServerName}, Typ: {GameType}", createDto.Name, createDto.GameType);
            var gameServer = new GameServer
            {
                GameServerId = Guid.NewGuid(),
                Name = createDto.Name,
                GameType = createDto.GameType,
                Status = ServerStatus.PendingCreation,
                CreatedAt = DateTime.UtcNow,
                StatusDetails = "Čeká na vytvoření kontejneru..."
            };

            if (createDto.GameType == GameType.CounterStrike)
            {
                gameServer.Port = 27015;
                _logger.LogInformation("Nastavuji výchozí port 27015 pro Counter-Strike server {ServerName}.", createDto.Name);
            }

            try
            {
                await _gameServerRepository.AddAsync(gameServer);
                _logger.LogInformation("Herní server {GameServerId} uložen do DB se stavem PendingCreation. Port: {Port}", gameServer.GameServerId, gameServer.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při ukládání počátečního stavu GameServer {GameServerId} do DB.", gameServer.GameServerId);
                return StatusCode(500, new { message = "Chyba při ukládání serveru do databáze.", details = ex.Message });
            }

            try
            {
                await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(gameServer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při odesílání SignalR ReceiveGameServerUpdate po AddAsync pro {GameServerId}.", gameServer.GameServerId);
            }

            _ = Task.Run(async () => {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                    var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<GameServersController>>();
                    var scopedDockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();
                    GameServer? serverToUpdate = null;
                    Guid serverIdForLogging = gameServer.GameServerId;
                    try
                    {
                        scopedLogger.LogInformation("Zahajuji vytváření Docker kontejneru pro server {GameServerId} na pozadí.", serverIdForLogging);
                        serverToUpdate = await scopedRepo.GetByIdAsync(serverIdForLogging);
                        if (serverToUpdate == null) { scopedLogger.LogError("Server {GameServerId} nenalezen pro update.", serverIdForLogging); return; }

                        serverToUpdate.StatusDetails = "Vytváření a spouštění Docker kontejneru...";
                        await scopedRepo.UpdateAsync(serverToUpdate);
                        await scopedHub.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto { GameServerId = serverToUpdate.GameServerId, NewOverallStatus = serverToUpdate.Status, StatusDetails = serverToUpdate.StatusDetails });

                        var imageTag = GetGameImageTag(createDto.GameType);
                        var gameIdentifier = GetGameIdentifier(createDto.GameType);
                        var (containerId, errorMessage) = await scopedDockerService.CreateContainerAsync(serverToUpdate, imageTag, gameIdentifier, createDto.AdditionalGsParams);

                        serverToUpdate = await scopedRepo.GetByIdAsync(serverIdForLogging);
                        if (serverToUpdate == null) { scopedLogger.LogError("Server {GameServerId} nenalezen po pokusu o vytvoření kontejneru.", serverIdForLogging); return; }

                        if (containerId != null)
                        {
                            serverToUpdate.ContainerId = containerId;
                            serverToUpdate.IpAddress = "localhost";
                            serverToUpdate.Status = ServerStatus.Starting;
                            serverToUpdate.StatusDetails = "Kontejner vytvořen, server se spouští/instaluje.";
                            scopedLogger.LogInformation("Docker kontejner {ContainerId} pro server {GameServerId} vytvořen. Stav: {Status}, IP: {IP}, Port: {Port}", containerId, serverToUpdate.GameServerId, serverToUpdate.Status, serverToUpdate.IpAddress, serverToUpdate.Port);
                        }
                        else
                        {
                            serverToUpdate.Status = ServerStatus.Error;
                            serverToUpdate.StatusDetails = errorMessage ?? "Neznámá chyba při vytváření kontejneru.";
                            scopedLogger.LogError("Chyba při vytváření Docker kontejneru pro server {GameServerId}: {ErrorMessage}", serverToUpdate.GameServerId, serverToUpdate.StatusDetails);
                        }
                        await scopedRepo.UpdateAsync(serverToUpdate);
                        await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(serverToUpdate));
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex, "Výjimka v Task.Run při vytváření kontejneru pro server {ServerId}.", serverIdForLogging);
                        var srvErr = await scopedRepo.GetByIdAsync(serverIdForLogging);
                        if (srvErr != null)
                        {
                            srvErr.Status = ServerStatus.Error;
                            srvErr.StatusDetails = $"Chyba na pozadí: {ex.GetType().Name}";
                            try
                            {
                                await scopedRepo.UpdateAsync(srvErr);
                                await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(srvErr));
                            }
                            catch (Exception iex)
                            {
                                scopedLogger.LogError(iex, "Výjimka při ukládání finálního chybového stavu serveru {GameServerId}.", serverIdForLogging);
                            }
                        }
                        else
                        {
                            scopedLogger.LogError("Server {GameServerId} nenalezen ani pro uložení chybového stavu po výjimce v Task.Run.", serverIdForLogging);
                        }
                    }
                }
            });

            return CreatedAtAction(nameof(GetGameServer), new { id = gameServer.GameServerId }, MapToDto(gameServer));
        }

        [HttpGet("{id}")]
        [Authorize] // Accessible to all authenticated users
        public async Task<ActionResult<GameServerDto>> GetGameServer(Guid id)
        {
            _logger.LogInformation("Požadavek na GetGameServer pro ID: {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null)
            {
                _logger.LogWarning("Server s ID {ServerId} nenalezen.", id);
                return NotFound(new { message = $"Server s ID {id} nebyl nalezen." });
            }
            _logger.LogInformation("Server {ServerId} nalezen, vracím DTO.", id);
            return Ok(MapToDto(server));
        }

        // GET: api/gameservers/{id}/details
        [HttpGet("{id}/details")]
        [Authorize] // Accessible to all authenticated users
        [ProducesResponseType(typeof(GameServerDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GameServerDetailDto>> GetGameServerDetails(Guid id)
        {
            _logger.LogInformation("Požadavek na GetGameServerDetails pro ID: {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null)
            {
                _logger.LogWarning("Server s ID {ServerId} nenalezen pro detaily.", id);
                return NotFound(new { message = $"Server s ID {id} nebyl nalezen." });
            }

            var basicDto = MapToDto(server);

            if (server.Status == ServerStatus.Online && !string.IsNullOrEmpty(server.IpAddress) && server.Port.HasValue)
            {
                int queryPort = server.Port.Value;
                GameServerDetailDto? detailedInfo = null;
                string a2sErrorMsg = "Neznámá chyba při A2S dotazu.";

                try
                {
                    detailedInfo = await _gameServerQueryService.GetServerDetailsA2SAsync(server.IpAddress, queryPort, basicDto);
                    if (detailedInfo != null)
                    {
                        _logger.LogInformation("Úspěšně získány A2S detaily pro server {ServerId}.", id);
                        return Ok(detailedInfo);
                    }
                    else
                    {
                        a2sErrorMsg = "A2S dotaz selhal nebo server neodpověděl (QueryMaster vrátil null).";
                        _logger.LogWarning("Nepodařilo se získat A2S detaily pro server {ServerId} (QueryMaster vrátil null). Server zůstává označen jako Online, ale detaily chybí.", id);
                    }
                }
                catch (Exception ex)
                {
                    a2sErrorMsg = $"Výjimka při A2S dotazu: {ex.Message}";
                    _logger.LogError(ex, "Výjimka při A2S dotazu na server {ServerId}.", id);
                }

                return Ok(new GameServerDetailDto
                {
                    GameServerId = basicDto.GameServerId,
                    Name = basicDto.Name,
                    GameType = basicDto.GameType,
                    Status = basicDto.Status,
                    IpAddress = basicDto.IpAddress,
                    Port = basicDto.Port,
                    ContainerId = basicDto.ContainerId,
                    CreatedAt = basicDto.CreatedAt,
                    StatusDetails = $"Server je online, ale A2S dotaz selhal: {a2sErrorMsg}",
                    GameName = basicDto.Name,
                    MapName = "N/A (A2S selhalo)",
                    CurrentPlayers = 0,
                    MaxPlayers = 0,
                    IsVacSecured = false,
                    Players = new List<PlayerDetailDto>()
                });
            }
            else
            {
                _logger.LogInformation("Server {ServerId} není online nebo nemá IP/Port pro A2S dotaz. Vracím základní informace.", id);
                return Ok(new GameServerDetailDto
                {
                    GameServerId = basicDto.GameServerId,
                    Name = basicDto.Name,
                    GameType = basicDto.GameType,
                    Status = basicDto.Status,
                    IpAddress = basicDto.IpAddress,
                    Port = basicDto.Port,
                    ContainerId = basicDto.ContainerId,
                    CreatedAt = basicDto.CreatedAt,
                    StatusDetails = basicDto.StatusDetails ?? "Server není online nebo chybí IP/Port pro detailní dotaz.",
                    Players = new List<PlayerDetailDto>()
                });
            }
        }


        [HttpGet("{id}/logs")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")] // Restricted
        public async Task<ActionResult<string>> GetGameServerLogs(Guid id, [FromQuery] uint tail = 200)
        {
            _logger.LogInformation("Požadavek na logy pro server {ServerId}, tail {Tail}", id, tail);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId))
            {
                _logger.LogWarning("Server {ServerId} nebo jeho kontejner nenalezen pro logy.", id);
                return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            }
            var logs = await _dockerService.GetContainerLogsAsync(server.ContainerId, null, tail);

            if (logs.Any(l => l.StartsWith("Chyba:") || l == "Kontejner nenalezen." || l == "Nepodařilo se získat stream logů."))
            {
                _logger.LogWarning("Problém při získávání logů pro kontejner {ContainerId}: {FirstLogLine}", server.ContainerId, logs.FirstOrDefault());
                return BadRequest(new { message = string.Join("\n", logs) });
            }
            _logger.LogInformation("Logy pro kontejner {ContainerId} úspěšně získány.", server.ContainerId);
            return Ok(string.Join("\n", logs));
        }

        [HttpPost("{id}/start")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")] // Restricted
        public async Task<IActionResult> StartGameServer(Guid id)
        {
            _logger.LogInformation("Požadavek na spuštění serveru {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });

            if (server.Status == ServerStatus.Online || server.Status == ServerStatus.Starting)
                return BadRequest(new { message = "Server již běží nebo se spouští." });

            var success = await _dockerService.StartContainerAsync(server.ContainerId);
            if (success)
            {
                server.Status = ServerStatus.Starting;
                server.StatusDetails = "Příkaz ke spuštění kontejneru odeslán.";
                await _gameServerRepository.UpdateAsync(server);
                await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
                _logger.LogInformation("Příkaz ke spuštění serveru {ServerId} odeslán.", id);
                return Ok(new { message = "Příkaz ke spuštění serveru odeslán." });
            }
            _logger.LogError("Nepodařilo se spustit server {ServerId} (kontejner {ContainerId}).", id, server.ContainerId);
            server.Status = ServerStatus.Error;
            server.StatusDetails = "Nepodařilo se spustit Docker kontejner.";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
            return StatusCode(500, new { message = "Nepodařilo se spustit server." });
        }

        [HttpPost("{id}/stop")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")] // Restricted
        public async Task<IActionResult> StopGameServer(Guid id)
        {
            _logger.LogInformation("Požadavek na zastavení serveru {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            if (server.Status == ServerStatus.Offline || server.Status == ServerStatus.Stopping)
                return BadRequest(new { message = "Server již neběží nebo se zastavuje." });

            server.Status = ServerStatus.Stopping;
            server.StatusDetails = "Server se zastavuje...";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));

            _ = Task.Run(async () => {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                    var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<GameServersController>>();
                    var scopedDockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();

                    GameServer? finalServerState = null;
                    try
                    {
                        bool success = await scopedDockerService.StopContainerAsync(server.ContainerId);

                        finalServerState = await scopedRepo.GetByIdAsync(id);
                        if (finalServerState != null)
                        {
                            finalServerState.Status = success ? ServerStatus.Offline : ServerStatus.Error;
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
                            finalServerState.Status = ServerStatus.Error;
                            finalServerState.StatusDetails = $"Výjimka při zastavování: {ex.GetType().Name}";
                            await scopedRepo.UpdateAsync(finalServerState);
                            await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(finalServerState));
                        }
                    }
                }
            });
            return Ok(new { message = "Příkaz k zastavení serveru odeslán." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")] // Restricted
        public async Task<IActionResult> DeleteGameServer(Guid id)
        {
            _logger.LogInformation("Požadavek na smazání serveru {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null) return NotFound(new { message = $"Server s ID {id} nenalezen." });
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
            _logger.LogInformation("Server {ServerId} smazán.", id);
            return NoContent();
        }
    }
}
