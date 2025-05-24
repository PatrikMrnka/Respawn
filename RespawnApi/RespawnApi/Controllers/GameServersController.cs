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
    public class GameServersController : ControllerBase
    {
        private readonly IDockerService _dockerService;
        private readonly IHubContext<GameServerHub> _gameServerHubContext;
        private readonly ILogger<GameServersController> _logger;
        private readonly IGameServerRepository _gameServerRepository;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IGameServerQueryService _gameServerQueryService; // Zůstává IGameServerQueryService

        public GameServersController(
            IDockerService dockerService,
            IHubContext<GameServerHub> gameServerHubContext,
            ILogger<GameServersController> logger,
            IGameServerRepository gameServerRepository,
            IServiceScopeFactory scopeFactory,
            IGameServerQueryService gameServerQueryService) // Injektuje se nová implementace
        {
            _dockerService = dockerService;
            _gameServerHubContext = gameServerHubContext;
            _logger = logger;
            _gameServerRepository = gameServerRepository;
            _scopeFactory = scopeFactory;
            _gameServerQueryService = gameServerQueryService;
        }

        // ... MapToDto, GetGameImageTag, GetGameIdentifier zůstávají stejné ...
        private GameServerDto MapToDto(GameServer s)
        {
            if (s == null) { /* ... */ return new GameServerDto { GameServerId = Guid.Empty, Name = "[CHYBA]", Status = ServerStatus.Unknown }; }
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
        [Authorize]
        public async Task<ActionResult<IEnumerable<GameServerDto>>> GetGameServers()
        {
            // ... stávající implementace ...
            _logger.LogInformation("Endpoint GetGameServers byl zavolán.");
            var servers = await _gameServerRepository.GetAllAsync();
            var dtos = servers?.Where(s => s != null).Select(MapToDto).ToList() ?? new List<GameServerDto>();
            return Ok(dtos);
        }

        // POST: api/gameservers
        [HttpPost]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<ActionResult<GameServerDto>> CreateGameServer(CreateGameServerDto createDto)
        {
            // ... stávající implementace ...
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
            if (createDto.GameType == GameType.CounterStrike) gameServer.Port = 27015;
            await _gameServerRepository.AddAsync(gameServer);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(gameServer));
            // Task.Run pro vytvoření kontejneru zůstává stejný
            _ = Task.Run(async () => {
                using var scope = _scopeFactory.CreateScope();
                var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<GameServersController>>();
                var scopedDockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();
                GameServer? serverToUpdate = await scopedRepo.GetByIdAsync(gameServer.GameServerId);
                if (serverToUpdate == null) { scopedLogger.LogError("Server {GameServerId} nenalezen pro update po AddAsync.", gameServer.GameServerId); return; }

                try
                {
                    serverToUpdate.StatusDetails = "Vytváření a spouštění Docker kontejneru...";
                    await scopedRepo.UpdateAsync(serverToUpdate); // Update status before long operation
                    await scopedHub.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto { GameServerId = serverToUpdate.GameServerId, NewOverallStatus = serverToUpdate.Status, StatusDetails = serverToUpdate.StatusDetails });


                    var imageTag = GetGameImageTag(serverToUpdate.GameType);
                    var gameIdentifier = GetGameIdentifier(serverToUpdate.GameType);
                    var (containerId, errorMessage) = await scopedDockerService.CreateContainerAsync(serverToUpdate, imageTag, gameIdentifier, createDto.AdditionalGsParams);

                    serverToUpdate = await scopedRepo.GetByIdAsync(gameServer.GameServerId); // Re-fetch
                    if (serverToUpdate == null) { scopedLogger.LogError("Server {GameServerId} nenalezen po CreateContainerAsync.", gameServer.GameServerId); return; }


                    if (containerId != null)
                    {
                        serverToUpdate.ContainerId = containerId; serverToUpdate.IpAddress = "localhost";
                        serverToUpdate.Status = ServerStatus.Starting;
                        serverToUpdate.StatusDetails = "Kontejner vytvořen, server se spouští/instaluje.";
                    }
                    else
                    {
                        serverToUpdate.Status = ServerStatus.Error;
                        serverToUpdate.StatusDetails = errorMessage ?? "Neznámá chyba při vytváření kontejneru.";
                    }
                    await scopedRepo.UpdateAsync(serverToUpdate);
                    await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(serverToUpdate));
                }
                catch (Exception ex)
                {
                    scopedLogger.LogError(ex, "Výjimka v Task.Run při vytváření kontejneru pro server {ServerId}.", gameServer.GameServerId);
                    var srvErr = await scopedRepo.GetByIdAsync(gameServer.GameServerId);
                    if (srvErr != null)
                    {
                        srvErr.Status = ServerStatus.Error; srvErr.StatusDetails = $"Chyba na pozadí: {ex.Message}";
                        await scopedRepo.UpdateAsync(srvErr);
                        await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(srvErr));
                    }
                }
            });
            return CreatedAtAction(nameof(GetGameServer), new { id = gameServer.GameServerId }, MapToDto(gameServer));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<GameServerDto>> GetGameServer(Guid id)
        {
            // ... stávající implementace ...
            _logger.LogInformation("Požadavek na GetGameServer pro ID: {ServerId}", id);
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null) return NotFound(new { message = $"Server s ID {id} nebyl nalezen." });
            return Ok(MapToDto(server));
        }

        // GET: api/gameservers/{id}/details
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
                // Použijeme novou metodu z IGameServerQueryService
                GameServerDetailDto? detailedInfo = await _gameServerQueryService.GetServerDetailsAsync(serverEntity, basicDto);

                if (detailedInfo != null)
                {
                    // Strategie vrátila DTO (buď s plnými detaily, nebo základní s chybovou hláškou)
                    _logger.LogInformation("Strategie vrátila detaily pro server {ServerId}. StatusDetails: {StatusDetailsFromStrategy}", id, detailedInfo.StatusDetails);
                    return Ok(detailedInfo);
                }
                else
                {
                    // Neočekávaný null výsledek ze strategie (měla by vždy vrátit DTO)
                    _logger.LogError("Strategie pro server {ServerId} ({GameType}) vrátila neočekávaně null.", id, serverEntity.GameType);
                    var fallbackDto = new GameServerDetailDto
                    { /* ... naplnit z basicDto ... */
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

        // ... ostatní metody (GetGameServerLogs, StartGameServer, StopGameServer, DeleteGameServer) zůstávají stejné
        // s již aplikovanou autorizací na úrovni metody ...
        [HttpGet("{id}/logs")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<ActionResult<string>> GetGameServerLogs(Guid id, [FromQuery] uint tail = 200)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            var logs = await _dockerService.GetContainerLogsAsync(server.ContainerId, null, tail);
            if (logs.Any(l => l.StartsWith("Chyba:"))) return BadRequest(new { message = string.Join("\n", logs) });
            return Ok(string.Join("\n", logs));
        }

        [HttpPost("{id}/start")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<IActionResult> StartGameServer(Guid id)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            if (server.Status == ServerStatus.Online || server.Status == ServerStatus.Starting) return BadRequest(new { message = "Server již běží nebo se spouští." });
            var success = await _dockerService.StartContainerAsync(server.ContainerId);
            if (success)
            { /* ... aktualizace a SignalR ... */
                server.Status = ServerStatus.Starting; server.StatusDetails = "Příkaz ke spuštění odeslán.";
                await _gameServerRepository.UpdateAsync(server);
                await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
                return Ok(new { message = "Příkaz ke spuštění serveru odeslán." });
            }
            server.Status = ServerStatus.Error; server.StatusDetails = "Nepodařilo se spustit Docker kontejner.";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));
            return StatusCode(500, new { message = "Nepodařilo se spustit server." });
        }

        [HttpPost("{id}/stop")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<IActionResult> StopGameServer(Guid id)
        {
            var server = await _gameServerRepository.GetByIdAsync(id);
            if (server == null || string.IsNullOrEmpty(server.ContainerId)) return NotFound(new { message = "Server nebo jeho kontejner nenalezen." });
            if (server.Status == ServerStatus.Offline || server.Status == ServerStatus.Stopping) return BadRequest(new { message = "Server již neběží nebo se zastavuje." });

            server.Status = ServerStatus.Stopping; server.StatusDetails = "Server se zastavuje...";
            await _gameServerRepository.UpdateAsync(server);
            await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(server));

            _ = Task.Run(async () => { /* ... stávající logika pro zastavení na pozadí ... */
                using var scope = _scopeFactory.CreateScope();
                var scopedRepo = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();
                var scopedDockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();
                GameServer? finalServerState = await scopedRepo.GetByIdAsync(id);
                if (finalServerState == null) return;

                bool success = await scopedDockerService.StopContainerAsync(finalServerState.ContainerId!);
                finalServerState.Status = success ? ServerStatus.Offline : ServerStatus.Error;
                finalServerState.StatusDetails = success ? "Server úspěšně zastaven." : "Chyba při zastavování serveru.";
                await scopedRepo.UpdateAsync(finalServerState);
                await scopedHub.Clients.All.SendAsync("ReceiveGameServerUpdate", MapToDto(finalServerState));
            });
            return Ok(new { message = "Příkaz k zastavení serveru odeslán." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
        public async Task<IActionResult> DeleteGameServer(Guid id)
        {
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
            return NoContent();
        }
    }
}
