using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.Docker;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Application.DTOs.GameServer;

namespace RespawnApi.Controllers
{
    [Route("api/dockeradmin")]
    [ApiController]
    [Authorize(Roles = UserRoles.Administrator)]
    public class DockerAdminController : ControllerBase
    {
        private readonly IContainerManagementService _containerManagementService;
        private readonly IVolumeManagementService _volumeManagementService;
        private readonly IImageManagementService _imageManagementService;
        private readonly IGameServerRepository _gameServerRepository;
        private readonly IHubContext<GameServerHub> _gameServerHubContext;
        private readonly ILogger<DockerAdminController> _logger;

        public DockerAdminController(
            IContainerManagementService containerManagementService,
            IVolumeManagementService volumeManagementService,
            IImageManagementService imageManagementService,
            IGameServerRepository gameServerRepository,
            IHubContext<GameServerHub> gameServerHubContext,
            ILogger<DockerAdminController> logger)
        {
            _containerManagementService = containerManagementService;
            _volumeManagementService = volumeManagementService;
            _imageManagementService = imageManagementService;
            _gameServerRepository = gameServerRepository;
            _gameServerHubContext = gameServerHubContext;
            _logger = logger;
        }

        // --- Volumes ---

        /// <summary>
        /// Retrieves a list of all Docker volumes.
        /// </summary>
        /// <returns>A collection of Docker volume DTOs.</returns>
        [HttpGet("volumes")]
        public async Task<ActionResult<IEnumerable<DockerVolumeDto>>> GetVolumes()
        {
            var volumes = await _volumeManagementService.ListVolumesAsync();
            return Ok(volumes);
        }

        /// <summary>
        /// Deletes a specified Docker volume.
        /// </summary>
        /// <param name="volumeName">The name of the volume to delete.</param>
        /// <param name="force">Whether to force deletion even if the volume is in use.</param>
        /// <returns>No content if successful; otherwise, a bad request with an error message.</returns>
        [HttpDelete("volumes/{volumeName}")]
        public async Task<IActionResult> DeleteVolume(string volumeName, [FromQuery] bool force = false)
        {
            _logger.LogInformation("Požadavek na smazání volume: {VolumeName}, Force: {Force}", volumeName, force);
            var success = await _volumeManagementService.RemoveVolumeAsync(volumeName, force);

            if (success)
            {
                return NoContent();
            }

            return BadRequest(new { message = $"Nepodařilo se smazat volume '{volumeName}'." });
        }

        // --- Containers ---

        /// <summary>
        /// Retrieves a list of Docker containers.
        /// </summary>
        /// <param name="all">Whether to include stopped containers.</param>
        /// <returns>A collection of Docker container DTOs.</returns>
        [HttpGet("containers")]
        public async Task<ActionResult<IEnumerable<DockerContainerDto>>> GetContainers([FromQuery] bool all = true)
        {
            var containers = await _containerManagementService.ListContainersAsync(all);
            return Ok(containers);
        }

        /// <summary>
        /// Starts a specified Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container to start.</param>
        /// <returns>Ok if started; otherwise, a bad request with an error message.</returns>
        [HttpPost("containers/{containerId}/start")]
        public async Task<IActionResult> StartContainer(string containerId)
        {
            _logger.LogInformation("Požadavek na spuštění kontejneru: {ContainerId}", containerId);
            var success = await _containerManagementService.StartContainerAsync(containerId);
            if (success)
            {
                var gameServer =
                    (await _gameServerRepository.GetAllAsync()).FirstOrDefault(gs => gs.ContainerId == containerId);

                if (gameServer != null)
                {
                    gameServer.Status = ServerStatus.Starting;
                    await _gameServerRepository.UpdateAsync(gameServer);
                    await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate",
                        new GameServerStatusUpdateDto
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

        /// <summary>
        /// Stops a specified Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container to stop.</param>
        /// <returns>Ok if stopped; otherwise, a bad request with an error message.</returns>
        [HttpPost("containers/{containerId}/stop")]
        public async Task<IActionResult> StopContainer(string containerId)
        {
            _logger.LogInformation("Požadavek na zastavení kontejneru: {ContainerId}", containerId);
            var success = await _containerManagementService.StopContainerAsync(containerId);

            if (success)
            {
                var gameServer =
                    (await _gameServerRepository.GetAllAsync()).FirstOrDefault(gs => gs.ContainerId == containerId);
                if (gameServer != null)
                {
                    gameServer.Status = ServerStatus.Stopping;
                    await _gameServerRepository.UpdateAsync(gameServer);
                    await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate",
                        new GameServerStatusUpdateDto
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

        /// <summary>
        /// Deletes a specified Docker container and optionally its associated volume.
        /// </summary>
        /// <param name="containerId">The ID of the container to delete.</param>
        /// <param name="removeAssociatedVolume">Whether to also remove the associated volume.</param>
        /// <returns>No content if successful; otherwise, a bad request with an error message.</returns>
        [HttpDelete("containers/{containerId}")]
        public async Task<IActionResult> DeleteContainer(string containerId,
            [FromQuery] bool removeAssociatedVolume = false)
        {
            _logger.LogInformation(
                "Požadavek na smazání kontejneru: {ContainerId}, RemoveAssociatedVolume: {RemoveAssociatedVolume}",
                containerId, removeAssociatedVolume);

            // find the associated GameServer if it exists
            var gameServer =
                (await _gameServerRepository.GetAllAsync()).FirstOrDefault(gs => gs.ContainerId == containerId);

            var success = await _containerManagementService.RemoveContainerAsync(containerId, removeAssociatedVolume);
            if (success)
            {
                if (gameServer != null)
                {
                    _logger.LogInformation(
                        "Docker kontejner {ContainerId} smazán. Mažu asociovaný GameServer {GameServerId}.",
                        containerId, gameServer.GameServerId);
                    await _gameServerRepository.DeleteAsync(gameServer.GameServerId);
                    await _gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerRemoval",
                        gameServer.GameServerId);
                    _logger.LogInformation("GameServer {GameServerId} smazán z databáze.", gameServer.GameServerId);
                }
                else
                {
                    _logger.LogInformation(
                        "Docker kontejner {ContainerId} smazán. Nebyl nalezen žádný asociovaný GameServer v databázi.",
                        containerId);
                }

                return NoContent();
            }

            return BadRequest(new { message = $"Nepodařilo se smazat kontejner '{containerId}'." });
        }

        /// <summary>
        /// Retrieves the logs of a specified Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container.</param>
        /// <param name="tail">The number of log lines to retrieve from the end.</param>
        /// <returns>The logs as a string if successful; otherwise, a bad request with an error message.</returns>
        [HttpGet("containers/{containerId}/logs")]
        public async Task<ActionResult<string>> GetContainerLogs(string containerId, [FromQuery] uint tail = 200)
        {
            _logger.LogInformation("Požadavek na logy kontejneru: {ContainerId}, Tail: {Tail}", containerId, tail);
            var logs = await _containerManagementService.GetContainerLogsAsync(containerId, null, tail);

            if (logs.Any(log => log.StartsWith("Chyba:")) || logs.Contains("Kontejner nenalezen.") ||
                logs.Contains("Nepodařilo se získat stream logů."))
            {
                return BadRequest(new { message = string.Join(" ", logs) });
            }

            return Ok(string.Join("\n", logs)); // Combine the logs into a single string for the response.
        }

        // --- Images ---

        /// <summary>
        /// Retrieves a list of Docker images.
        /// </summary>
        /// <param name="all">Whether to include intermediate images.</param>
        /// <returns>A collection of Docker image DTOs.</returns>
        [HttpGet("images")]
        public async Task<ActionResult<IEnumerable<DockerImageDto>>> GetImages([FromQuery] bool all = false)
        {
            var images = await _imageManagementService.ListImagesAsync(all);
            return Ok(images);
        }

        /// <summary>
        /// Deletes a specified Docker image.
        /// </summary>
        /// <param name="imageId">The ID or tag of the image to delete.</param>
        /// <param name="force">Whether to force deletion even if the image is used by a container.</param>
        /// <param name="pruneChildren">Whether to prune parent layers if they are no longer used.</param>
        /// <returns>No content if successful; otherwise, a bad request with an error message.</returns>
        [HttpDelete("images/{imageId}")]
        public async Task<IActionResult> DeleteImage(string imageId, [FromQuery] bool force = false,
            [FromQuery] bool pruneChildren = false)
        {
            _logger.LogInformation("Požadavek na smazání image: {ImageId}, Force: {Force}, Prune: {Prune}", imageId,
                force, pruneChildren);

            var (success, errorMessage) = await _imageManagementService.RemoveImageAsync(imageId, force, pruneChildren);
            if (success)
            {
                return NoContent();
            }

            return BadRequest(new { message = errorMessage ?? $"Nepodařilo se smazat image '{imageId}'." });
        }
    }
}