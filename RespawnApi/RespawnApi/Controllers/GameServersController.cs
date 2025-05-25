using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs;
using RespawnApi.DataAccess.Interfaces;

namespace RespawnApi.Controllers
{
    /// <summary>
    /// Controller for managing game servers.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GameServersController : ControllerBase
    {
        private readonly IContainerManagementService _containerManagementService;
        private readonly IHubContext<GameServerHub> _gameServerHubContext;
        private readonly ILogger<GameServersController> _logger;
        private readonly IGameServerRepository _gameServerRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IGameServerQueryService _gameServerQueryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameServersController"/> class.
        /// </summary>
        public GameServersController(
            IContainerManagementService containerManagementService,
            IHubContext<GameServerHub> gameServerHubContext,
            ILogger<GameServersController> logger,
            IGameServerRepository gameServerRepository,
            IServiceScopeFactory scopeFactory,
            IGameServerQueryService gameServerQueryService)
        {
            _containerManagementService = containerManagementService;
            _gameServerHubContext = gameServerHubContext;
            _logger = logger;
            _gameServerRepository = gameServerRepository;
            _scopeFactory = scopeFactory;
            _gameServerQueryService = gameServerQueryService;
        }

        /// <summary>
        /// Maps a GameServer entity to a GameServerDto.
        /// </summary>
        /// <param name="s">The GameServer entity.</param>
        /// <returns>A GameServerDto.</returns>
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

        /// <summary>
        /// Gets the Docker image tag for a given game type.
        /// </summary>
        /// <param name="gameType">The type of the game.</param>
        /// <returns>The Docker image tag.</returns>
        private string GetGameImageTag(GameType gameType) => gameType switch
        {
            GameType.CounterStrike => "cs",
            GameType.TeamFortress2 => "tf2",
            GameType.GarrysMod => "gmod",
            _ => throw new ArgumentOutOfRangeException(nameof(gameType), $"Nepodporovaný typ hry: {gameType}")
        };

        /// <summary>
        /// Gets a game identifier string for a given game type, used in container/volume naming.
        /// </summary>
        /// <param name="gameType">The type of the game.</param>
        /// <returns>A game identifier string.</returns>
        private string GetGameIdentifier(GameType gameType) => gameType switch
        {
            GameType.CounterStrike => "csserver",
            GameType.TeamFortress2 => "tf2server",
            GameType.GarrysMod => "gmodserver",
            _ => $"unknownserver-{Guid.NewGuid().ToString().Substring(0, 4)}"
        };

        /// <summary>
        /// Gets a list of all game servers.
        /// </summary>
        /// <returns>A list of game servers.</returns>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<GameServerDto>>> GetGameServers()
        {
            _logger.LogInformation("Endpoint GetGameServers byl zavolán.");
            var servers = await _gameServerRepository.GetAllAsync();
            var dtos = servers?.Where(s => s != null).Select(MapToDto).ToList() ?? new List<GameServerDto>();

            return Ok(dtos);
        }

        /// <summary>
        /// Creates a new game server.
        /// The actual Docker container creation is performed in the background.
        /// </summary>
        /// <param name="createDto">Data for creating the new game server.</param>
        /// <returns>The created game server with an initial status.</returns>
        [HttpPost]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
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
                StatusDetails = "Čeká na inicializaci..." // Initial status detail
            };

            if (createDto.GameType == GameType.CounterStrike)
            {
                gameServer.Port = 27015;
                _logger.LogInformation("Nastavuji výchozí port 27015 pro Counter-Strike server {ServerName}.", createDto.Name);
            }

            try
            {
                await _gameServerRepository.AddAsync(gameServer);
                _logger.LogInformation("Herní server {GameServerId} ('{ServerName}') uložen do DB se stavem PendingCreation. Port: {Port}",
                    gameServer.GameServerId, gameServer.Name, gameServer.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při ukládání počátečního stavu GameServer {GameServerId} ('{ServerName}') do DB.",
                    gameServer.GameServerId, gameServer.Name);
                return StatusCode(500, new { message = "Chyba při ukládání serveru do databáze.", details = ex.Message });
            }

            // Send initial update to clients
            try
            {
                await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(gameServer));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při odesílání SignalR ReceiveGameServerUpdate po AddAsync pro {GameServerId} ('{ServerName}').",
                    gameServer.GameServerId, gameServer.Name);
            }

            // Start background task for container creation without awaiting it
            _ = PerformContainerCreationInBackgroundAsync(gameServer.GameServerId, gameServer.GameType, createDto.AdditionalGsParams);

            return CreatedAtAction(nameof(GetGameServer), new { id = gameServer.GameServerId }, MapToDto(gameServer));
        }

        /// <summary>
        /// Performs the Docker container creation and related updates in a background task.
        /// This method creates its own service scope to resolve scoped services.
        /// </summary>
        /// <param name="gameServerId">The ID of the game server to process.</param>
        /// <param name="gameType">The type of the game for the server.</param>
        /// <param name="additionalGsParams">Additional game server parameters for LGSM.</param>
        private async Task PerformContainerCreationInBackgroundAsync(Guid gameServerId, GameType gameType, string? additionalGsParams)
        {
            // Create a new scope to resolve scoped services for this background task
            using (var scope = _scopeFactory.CreateScope())
            {
                // Resolve services from the new scope
                var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                var scopedHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<GameServersController>>();
                var scopedContainerManagementService = scope.ServiceProvider.GetRequiredService<IContainerManagementService>();

                GameServer? serverToUpdate = null; // Initialize to null

                try
                {
                    scopedLogger.LogInformation("[BG Task - {GameServerId}] Zahajuji vytváření Docker kontejneru.", gameServerId);

                    serverToUpdate = await scopedRepo.GetByIdAsync(gameServerId);
                    if (serverToUpdate == null)
                    {
                        scopedLogger.LogError("[BG Task - {GameServerId}] Server nenalezen v DB pro update.", gameServerId);
                        return; // Cannot proceed if server entity is gone
                    }

                    serverToUpdate.StatusDetails = "Vytváření a spouštění Docker kontejneru...";
                    await scopedRepo.UpdateAsync(serverToUpdate);
                    await scopedHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto
                    {
                        GameServerId = serverToUpdate.GameServerId,
                        NewOverallStatus = serverToUpdate.Status,
                        StatusDetails = serverToUpdate.StatusDetails
                    });

                    var imageTag = GetGameImageTag(gameType); // Use passed gameType
                    var gameIdentifier = GetGameIdentifier(gameType); // Use passed gameType

                    // Pass the serverToUpdate (which is the GameServer entity) to CreateContainerAsync
                    var (containerId, errorMessage) = await scopedContainerManagementService.CreateContainerAsync(serverToUpdate, imageTag, gameIdentifier, additionalGsParams);

                    // Re-fetch the server entity to ensure we have the latest state before final update
                    // This is important if other operations could have modified it, though less likely in this specific flow.
                    serverToUpdate = await scopedRepo.GetByIdAsync(gameServerId);
                    if (serverToUpdate == null)
                    {
                        scopedLogger.LogError("[BG Task - {GameServerId}] Server nenalezen v DB po pokusu o vytvoření kontejneru.", gameServerId);
                        return;
                    }

                    if (containerId != null)
                    {
                        serverToUpdate.ContainerId = containerId;
                        serverToUpdate.IpAddress = "172.20.10.5"; // Default IP for local Docker with host networking - now hardcoded!!!
                        serverToUpdate.Status = ServerStatus.Starting; // Server is starting up within the container
                        serverToUpdate.StatusDetails = "Kontejner vytvořen, server se spouští/instaluje.";
                        scopedLogger.LogInformation("[BG Task - {GameServerId}] Docker kontejner {ContainerId} vytvořen. Stav: {Status}, IP: {IP}, Port: {Port}",
                            gameServerId, containerId, serverToUpdate.Status, serverToUpdate.IpAddress, serverToUpdate.Port);
                    }
                    else
                    {
                        serverToUpdate.Status = ServerStatus.Error;
                        serverToUpdate.StatusDetails = errorMessage ?? "Neznámá chyba při vytváření Docker kontejneru.";
                        scopedLogger.LogError("[BG Task - {GameServerId}] Chyba při vytváření Docker kontejneru: {ErrorMessage}",
                            gameServerId, serverToUpdate.StatusDetails);
                    }
                    await scopedRepo.UpdateAsync(serverToUpdate);
                    await scopedHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(serverToUpdate));
                }
                catch (Exception ex)
                {
                    scopedLogger.LogError(ex, "[BG Task - {GameServerId}] Výjimka při vytváření kontejneru na pozadí.", gameServerId);
                    // Attempt to update server to error state if possible and if serverToUpdate was fetched
                    if (serverToUpdate != null) // serverToUpdate might be null if GetByIdAsync failed early
                    {
                        // Re-fetch in case of concurrent updates or if serverToUpdate is stale after an exception
                        var serverForErrorUpdate = await scopedRepo.GetByIdAsync(gameServerId);
                        if (serverForErrorUpdate != null)
                        {
                            serverForErrorUpdate.Status = ServerStatus.Error;
                            serverForErrorUpdate.StatusDetails = $"Chyba na pozadí při vytváření: {ex.GetType().Name}";
                            try
                            {
                                await scopedRepo.UpdateAsync(serverForErrorUpdate);
                                await scopedHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(serverForErrorUpdate));
                            }
                            catch (Exception updateEx)
                            {
                                scopedLogger.LogError(updateEx, "[BG Task - {GameServerId}] Výjimka při ukládání finálního chybového stavu serveru.", gameServerId);
                            }
                        }
                        else
                        {
                            scopedLogger.LogError("[BG Task - {GameServerId}] Server nenalezen ani pro uložení chybového stavu po výjimce.", gameServerId);
                        }
                    }
                    else
                    {
                        // If serverToUpdate was never fetched (e.g. error before first GetByIdAsync)
                        scopedLogger.LogError("[BG Task - {GameServerId}] serverToUpdate bylo null, nelze aktualizovat na chybový stav po výjimce.", gameServerId);
                    }
                }
            } // Scope is disposed here, along with resolved scoped services
        }

        /// <summary>
        /// Gets a specific game server by its ID.
        /// </summary>
        /// <param name="id">The ID of the game server.</param>
        /// <returns>The game server DTO or NotFound.</returns>
        [HttpGet("{id}")]
        [Authorize]
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

        /// <summary>
        /// Gets detailed information about a specific game server, including live A2S data if available.
        /// </summary>
        /// <param name="id">The ID of the game server.</param>
        /// <returns>Detailed game server information or NotFound.</returns>
        [HttpGet("{id}/details")]
        [Authorize]
        public async Task<ActionResult<GameServerDetailDto>> GetGameServerDetails(Guid id)
        {
            _logger.LogInformation("Požadavek na GetGameServerDetails pro ID: {ServerId}", id);
            var serverEntity = await _gameServerRepository.GetByIdAsync(id);
            if (serverEntity == null)
            {
                _logger.LogWarning("Server s ID {ServerId} nenalezen pro detaily.", id);
                return NotFound(new { message = $"Server s ID {id} nebyl nalezen." });
            }

            var basicDto = MapToDto(serverEntity);

            if (serverEntity.Status == ServerStatus.Online && !string.IsNullOrEmpty(serverEntity.IpAddress) && serverEntity.Port.HasValue)
            {
                GameServerDetailDto? detailedInfo = await _gameServerQueryService.GetServerDetailsAsync(serverEntity, basicDto);

                if (detailedInfo != null)
                {
                    _logger.LogInformation("Strategie vrátila detaily pro server {ServerId}. StatusDetails: {StatusDetailsFromStrategy}", id, detailedInfo.StatusDetails);
                    return Ok(detailedInfo);
                }
                else
                {
                    _logger.LogError("Strategie pro server {ServerId} ({GameType}) vrátila neočekávaně null.", id, serverEntity.GameType);
                    var fallbackDto = new GameServerDetailDto
                    {
                        GameServerId = basicDto.GameServerId,
                        Name = basicDto.Name,
                        GameType = basicDto.GameType,
                        Status = basicDto.Status,
                        IpAddress = basicDto.IpAddress,
                        Port = basicDto.Port,
                        ContainerId = basicDto.ContainerId,
                        CreatedAt = basicDto.CreatedAt,
                        StatusDetails = "Interní chyba: Nepodařilo se zpracovat požadavek na detaily serveru.",
                        Players = new List<PlayerDetailDto>()
                    };
                    return Ok(fallbackDto);
                }
            }
            else
            {
                _logger.LogInformation("Server {ServerId} není online nebo nemá IP/Port pro detailní dotaz. Vracím základní informace.", id);
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

        /// <summary>
        /// Gets the logs for a specific game server's container.
        /// </summary>
        /// <param name="id">The ID of the game server.</param>
        /// <param name="tail">The number of recent log lines to retrieve.</param>
        /// <returns>The container logs or NotFound/BadRequest.</returns>
        [HttpGet("{id}/logs")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<ActionResult<string>> GetGameServerLogs(Guid id, [FromQuery] uint tail = 200)
        {
            _logger.LogInformation("Požadavek na logy pro server {ServerId}, tail {Tail}", id, tail);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId))
            {
                return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            }
            var logs = await _containerManagementService.GetContainerLogsAsync(server.ContainerId, null, tail);
            if (logs.Any(l => l.StartsWith("Chyba:") || l == "Kontejner nenalezen." || l == "Nepodařilo se získat stream logů."))
            {
                return BadRequest(new { message = string.Join("\n", logs) });
            }
            return Ok(string.Join("\n", logs));
        }

        /// <summary>
        /// Starts a specific game server.
        /// </summary>
        /// <param name="id">The ID of the game server to start.</param>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpPost("{id}/start")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<IActionResult> StartGameServer(Guid id)
        {
            _logger.LogInformation("Požadavek na spuštění serveru {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            if (server.Status == ServerStatus.Online || server.Status == ServerStatus.Starting) return BadRequest(new { message = "Server již běží nebo se spouští." });

            var success = await _containerManagementService.StartContainerAsync(server.ContainerId);
            if (success)
            {
                server.Status = ServerStatus.Starting; server.StatusDetails = "Příkaz ke spuštění kontejneru odeslán.";
                await _gameServerRepository.UpdateAsync(server);
                await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
                return Ok(new { message = "Příkaz ke spuštění serveru odeslán." });
            }
            server.Status = ServerStatus.Error; server.StatusDetails = "Nepodařilo se spustit Docker kontejner.";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
            return StatusCode(500, new { message = "Nepodařilo se spustit server." });
        }

        /// <summary>
        /// Stops a specific game server.
        /// The actual container stop is performed in the background.
        /// </summary>
        /// <param name="id">The ID of the game server to stop.</param>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpPost("{id}/stop")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<IActionResult> StopGameServer(Guid id)
        {
            _logger.LogInformation("Požadavek na zastavení serveru {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            if (server.Status == ServerStatus.Offline || server.Status == ServerStatus.Stopping) return BadRequest(new { message = "Server již neběží nebo se zastavuje." });

            server.Status = ServerStatus.Stopping; server.StatusDetails = "Server se zastavuje...";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                var scopedContainerManagementService = scope.ServiceProvider.GetRequiredService<IContainerManagementService>();
                var scopedLoggerStop = scope.ServiceProvider.GetRequiredService<ILogger<GameServersController>>();

                GameServer? srvToStop = await scopedRepo.GetByIdAsync(id); // Re-fetch in scope
                if (srvToStop == null || string.IsNullOrEmpty(srvToStop.ContainerId))
                {
                    scopedLoggerStop.LogError("[BG Stop Task - {ServerId}] Server nebo ContainerId nenalezen.", id);
                    return;
                }

                try
                {
                    bool stopped = await scopedContainerManagementService.StopContainerAsync(srvToStop.ContainerId);
                    srvToStop = await scopedRepo.GetByIdAsync(id); // Re-fetch before final update
                    if (srvToStop == null) { scopedLoggerStop.LogError("[BG Stop Task - {ServerId}] Server nenalezen po StopContainerAsync.", id); return; }

                    srvToStop.Status = stopped ? ServerStatus.Offline : ServerStatus.Error;
                    srvToStop.StatusDetails = stopped ? "Server úspěšně zastaven." : "Chyba při zastavování Docker kontejneru.";
                    await scopedRepo.UpdateAsync(srvToStop);
                    await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(srvToStop));
                }
                catch (Exception ex)
                {
                    scopedLoggerStop.LogError(ex, "[BG Stop Task - {ServerId}] Výjimka při zastavování kontejneru.", id);
                    var srvErr = await scopedRepo.GetByIdAsync(id);
                    if (srvErr != null)
                    {
                        srvErr.Status = ServerStatus.Error; srvErr.StatusDetails = $"Chyba na pozadí při zastavování: {ex.Message}";
                        await scopedRepo.UpdateAsync(srvErr);
                        await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(srvErr));
                    }
                }
            });
            return Ok(new { message = "Příkaz k zastavení serveru odeslán." });
        }

        /// <summary>
        /// Deletes a specific game server and its associated Docker container and volume.
        /// </summary>
        /// <param name="id">The ID of the game server to delete.</param>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<IActionResult> DeleteGameServer(Guid id)
        {
            _logger.LogInformation("Požadavek na smazání serveru {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null) return NotFound(new { message = $"Server s ID {id} nenalezen." });

            if (!string.IsNullOrEmpty(server.ContainerId))
            {
                // Attempt to stop the container if it's running before removal
                if (server.Status != ServerStatus.Offline && server.Status != ServerStatus.Error && server.Status != ServerStatus.PendingCreation)
                {
                    _logger.LogInformation("Pokouším se zastavit kontejner {ContainerId} před smazáním serveru {ServerId}.", server.ContainerId, id);
                    await _containerManagementService.StopContainerAsync(server.ContainerId);
                }
                _logger.LogInformation("Mažu kontejner {ContainerId} pro server {ServerId}.", server.ContainerId, id);
                await _containerManagementService.RemoveContainerAsync(server.ContainerId, true); // true to remove associated volume
            }
            else
            {
                _logger.LogWarning("Server {ServerId} nemá asociovaný ContainerId, kontejner a volume nebudou smazány přes DockerService.", id);
            }

            await _gameServerRepository.DeleteAsync(id);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerRemoval", id);
            _logger.LogInformation("Server {ServerId} smazán z databáze.", id);
            return NoContent();
        }
    }
}
