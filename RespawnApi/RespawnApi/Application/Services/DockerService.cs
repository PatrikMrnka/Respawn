using Docker.DotNet.Models;
using Docker.DotNet;
using RespawnApi.Application.DTOs.DockerAdmin;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using System.Text;
using System.Reflection.Emit;

namespace RespawnApi.Application.Services
{
    public class DockerService : IDockerService
    {
        private readonly DockerClient _client;
        private readonly ILogger<DockerService> _logger;
        private readonly string _lgsmBaseImage = "gameservermanagers/gameserver";

        public DockerService(IConfiguration configuration, ILogger<DockerService> logger)
        {
            _logger = logger;
            var dockerApiUri = configuration["DockerSettings:DockerApiUri"];
            if (string.IsNullOrEmpty(dockerApiUri))
            {
                _logger.LogError("Docker API URI není nakonfigurováno v DockerSettings:DockerApiUri.");
                throw new InvalidOperationException("Docker API URI není nakonfigurováno.");
            }
            _client = new DockerClientConfiguration(new Uri(dockerApiUri)).CreateClient();
            _logger.LogInformation("DockerService inicializován s URI: {DockerApiUri}", dockerApiUri);
        }

        // --- Metody pro GameServer (ponechány a mírně upraveny) ---
        public async Task<(string? ContainerId, string? ErrorMessage)> CreateContainerAsync(
            GameServer serverDetails, string gameImageTag, string gameIdentifier, string? additionalGsParams)
        {
            var imageName = $"{_lgsmBaseImage}:{gameImageTag}";
            var containerName = $"lgsm-{gameIdentifier}-{serverDetails.GameServerId.ToString().Substring(0, 8)}";
            var volumeName = $"lgsm-data-{gameIdentifier}-{serverDetails.GameServerId.ToString().Substring(0, 8)}";
            _logger.LogInformation("Pokus o vytvoření kontejneru: Name={ContainerName}, Image={ImageName}, Volume={VolumeName}", containerName, imageName, volumeName);
            try
            {
                var volumes = await _client.Volumes.ListAsync();
                if (volumes.Volumes == null || !volumes.Volumes.Any(v => v.Name == volumeName))
                {
                    await _client.Volumes.CreateAsync(new VolumesCreateParameters { Name = volumeName });
                    _logger.LogInformation("Volume {VolumeName} vytvořen.", volumeName);
                }
                var envVars = new List<string> {
                    $"GAMESERVER_NAME={serverDetails.Name}", $"LGSM_SERVERNAME={containerName}",
                    $"LGSM_GITHUBUSER={Environment.GetEnvironmentVariable("LGSM_GITHUBUSER")}",
                    $"LGSM_GITHUBTOKEN={Environment.GetEnvironmentVariable("LGSM_GITHUBTOKEN")}",
                    "SKIP_UPDATE=TRUE"
                };
                if (!string.IsNullOrWhiteSpace(additionalGsParams)) envVars.Add($"GS_PARAMS={additionalGsParams}");
                var createParams = new CreateContainerParameters
                {
                    Image = imageName,
                    Name = containerName,
                    Env = envVars,
                    HostConfig = new HostConfig
                    {
                        RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped },
                        NetworkMode = "host",
                        Binds = new List<string> { $"{volumeName}:/data" }
                    },
                };
                var response = await _client.Containers.CreateContainerAsync(createParams);
                _logger.LogInformation("Kontejner {ContainerName} (ID: {ContainerId}) vytvořen.", containerName, response.ID);
                if (await _client.Containers.StartContainerAsync(response.ID, null))
                {
                    _logger.LogInformation("Kontejner {ContainerId} úspěšně spuštěn.", response.ID);
                    return (response.ID, null);
                }
                _logger.LogError("Nepodařilo se spustit kontejner {ContainerId}.", response.ID);
                return (null, "Nepodařilo se spustit kontejner po vytvoření.");
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při vytváření kontejneru {ContainerName}", containerName); return (null, $"Chyba: {ex.Message}"); }
        }
        public async Task<string?> GetContainerStatusAsync(string containerId)
        {
            try { var inspect = await _client.Containers.InspectContainerAsync(containerId); return inspect?.State?.Status; }
            catch (DockerContainerNotFoundException) { _logger.LogWarning("Kontejner {ContainerId} nenalezen.", containerId); return "not_found"; }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při získávání stavu kontejneru {ContainerId}.", containerId); return "error"; }
        }
        public async Task<(string? LgsmStatus, string? ErrorMessage)> GetLgsmServerDetailsAsync(string containerId)
        {
            var command = new[] { "/bin/bash", "-c", "./linuxgsm.sh details" };
            try
            {
                var execParams = new ContainerExecCreateParameters { AttachStdout = true, AttachStderr = true, Cmd = command, Tty = false, WorkingDir = "/home/linuxgsm/serverfiles" };
                var execId = (await _client.Exec.ExecCreateContainerAsync(containerId, execParams)).ID;
                var stream = await _client.Exec.StartAndAttachContainerExecAsync(execId, false, CancellationToken.None);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                var (stdout, stderr) = await stream.ReadOutputToEndAsync(cts.Token);
                if (!string.IsNullOrWhiteSpace(stderr) && !stderr.Contains("Loading") && !stderr.Contains("Applying") && !stderr.Contains("Checking")) _logger.LogWarning("LGSM details stderr pro {ContainerId}: {Stderr}", containerId, stderr);
                var statusLine = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Trim().StartsWith("Server Status:") || l.Trim().StartsWith("Status:"));
                if (statusLine != null) return (statusLine.Replace("Server Status:", "").Replace("Status:", "").Trim().ToUpperInvariant(), null);
                _logger.LogWarning("Nepodařilo se parsovat LGSM status z '{Stdout}' pro {ContainerId}", stdout, containerId);
                return ("UNKNOWN", "Nepodařilo se parsovat stav.");
            }
            catch (OperationCanceledException) { _logger.LogWarning("Timeout při LGSM details pro {ContainerId}.", containerId); return (null, "Timeout."); }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při LGSM details pro {ContainerId}.", containerId); return (null, ex.Message); }
        }
        public async Task<bool> StopContainerAsync(string containerId)
        {
            try { /* ... logika s LGSM stop a pak Docker stop ... */ return await _client.Containers.StopContainerAsync(containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 10 }); }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při zastavování kontejneru {ContainerId}.", containerId); return false; }
        }
        public async Task<bool> StartContainerAsync(string containerId)
        {
            try { return await _client.Containers.StartContainerAsync(containerId, null); }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při spouštění kontejneru {ContainerId}.", containerId); return false; }
        }
        public async Task<bool> RemoveContainerAsync(string containerId, bool removeAssociatedVolume)
        {
            try
            {
                var inspect = await _client.Containers.InspectContainerAsync(containerId);
                await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true, RemoveVolumes = false }); // Volumes mažeme explicitně, pokud je potřeba
                if (removeAssociatedVolume && inspect?.Config?.Labels?.TryGetValue("com.respawn.lgsm.volume", out var volumeName) == true && !string.IsNullOrEmpty(volumeName))
                {
                    try { await _client.Volumes.RemoveAsync(volumeName, true); _logger.LogInformation("Volume {VolumeName} smazán.", volumeName); }
                    catch (Exception volEx) { _logger.LogError(volEx, "Chyba při mazání volume {VolumeName}.", volumeName); }
                }
                return true;
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při mazání kontejneru {ContainerId}.", containerId); return false; }
        }
        public async Task<string> GetContainerLogsAsync(string containerId, uint tail = 200)
        {
            try
            {
                var logsStream = await _client.Containers.GetContainerLogsAsync(containerId, new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Tail = tail.ToString(), Timestamps = true }, CancellationToken.None);
                if (logsStream == null) return "Stream logů je null.";
                using var reader = new StreamReader(logsStream, Encoding.UTF8, true, 1024, true);
                // Zjednodušené čtení, pro produkci je nutné demultiplexování!
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při získávání logů kontejneru {ContainerId}.", containerId); return $"Chyba: {ex.Message}"; }
        }

        // --- Nové metody pro Docker Admin ---
        public async Task<IEnumerable<DockerVolumeDto>> ListVolumesAsync()
        {
            try
            {
                var response = await _client.Volumes.ListAsync();
                return response.Volumes?.Select(v => new DockerVolumeDto
                {
                    Name = v.Name,
                    Driver = v.Driver,
                    CreatedAt = DateTime.TryParse(v.CreatedAt, out var dt) ? dt : default,
                    Labels = v.Labels is Dictionary<string, string> dict ? dict : v.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                    // SizeBytes by vyžadovalo 'docker system df -v' nebo parsování z 'docker volume inspect' - složitější
                }) ?? Enumerable.Empty<DockerVolumeDto>();
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při výpisu Docker volumes."); return Enumerable.Empty<DockerVolumeDto>(); }
        }

        public async Task<bool> RemoveVolumeAsync(string volumeName, bool force = false)
        {
            try { await _client.Volumes.RemoveAsync(volumeName, force); return true; }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při mazání Docker volume {VolumeName}.", volumeName); return false; }
        }

        public async Task<IEnumerable<DockerContainerDto>> ListContainersAsync(bool all = true)
        {
            try
            {
                var response = await _client.Containers.ListContainersAsync(new ContainersListParameters { All = all });
                return response.Select(c => new DockerContainerDto
                {
                    Id = c.ID,
                    Names = c.Names?.ToList() ?? new List<string>(),
                    Image = c.Image,
                    ImageId = c.ImageID,
                    Command = c.Command,
                    Created = c.Created,
                    Ports = c.Ports?.Select(p => new DockerContainerPortDto { PrivatePort = p.PrivatePort, PublicPort = p.PublicPort, Type = p.Type, IP = p.IP }).ToList() ?? new List<DockerContainerPortDto>(),
                    State = c.State,
                    Status = c.Status,
                    Labels = c.Labels is Dictionary<string, string> dict ? dict : c.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>()
                }) ?? Enumerable.Empty<DockerContainerDto>();
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při výpisu Docker kontejnerů."); return Enumerable.Empty<DockerContainerDto>(); }
        }

        public async Task<IEnumerable<DockerImageDto>> ListImagesAsync(bool all = false)
        {
            try
            {
                var response = await _client.Images.ListImagesAsync(new ImagesListParameters { All = all });
                return response.Select(i => new DockerImageDto
                {
                    Id = i.ID.Split(':').Last().Substring(0, 12), // Krátké ID
                    FullId = i.ID,
                    RepoTags = i.RepoTags?.ToList() ?? new List<string>(),
                    RepoDigests = i.RepoDigests?.ToList() ?? new List<string>(),
                    Created = i.Created,
                    Size = i.Size,
                    VirtualSize = i.VirtualSize,
                    Labels = i.Labels is Dictionary<string, string> dict ? dict : i.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                    Containers = (int)i.Containers
                }) ?? Enumerable.Empty<DockerImageDto>();
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při výpisu Docker images."); return Enumerable.Empty<DockerImageDto>(); }
        }

        public async Task<(bool Success, string? ErrorMessage)> RemoveImageAsync(string imageId, bool force = false, bool pruneChildren = false)
        {
            try
            {
                await _client.Images.DeleteImageAsync(imageId, new ImageDeleteParameters { Force = force });
                return (true, null);
            }
            catch (DockerImageNotFoundException) { return (false, "Image nebyl nalezen."); }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning(ex, "Konflikt při mazání image {ImageId} (pravděpodobně používán kontejnerem).", imageId);
                return (false, $"Konflikt: Image je pravděpodobně používán kontejnerem. Zkuste nejprve smazat/zastavit kontejnery nebo použít 'force'. Chyba: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při mazání Docker image {ImageId}.", imageId);
                return (false, ex.Message);
            }
        }
    }
}
