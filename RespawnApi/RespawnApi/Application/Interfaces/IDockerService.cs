using RespawnApi.Domain.Enums;
using RespawnApi.Domain.Entities;
using RespawnApi.Application.DTOs.DockerAdmin;

namespace RespawnApi.Application.Interfaces
{
    public interface IDockerService
    {
        Task<(string? ContainerId, string? ErrorMessage)> CreateContainerAsync(GameServer serverDetails, string gameImageTag, string gameIdentifier, string? additionalGsParams);
        Task<string?> GetContainerStatusAsync(string containerId);
        Task<bool> StopContainerAsync(string containerId);
        Task<bool> StartContainerAsync(string containerId);
        Task<bool> RemoveContainerAsync(string containerId, bool removeAssociatedVolume);
        // Upraveno: since pro získání logů od určitého času, lines pro počet řádků
        Task<List<string>> GetContainerLogsAsync(string containerId, DateTime? since = null, uint lines = 200);

        Task<IEnumerable<DockerVolumeDto>> ListVolumesAsync();
        Task<bool> RemoveVolumeAsync(string volumeName, bool force = false);
        Task<IEnumerable<DockerContainerDto>> ListContainersAsync(bool all = true);
        Task<IEnumerable<DockerImageDto>> ListImagesAsync(bool all = false);
        Task<(bool Success, string? ErrorMessage)> RemoveImageAsync(string imageId, bool force = false, bool pruneChildren = false);
    }
}