using RespawnApi.Application.DTOs.Docker;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages Docker images.
    /// </summary>
    public interface IImageManagementService
    {
        /// <summary>
        /// Lists all Docker images.
        /// </summary>
        /// <param name="all">True to show all images (including intermediate layers), false for only top-level images.</param>
        /// <returns>A collection of Docker image DTOs.</returns>
        Task<IEnumerable<DockerImageDto>> ListImagesAsync(bool all = false);

        /// <summary>
        /// Removes a Docker image.
        /// </summary>
        /// <param name="imageId">The ID or tag of the image to remove.</param>
        /// <param name="force">True to force removal even if the image is used by a container.</param>
        /// <param name="pruneChildren">True to prune parent layers if they are no longer used (not directly supported by Docker.DotNet DeleteImageAsync, more of a system prune concept).</param>
        /// <returns>A tuple indicating success and an optional error message.</returns>
        Task<(bool Success, string? ErrorMessage)> RemoveImageAsync(string imageId, bool force = false, bool pruneChildren = false);
    }
}