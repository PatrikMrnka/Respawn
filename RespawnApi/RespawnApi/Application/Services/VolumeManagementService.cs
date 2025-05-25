using Docker.DotNet;
using RespawnApi.Application.DTOs.Docker;
using RespawnApi.Application.Interfaces;

namespace RespawnApi.Application.Services
{
    /// <summary>
    /// Service for managing Docker volumes.
    /// </summary>
    public class VolumeManagementService : IVolumeManagementService
    {
        private readonly DockerClient _client;
        private readonly ILogger<VolumeManagementService> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeManagementService"/> class.
        /// </summary>
        public VolumeManagementService(IConfiguration configuration, ILogger<VolumeManagementService> logger)
        {
            _logger = logger;
            _configuration = configuration;
            var dockerApiUri = _configuration["DockerSettings:DockerApiUri"];
            if (string.IsNullOrEmpty(dockerApiUri))
            {
                _logger.LogError("Docker API URI není nakonfigurováno v DockerSettings:DockerApiUri.");
                throw new InvalidOperationException("Docker API URI není nakonfigurováno.");
            }

            _client = new DockerClientConfiguration(new Uri(dockerApiUri)).CreateClient();
            _logger.LogInformation("VolumeManagementService inicializován s URI: {DockerApiUri}", dockerApiUri);
        }

        public async Task<IEnumerable<DockerVolumeDto>> ListVolumesAsync()
        {
            try
            {
                var response = await _client.Volumes.ListAsync();
                return response.Volumes?.Select(v => new DockerVolumeDto // map response to DTO
                {
                    Name = v.Name,
                    Driver = v.Driver,
                    CreatedAt = DateTime.TryParse(v.CreatedAt, out var dt) ? dt : default,
                    Labels = (Dictionary<string, string>)(v.Labels ?? new Dictionary<string, string>()),
                    // Size is not directly available from ListVolumesResponse, would require inspect or df
                }) ?? Enumerable.Empty<DockerVolumeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při výpisu Docker volumes.");
                return Enumerable.Empty<DockerVolumeDto>();
            }
        }

        public async Task<bool> RemoveVolumeAsync(string volumeName, bool force = false)
        {
            try
            {
                _logger.LogInformation("Pokouším se smazat volume '{VolumeName}' s force={ForceFlag}.", volumeName,
                    force);
                await _client.Volumes.RemoveAsync(volumeName, force);

                _logger.LogInformation("Volume '{VolumeName}' úspěšně smazán.", volumeName);
                return true;
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning(ex,
                    "Konflikt při mazání volume '{VolumeName}' (pravděpodobně používán). Force={ForceFlag}", volumeName,
                    force);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při mazání Docker volume '{VolumeName}'.", volumeName);
                return false;
            }
        }
    }
}