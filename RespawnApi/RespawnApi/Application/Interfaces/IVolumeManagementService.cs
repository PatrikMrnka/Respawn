using RespawnApi.Application.DTOs.Docker;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages Docker volumes.
    /// </summary>
    public interface IVolumeManagementService
    {
        /// <summary>
        /// Lists all Docker volumes.
        /// </summary>
        /// <returns>A collection of Docker volume DTOs.</returns>
        Task<IEnumerable<DockerVolumeDto>> ListVolumesAsync();

        /// <summary>
        /// Removes a Docker volume.
        /// </summary>
        /// <param name="volumeName">The name of the volume to remove.</param>
        /// <param name="force">True to force removal even if the volume is in use (not always effective).</param>
        /// <returns>True if the volume was removed successfully, otherwise false.</returns>
        Task<bool> RemoveVolumeAsync(string volumeName, bool force = false);
    }
}