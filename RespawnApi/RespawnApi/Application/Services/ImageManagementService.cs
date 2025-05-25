// File: haha/RespawnApi/RespawnApi/Application/Services/ImageManagementService.cs
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.Docker;
using RespawnApi.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RespawnApi.Application.Services
{
    /// <summary>
    /// Service for managing Docker images.
    /// </summary>
    public class ImageManagementService : IImageManagementService
    {
        private readonly DockerClient _client;
        private readonly ILogger<ImageManagementService> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageManagementService"/> class.
        /// </summary>
        public ImageManagementService(IConfiguration configuration, ILogger<ImageManagementService> logger)
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
            _logger.LogInformation("ImageManagementService inicializován s URI: {DockerApiUri}", dockerApiUri);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DockerImageDto>> ListImagesAsync(bool all = false)
        {
            try
            {
                var response = await _client.Images.ListImagesAsync(new ImagesListParameters { All = all });
                return response.Select(i => new DockerImageDto
                {
                    Id = i.ID.Split(':').LastOrDefault()?.Split('@').First().Substring(0, Math.Min(12, i.ID.Split(':').LastOrDefault()?.Split('@').First().Length ?? 0)) ?? i.ID.Substring(0, Math.Min(12, i.ID.Length)),
                    FullId = i.ID,
                    RepoTags = i.RepoTags?.ToList() ?? new List<string>(),
                    RepoDigests = i.RepoDigests?.ToList() ?? new List<string>(),
                    Created = i.Created,
                    Size = i.Size,
                    VirtualSize = i.VirtualSize,
                    Labels = (Dictionary<string, string>)(i.Labels ?? new Dictionary<string, string>()),
                    Containers = (int)i.Containers
                }) ?? Enumerable.Empty<DockerImageDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při výpisu Docker images.");
                return Enumerable.Empty<DockerImageDto>();
            }
        }

        /// <inheritdoc />
        public async Task<(bool Success, string? ErrorMessage)> RemoveImageAsync(string imageId, bool force = false, bool pruneChildren = false)
        {
            // pruneChildren is not directly supported by Docker.DotNet's DeleteImageAsync in a simple way.
            // Docker's 'docker image prune' handles dangling parents, but not explicitly children of a deleted image.
            // If an image has children, 'force' is usually needed.
            _logger.LogInformation("Pokouším se smazat image '{ImageId}' s force={ForceFlag}.", imageId, force);
            try
            {
                var result = await _client.Images.DeleteImageAsync(imageId, new ImageDeleteParameters { Force = force, NoPrune = !pruneChildren }); // NoPrune=true means don't prune parents
                _logger.LogInformation("Image '{ImageId}' smazán. Výsledek: {DeleteResultCount} položek smazáno/uvolněno.", imageId, result?.Count ?? 0);
                return (true, null);
            }
            catch (DockerImageNotFoundException)
            {
                _logger.LogWarning("Image '{ImageId}' nenalezen při pokusu o smazání.", imageId);
                return (false, "Image nebyl nalezen.");
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning(ex, "Konflikt při mazání image '{ImageId}' (pravděpodobně používán kontejnerem nebo má závislé potomky). Force={ForceFlag}", imageId, force);
                return (false, $"Konflikt: Image je používán nebo má závislé potomky. Zkuste nejprve smazat/zastavit kontejnery nebo použít 'force'. Chyba: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Obecná chyba při mazání Docker image '{ImageId}'.", imageId);
                return (false, ex.Message);
            }
        }
    }
}
