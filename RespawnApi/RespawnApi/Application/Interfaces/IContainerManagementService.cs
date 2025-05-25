using RespawnApi.Domain.Entities;
using RespawnApi.Application.DTOs.Docker;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RespawnApi.Application.DTOs.Docker;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages Docker containers.
    /// </summary>
    public interface IContainerManagementService
    {
        /// <summary>
        /// Creates a new Docker container for a game server.
        /// </summary>
        /// <param name="serverDetails">Details of the game server for which the container is being created.</param>
        /// <param name="gameImageTag">The specific tag of the LGSM image to use (e.g., "cs", "tf2").</param>
        /// <param name="gameIdentifier">A short identifier for the game (e.g., "csserver", "tf2server").</param>
        /// <param name="additionalGsParams">Additional game server parameters for LGSM.</param>
        /// <returns>A tuple containing the ID of the created container and an error message if creation failed.</returns>
        Task<(string? ContainerId, string? ErrorMessage)> CreateContainerAsync(GameServer serverDetails, string gameImageTag, string gameIdentifier, string? additionalGsParams);

        /// <summary>
        /// Gets the current status of a Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container.</param>
        /// <returns>A string representing the container's status (e.g., "running", "exited", "not_found").</returns>
        Task<string?> GetContainerStatusAsync(string containerId);

        /// <summary>
        /// Starts a Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container to start.</param>
        /// <returns>True if the container was started successfully, otherwise false.</returns>
        Task<bool> StartContainerAsync(string containerId);

        /// <summary>
        /// Stops a Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container to stop.</param>
        /// <returns>True if the container was stopped successfully, otherwise false.</returns>
        Task<bool> StopContainerAsync(string containerId);

        /// <summary>
        /// Removes a Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container to remove.</param>
        /// <param name="removeAssociatedVolume">Flag indicating whether to also remove the volume associated via the 'com.respawn.lgsm.volume' label.</param>
        /// <returns>True if the container was removed successfully, otherwise false.</returns>
        /// <remarks>The associated volume is identified by the 'com.respawn.lgsm.volume' label on the container.</remarks>
        Task<bool> RemoveContainerAsync(string containerId, bool removeAssociatedVolume);

        /// <summary>
        /// Gets a specified number of log lines from a Docker container.
        /// </summary>
        /// <param name="containerId">The ID of the container.</param>
        /// <param name="since">Optional. Only return logs since this DateTime.</param>
        /// <param name="lines">The number of log lines to retrieve from the end of the logs.</param>
        /// <returns>A list of log lines.</returns>
        Task<List<string>> GetContainerLogsAsync(string containerId, DateTime? since = null, uint lines = 200);

        /// <summary>
        /// Asynchronously streams logs from a specified container.
        /// </summary>
        /// <param name="containerId">The ID of the container.</param>
        /// <param name="onLogLineReceived">A callback function invoked for each log line.</param>
        /// <param name="cancellationToken">A token to signal cancellation of the log streaming.</param>
        /// <returns>A task representing the asynchronous log streaming operation.</returns>
        Task StreamContainerLogsAsync(string containerId, Func<string, Task> onLogLineReceived, CancellationToken cancellationToken);

        /// <summary>
        /// Lists all Docker containers.
        /// </summary>
        /// <param name="all">True to show all containers (including stopped), false for only running.</param>
        /// <returns>A collection of Docker container DTOs.</returns>
        Task<IEnumerable<DockerContainerDto>> ListContainersAsync(bool all = true);
    }
}
