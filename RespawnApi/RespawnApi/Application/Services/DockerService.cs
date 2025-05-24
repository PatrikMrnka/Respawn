// File: haha/RespawnApi/RespawnApi/Application/Services/DockerService.cs
using Docker.DotNet.Models;
using Docker.DotNet;
using RespawnApi.Application.DTOs.DockerAdmin;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using RespawnApi.DataAccess.Interfaces; // Required for IGameServerRepository

namespace RespawnApi.Application.Services
{
    public class DockerService : IDockerService
    {
        private readonly DockerClient _client;
        private readonly ILogger<DockerService> _logger;
        private readonly string _lgsmBaseImage = "gameservermanagers/gameserver";
        // This is a workaround for GetGameIdentifier. Ideally, this logic shouldn't be duplicated
        // or the dependency should be handled differently (e.g., pass GameType directly).
        private IGameServerRepository? _gameServerRepository;


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

        // Setter for IGameServerRepository (workaround for singleton needing scoped service)
        public void SetGameServerRepository(IGameServerRepository gameServerRepository)
        {
            _gameServerRepository = gameServerRepository;
        }


        public async Task<(string? ContainerId, string? ErrorMessage)> CreateContainerAsync(
            GameServer serverDetails, string gameImageTag, string gameIdentifier, string? additionalGsParams)
        {
            var imageName = $"{_lgsmBaseImage}:{gameImageTag}";
            var uniqueSuffix = serverDetails.GameServerId.ToString().Substring(0, 8);
            var containerName = $"lgsm-{gameIdentifier}-{uniqueSuffix}";
            var volumeName = $"lgsm-data-{gameIdentifier}-{uniqueSuffix}";

            _logger.LogInformation("Pokus o vytvoření kontejneru: Name={ContainerName}, Image={ImageName}, Volume={VolumeName}", containerName, imageName, volumeName);
            try
            {
                var volumes = await _client.Volumes.ListAsync();
                if (volumes.Volumes == null || !volumes.Volumes.Any(v => v.Name == volumeName))
                {
                    await _client.Volumes.CreateAsync(new VolumesCreateParameters
                    {
                        Name = volumeName,
                        Labels = new Dictionary<string, string> { { "com.respawn.lgsm.serverid", serverDetails.GameServerId.ToString() } }
                    });
                    _logger.LogInformation("Volume {VolumeName} vytvořen.", volumeName);
                }
                var envVars = new List<string> {
                    "SKIP_UPDATE=TRUE"
                };
                if (!string.IsNullOrWhiteSpace(additionalGsParams)) envVars.Add($"GS_PARAMS={additionalGsParams}");

                var createParams = new CreateContainerParameters
                {
                    Image = imageName,
                    Name = containerName,
                    Env = envVars,
                    Labels = new Dictionary<string, string> {
                        { "com.respawn.gameserver.id", serverDetails.GameServerId.ToString() },
                        { "com.respawn.lgsm.volume", volumeName } // Store volume name for easier removal
                    },
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
        public async Task<bool> StartContainerAsync(string containerId)
        {
            try { return await _client.Containers.StartContainerAsync(containerId, null); }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při spouštění kontejneru {ContainerId}.", containerId); return false; }
        }

        public async Task<bool> StopContainerAsync(string containerId)
        {
            try
            {
                _logger.LogInformation("Provádím Docker stop pro kontejner {ContainerId}.", containerId);
                return await _client.Containers.StopContainerAsync(containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 15 });
            }
            catch (DockerContainerNotFoundException)
            {
                _logger.LogWarning("Kontejner {ContainerId} nenalezen při pokusu o Docker stop.", containerId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při Docker stop pro kontejner {ContainerId}.", containerId);
                return false;
            }
        }
        public async Task<bool> RemoveContainerAsync(string containerId, bool removeAssociatedVolume)
        {
            try
            {
                ContainerInspectResponse? inspect = null;
                try
                {
                    inspect = await _client.Containers.InspectContainerAsync(containerId);
                }
                catch (DockerContainerNotFoundException)
                {
                    _logger.LogWarning("Kontejner {ContainerId} nenalezen při inspekci před smazáním. Pokračuji pokusem o smazání.", containerId);
                }

                await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true, RemoveVolumes = false });
                _logger.LogInformation("Kontejner {ContainerId} smazán.", containerId);

                if (removeAssociatedVolume && inspect != null) // Proceed only if inspect was successful
                {
                    string? volumeNameToRemove = null;
                    // Try to get volume name from label first
                    if (inspect.Config?.Labels?.TryGetValue("com.respawn.lgsm.volume", out var volNameFromLabel) == true && !string.IsNullOrEmpty(volNameFromLabel))
                    {
                        volumeNameToRemove = volNameFromLabel;
                    }
                    // Fallback: try to find a volume from mounts if label method failed or label not present
                    else if (inspect.Mounts?.Any(m => m.Type == "volume" && !string.IsNullOrEmpty(m.Name)) == true)
                    {
                        volumeNameToRemove = inspect.Mounts.FirstOrDefault(m => m.Type == "volume" && !string.IsNullOrEmpty(m.Name))?.Name;
                        _logger.LogInformation("Volume pro {ContainerId} identifikován z mounts: {VolumeName}", containerId, volumeNameToRemove);
                    }


                    if (!string.IsNullOrEmpty(volumeNameToRemove))
                    {
                        try
                        {
                            await _client.Volumes.RemoveAsync(volumeNameToRemove, true);
                            _logger.LogInformation("Asociovaný volume {VolumeName} pro kontejner {ContainerId} smazán.", volumeNameToRemove, containerId);
                        }

                        catch (Exception volEx)
                        {
                            _logger.LogError(volEx, "Chyba při mazání asociovaného volume {VolumeName} pro kontejner {ContainerId}.", volumeNameToRemove, containerId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Nepodařilo se identifikovat asociovaný volume pro smazání pro kontejner {ContainerId}.", containerId);
                    }
                }
                return true;
            }
            catch (DockerContainerNotFoundException)
            {
                _logger.LogWarning("Kontejner {ContainerId} nenalezen při pokusu o smazání.", containerId);
                return true; // Container is already gone
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při mazání kontejneru {ContainerId}.", containerId); return false; }
        }

        private string GetGameIdentifier(Domain.Enums.GameType gameType) // Make sure GameType is from Domain.Enums
        {
            // This method might be problematic if _gameServerRepository is null during early init of singleton
            // For now, direct switch is safer if GameType is passed.
            return gameType switch
            {
                Domain.Enums.GameType.CounterStrike => "csserver",
                Domain.Enums.GameType.TeamFortress2 => "tf2server",
                Domain.Enums.GameType.GarrysMod => "gmodserver",
                _ => $"unknownserver-{Guid.NewGuid().ToString().Substring(0, 4)}"
            };
        }


        public async Task<List<string>> GetContainerLogsAsync(string containerId, DateTime? since, uint lines)
        {
            var logLines = new List<string>();
            try
            {
                var parameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = lines.ToString(),
                    Timestamps = true,
                    Since = since?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", System.Globalization.CultureInfo.InvariantCulture)
                };
                // GetContainerLogsAsync returns a MultiplexedStream
                using var logsStreamMultiplexed = await _client.Containers.GetContainerLogsAsync(containerId, parameters, CancellationToken.None);

                if (logsStreamMultiplexed == null)
                {
                    _logger.LogWarning("Nepodařilo se získat stream logů pro kontejner {ContainerId}.", containerId);
                    return logLines;
                }

                // Demultiplex the stream to get stdout and stderr separately

                // Replace this line:
                // (string stdout, string stderr) = await logsStreamMultiplexed.ReadOutputToEndAsync(CancellationToken.None);

                // With the following implementation:
                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();
                var buffer = new byte[8192];
                var result = new List<(string Type, string Line)>();

                while (true)
                {
                    var readResult = await logsStreamMultiplexed.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
                    if (readResult == 0) break;

                    var output = Encoding.UTF8.GetString(buffer, 0, readResult);
                    var liness = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in liness)
                    {
                        if (line.StartsWith("stdout:"))
                        {
                            stdoutBuilder.AppendLine(line.Substring(7).Trim());
                        }
                        else if (line.StartsWith("stderr:"))
                        {
                            stderrBuilder.AppendLine(line.Substring(7).Trim());
                        }
                    }
                }

                string stdout = stdoutBuilder.ToString();
                string stderr = stderrBuilder.ToString();

                if (!string.IsNullOrEmpty(stdout))
                {
                    logLines.AddRange(stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                }
                if (!string.IsNullOrEmpty(stderr))
                {
                    _logger.LogWarning("Stderr for container {ContainerId}: {StdErrOutput}", containerId, stderr);
                    logLines.AddRange(stderr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                }

                _logger.LogInformation("Načteno {Count} řádků logů pro kontejner {ContainerId}.", logLines.Count, containerId);
            }
            catch (DockerContainerNotFoundException) { _logger.LogWarning("Kontejner {ContainerId} nenalezen při GetContainerLogsAsync.", containerId); logLines.Add("Kontejner nenalezen."); }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při získávání logů kontejneru {ContainerId}.", containerId); logLines.Add($"Chyba při získávání logů: {ex.Message}"); }
            return logLines;
        }

        public async Task StreamContainerLogsAsync(string containerId, Func<string, Task> onLogLineReceived, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Zahajuji streamování logů pro kontejner {ContainerId}", containerId);
            MultiplexedStream? logsStreamMultiplexed = null;
            try
            {
                var parameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Follow = true,
                    Timestamps = true,
                    Tail = "50"
                };

                // Updated to use the correct overload with the 'tty' parameter
                logsStreamMultiplexed = await _client.Containers.GetContainerLogsAsync(containerId, false, parameters, cancellationToken);

                if (logsStreamMultiplexed == null)
                {
                    _logger.LogWarning("Nepodařilo se získat multiplexovaný stream logů pro kontejner {ContainerId} pro streamování.", containerId);
                    await onLogLineReceived($"[SYSTEM] Nepodařilo se připojit k logům kontejneru {containerId}.");
                    return;
                }

                // With the following implementation:
                var buffer = new byte[8192];
                while (!cancellationToken.IsCancellationRequested)
                {
                    var readResult = await logsStreamMultiplexed.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (readResult.Count == 0) break;

                    var output = Encoding.UTF8.GetString(buffer, 0, readResult.Count);
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        await onLogLineReceived(line.TrimEnd('\r', '\n'));
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Streamování logů pro kontejner {ContainerId} bylo zrušeno (po smyčce).", containerId);
                    await onLogLineReceived($"[SYSTEM] Streamování logů pro kontejner {containerId} bylo zrušeno.");
                }
                else
                {
                    _logger.LogInformation("Stream logů pro kontejner {ContainerId} byl přirozeně ukončen.", containerId);
                    await onLogLineReceived($"[SYSTEM] Stream logů pro kontejner {containerId} byl ukončen.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Streamování logů pro kontejner {ContainerId} bylo zrušeno (OperationCanceledException).", containerId);
                await onLogLineReceived($"[SYSTEM] Streamování logů pro kontejner {containerId} bylo zrušeno.");
            }
            catch (DockerContainerNotFoundException)
            {
                _logger.LogWarning("Kontejner {ContainerId} nenalezen při pokusu o streamování logů.", containerId);
                await onLogLineReceived($"[SYSTEM] Kontejner {containerId} nenalezen.");
            }
            catch (IOException ioex) when (ioex.InnerException is SocketException se && (se.SocketErrorCode == SocketError.ConnectionAborted || se.SocketErrorCode == SocketError.OperationAborted))
            {
                _logger.LogInformation(ioex, "Stream logů pro kontejner {ContainerId} byl přerušen (ConnectionAborted/OperationAborted). Pravděpodobně zrušeno.", containerId);
                await onLogLineReceived($"[SYSTEM] Streamování logů pro kontejner {containerId} bylo přerušeno.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při streamování logů kontejneru {ContainerId}", containerId);
                await onLogLineReceived($"[SYSTEM] Chyba při streamování logů: {ex.Message}");
            }
            finally
            {
                logsStreamMultiplexed?.Dispose(); // Ensure stream is disposed
                _logger.LogInformation("Streamování logů pro kontejner {ContainerId} bylo definitivně ukončeno (finally block).", containerId);
            }
        }

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
                    Id = i.ID.Split(':').LastOrDefault()?.Substring(0, 12) ?? i.ID.Substring(0, Math.Min(12, i.ID.Length)),
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
