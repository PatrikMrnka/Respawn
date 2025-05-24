// Controllers/DockerAdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // Přidáno pro IHubContext
using RespawnApi.Application.DTOs.DockerAdmin;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs; // Přidáno pro GameServerHub
using RespawnApi.DataAccess.Interfaces; // Přidáno pro IGameServerRepository
using System.Collections.Generic;
using System.Linq; // Přidáno pro FirstOrDefault
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.GameServer; // Přidáno pro ILogger

namespace RespawnApi.Controllers
{
    [Route("api/dockeradmin")]
    [ApiController]
    [Authorize(Roles = UserRoles.Administrator)]
    public class DockerAdminController : ControllerBase
    {
        private readonly IDockerService _dockerService;
        private readonly IGameServerRepository _gameServerRepository; // Přidáno
        private readonly IHubContext<GameServerHub> _gameServerHubContext; // Přidáno
        private readonly ILogger<DockerAdminController> _logger;

        public DockerAdminController(
            IDockerService dockerService,
            IGameServerRepository gameServerRepository, // Přidána injekce
            IHubContext<GameServerHub> gameServerHubContext, // Přidána injekce
            ILogger<DockerAdminController> logger)
        {
            _dockerService = dockerService;
            _gameServerRepository = gameServerRepository; // Přiřazení
            _gameServerHubContext = gameServerHubContext; // Přiřazení
            _logger = logger;
        }

        // --- Volumes ---
        [HttpGet("volumes")]
        public async Task<ActionResult<IEnumerable<DockerVolumeDto>>> GetVolumes()
        {
            var volumes = await _dockerService.ListVolumesAsync();
            return Ok(volumes);
        }

        [HttpDelete("volumes/{volumeName}")]
        public async Task<IActionResult> DeleteVolume(string volumeName, [FromQuery] bool force = false)
        {
            _logger.LogInformation("Požadavek na smazání volume: {VolumeName}, Force: {Force}", volumeName, force);
            var success = await _dockerService.RemoveVolumeAsync(volumeName, force);
            if (success) return NoContent();
            return BadRequest(new { message = $"Nepodařilo se smazat volume '{volumeName}'." });
        }

        // --- Containers ---
        [HttpGet("containers")]
        public async Task<ActionResult<IEnumerable<DockerContainerDto>>> GetContainers([FromQuery] bool all = true)
        {
            var containers = await _dockerService.ListContainersAsync(all);
            return Ok(containers);
        }

        [HttpPost("containers/{containerId}/start")]
        public async Task<IActionResult> StartContainer(string containerId)
        {
            _logger.LogInformation("Požadavek na spuštění kontejneru: {ContainerId}", containerId);
            var success = await _dockerService.StartContainerAsync(containerId);
            if (success)
            {
                // Po spuštění kontejneru můžeme chtít aktualizovat stav GameServeru, pokud existuje
                var gameServer = (await _gameServerRepository.GetAllAsync()).FirstOrDefault(gs => gs.ContainerId == containerId);
                if (gameServer != null)
                {
                    gameServer.Status = ServerStatus.Starting; // Nebo jiný vhodný stav
                    await _gameServerRepository.UpdateAsync(gameServer);
                    await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto
                    {
                        GameServerId = gameServer.GameServerId,
                        NewOverallStatus = gameServer.Status,
                        StatusDetails = "Kontejner spuštěn přes Docker Admin."
                    });
                }
                return Ok(new { message = "Kontejner úspěšně spuštěn." });
            }
            return BadRequest(new { message = $"Nepodařilo se spustit kontejner '{containerId}'." });
        }

        [HttpPost("containers/{containerId}/stop")]
        public async Task<IActionResult> StopContainer(string containerId)
        {
            _logger.LogInformation("Požadavek na zastavení kontejneru: {ContainerId}", containerId);
            var success = await _dockerService.StopContainerAsync(containerId);
            if (success)
            {
                var gameServer = (await _gameServerRepository.GetAllAsync()).FirstOrDefault(gs => gs.ContainerId == containerId);
                if (gameServer != null)
                {
                    gameServer.Status = ServerStatus.Stopping; // Nebo Offline, pokud stop je okamžitý
                    await _gameServerRepository.UpdateAsync(gameServer);
                    await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto
                    {
                        GameServerId = gameServer.GameServerId,
                        NewOverallStatus = gameServer.Status,
                        StatusDetails = "Kontejner zastaven přes Docker Admin."
                    });
                }
                return Ok(new { message = "Kontejner úspěšně zastaven." });
            }
            return BadRequest(new { message = $"Nepodařilo se zastavit kontejner '{containerId}'." });
        }

        [HttpDelete("containers/{containerId}")]
        public async Task<IActionResult> DeleteContainer(string containerId, [FromQuery] bool removeAssociatedVolume = false)
        {
            _logger.LogInformation("Požadavek na smazání kontejneru: {ContainerId}, RemoveAssociatedVolume: {RemoveAssociatedVolume}", containerId, removeAssociatedVolume);

            // Najdeme GameServer spojený s tímto kontejnerem PŘED smazáním kontejneru
            var gameServer = (await _gameServerRepository.GetAllAsync()).FirstOrDefault(gs => gs.ContainerId == containerId);

            var success = await _dockerService.RemoveContainerAsync(containerId, removeAssociatedVolume);
            if (success)
            {
                if (gameServer != null)
                {
                    _logger.LogInformation("Docker kontejner {ContainerId} smazán. Mažu asociovaný GameServer {GameServerId}.", containerId, gameServer.GameServerId);
                    await _gameServerRepository.DeleteAsync(gameServer.GameServerId);
                    await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerRemoval", gameServer.GameServerId);
                    _logger.LogInformation("GameServer {GameServerId} smazán z databáze.", gameServer.GameServerId);
                }
                else
                {
                    _logger.LogInformation("Docker kontejner {ContainerId} smazán. Nebyl nalezen žádný asociovaný GameServer v databázi.", containerId);
                }
                return NoContent();
            }
            return BadRequest(new { message = $"Nepodařilo se smazat kontejner '{containerId}'." });
        }

        [HttpGet("containers/{containerId}/logs")]
        public async Task<ActionResult<string>> GetContainerLogs(string containerId, [FromQuery] uint tail = 200)
        {
            _logger.LogInformation("Požadavek na logy kontejneru: {ContainerId}, Tail: {Tail}", containerId, tail);
            var logs = await _dockerService.GetContainerLogsAsync(containerId, null, tail);

            // Assuming logs is a List<string>, we need to check its contents instead of treating it as a single string.
            if (logs.Any(log => log.StartsWith("Chyba:")) || logs.Contains("Kontejner nenalezen.") || logs.Contains("Nepodařilo se získat stream logů."))
            {
                return BadRequest(new { message = string.Join(" ", logs) });
            }

            return Ok(string.Join("\n", logs)); // Combine the logs into a single string for the response.
        }

        // --- Images ---
        [HttpGet("images")]
        public async Task<ActionResult<IEnumerable<DockerImageDto>>> GetImages([FromQuery] bool all = false)
        {
            var images = await _dockerService.ListImagesAsync(all);
            return Ok(images);
        }

        [HttpDelete("images/{imageId}")]
        public async Task<IActionResult> DeleteImage(string imageId, [FromQuery] bool force = false, [FromQuery] bool pruneChildren = false)
        {
            _logger.LogInformation("Požadavek na smazání image: {ImageId}, Force: {Force}, Prune: {Prune}", imageId, force, pruneChildren);
            var (success, errorMessage) = await _dockerService.RemoveImageAsync(imageId, force, pruneChildren);
            if (success) return NoContent();
            return BadRequest(new { message = errorMessage ?? $"Nepodařilo se smazat image '{imageId}'." });
        }
    }
}
