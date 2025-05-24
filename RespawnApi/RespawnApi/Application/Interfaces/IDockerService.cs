// File: haha/RespawnApi/RespawnApi/Application/Interfaces/IDockerService.cs
using RespawnApi.Domain.Enums;
using RespawnApi.Domain.Entities;
using RespawnApi.Application.DTOs.DockerAdmin;
using System; // Required for Func and DateTime
using System.Collections.Generic; // Required for IEnumerable and List
using System.Threading; // Required for CancellationToken
using System.Threading.Tasks; // Required for Task

namespace RespawnApi.Application.Interfaces
{
    public interface IDockerService
    {
        Task<(string? ContainerId, string? ErrorMessage)> CreateContainerAsync(GameServer serverDetails, string gameImageTag, string gameIdentifier, string? additionalGsParams);
        Task<string?> GetContainerStatusAsync(string containerId);
        Task<bool> StopContainerAsync(string containerId);
        Task<bool> StartContainerAsync(string containerId);
        Task<bool> RemoveContainerAsync(string containerId, bool removeAssociatedVolume);
        Task<List<string>> GetContainerLogsAsync(string containerId, DateTime? since = null, uint lines = 200);

        /// <summary>
        /// Asynchronously streams logs from a specified container.
        /// </summary>
        /// <param name="containerId">The ID of the container.</param>
        /// <param name="onLogLineReceived">A callback function that is invoked for each log line received. The string parameter is the log line.</param>
        /// <param name="cancellationToken">A token to signal cancellation of the log streaming.</param>
        /// <returns>A task representing the asynchronous log streaming operation.</returns>
        Task StreamContainerLogsAsync(string containerId, Func<string, Task> onLogLineReceived, CancellationToken cancellationToken);

        Task<IEnumerable<DockerVolumeDto>> ListVolumesAsync();
        Task<bool> RemoveVolumeAsync(string volumeName, bool force = false);
        Task<IEnumerable<DockerContainerDto>> ListContainersAsync(bool all = true);
        Task<IEnumerable<DockerImageDto>> ListImagesAsync(bool all = false);
        Task<(bool Success, string? ErrorMessage)> RemoveImageAsync(string imageId, bool force = false, bool pruneChildren = false);
    }
}
