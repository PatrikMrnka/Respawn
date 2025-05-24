using RespawnApi.Domain.Enums;
using RespawnApi.Domain.Entities;
using RespawnApi.Application.DTOs.DockerAdmin;

namespace RespawnApi.Application.Interfaces
{
    public interface IDockerService
    {
        // Metody pro GameServer (ponechány)
        Task<(string? ContainerId, string? ErrorMessage)> CreateContainerAsync(GameServer serverDetails, string gameImageTag, string gameIdentifier, string? additionalGsParams);
        Task<string?> GetContainerStatusAsync(string containerId); // Vrací Docker status (running, exited)
        Task<(string? LgsmStatus, string? ErrorMessage)> GetLgsmServerDetailsAsync(string containerId);
        Task<bool> StopContainerAsync(string containerId);
        Task<bool> StartContainerAsync(string containerId);
        Task<bool> RemoveContainerAsync(string containerId, bool removeAssociatedVolume); // Změněn parametr
        Task<string> GetContainerLogsAsync(string containerId, uint tail = 200);

        // Nové metody pro Docker Admin
        Task<IEnumerable<DockerVolumeDto>> ListVolumesAsync();
        Task<bool> RemoveVolumeAsync(string volumeName, bool force = false);

        Task<IEnumerable<DockerContainerDto>> ListContainersAsync(bool all = true);
        // StartContainerAsync, StopContainerAsync, RemoveContainerAsync, GetContainerLogsAsync jsou již definovány

        Task<IEnumerable<DockerImageDto>> ListImagesAsync(bool all = false);
        Task<(bool Success, string? ErrorMessage)> RemoveImageAsync(string imageId, bool force = false, bool pruneChildren = false);
    }
}