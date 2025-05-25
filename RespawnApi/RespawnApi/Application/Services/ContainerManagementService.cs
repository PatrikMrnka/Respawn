using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.Docker;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums; // Potřebné pro GameType
using RespawnApi.DataAccess.Interfaces; // Potřebné pro IGameServerRepository
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RespawnApi.Application.DTOs.Docker;

namespace RespawnApi.Application.Services
{
    /// <summary>
    /// Service for managing Docker containers.
    /// </summary>
    public class ContainerManagementService : IContainerManagementService
    {
        private readonly DockerClient _client;
        private readonly ILogger<ContainerManagementService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _lgsmBaseImage;
        private readonly IVolumeManagementService _volumeManagementService; // Potřebné pro smazání volume
        private readonly IGameServerRepository _gameServerRepository; // Potřebné pro GetGameIdentifier

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerManagementService"/> class.
        /// </summary>
        public ContainerManagementService(
            IConfiguration configuration,
            ILogger<ContainerManagementService> logger,
            IVolumeManagementService volumeManagementService, // Injektovat IVolumeManagementService
            IGameServerRepository gameServerRepository) // Injektovat IGameServerRepository
        {
            _logger = logger;
            _configuration = configuration;
            _volumeManagementService = volumeManagementService;
            _gameServerRepository = gameServerRepository;

            var dockerApiUri = _configuration["DockerSettings:DockerApiUri"];
            if (string.IsNullOrEmpty(dockerApiUri))
            {
                _logger.LogError("Docker API URI není nakonfigurováno v DockerSettings:DockerApiUri.");
                throw new InvalidOperationException("Docker API URI není nakonfigurováno.");
            }
            _client = new DockerClientConfiguration(new Uri(dockerApiUri)).CreateClient();
            _lgsmBaseImage = _configuration["DockerSettings:LgsmBaseImage"] ?? "gameservermanagers/gameserver";
            _logger.LogInformation("ContainerManagementService inicializován s URI: {DockerApiUri} a base image: {LgsmBaseImage}", dockerApiUri, _lgsmBaseImage);
        }

        /// <inheritdoc />
        public async Task<(string? ContainerId, string? ErrorMessage)> CreateContainerAsync(
            GameServer serverDetails, string gameImageTag, string gameIdentifier, string? additionalGsParams)
        {
            var imageName = $"{_lgsmBaseImage}:{gameImageTag}";
            var uniqueSuffix = serverDetails.GameServerId.ToString("N").Substring(0, 8); // Použít "N" formát pro Guid bez pomlček
            var containerName = $"lgsm-{gameIdentifier}-{uniqueSuffix}";
            var volumeName = $"lgsm-data-{gameIdentifier}-{uniqueSuffix}";

            _logger.LogInformation("Pokus o vytvoření kontejneru: Name={ContainerName}, Image={ImageName}, Volume={VolumeName} pro GameServerId={GameServerId}",
                containerName, imageName, volumeName, serverDetails.GameServerId);
            try
            {
                // Zkontrolovat existenci volume a případně vytvořit
                var volumes = await _volumeManagementService.ListVolumesAsync(); // Použít IVolumeManagementService
                if (volumes == null || !volumes.Any(v => v.Name == volumeName))
                {
                    // Vytvoření volume by mělo být součástí IVolumeManagementService, pokud chceme striktní SRP,
                    // ale pro jednoduchost to zde ponecháme, nebo předpokládáme, že IVolumeManagementService má CreateVolume metodu.
                    // Prozatím přímé volání Docker API pro volume creation:
                    await _client.Volumes.CreateAsync(new VolumesCreateParameters
                    {
                        Name = volumeName,
                        Labels = new Dictionary<string, string> {
                            { "com.respawn.lgsm.serverid", serverDetails.GameServerId.ToString() },
                            { "com.respawn.gameserver.name", serverDetails.Name }
                        }
                    });
                    _logger.LogInformation("Volume {VolumeName} vytvořen pro kontejner {ContainerName}.", volumeName, containerName);
                }

                var envVars = new List<string> { "SKIP_UPDATE=TRUE" };
                if (!string.IsNullOrWhiteSpace(additionalGsParams)) envVars.Add($"GS_PARAMS={additionalGsParams}");

                var createParams = new CreateContainerParameters
                {
                    Image = imageName,
                    Name = containerName,
                    Env = envVars,
                    Labels = new Dictionary<string, string> {
                        { "com.respawn.gameserver.id", serverDetails.GameServerId.ToString() },
                        { "com.respawn.lgsm.volume", volumeName } // Uložení názvu volume do labelu kontejneru
                    },
                    HostConfig = new HostConfig
                    {
                        RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped },
                        NetworkMode = "host", // Pro jednoduchost, zvažte bridge network pro lepší izolaci
                        Binds = new List<string> { $"{volumeName}:/data" }
                    },
                };
                var response = await _client.Containers.CreateContainerAsync(createParams);
                _logger.LogInformation("Kontejner {ContainerName} (ID: {ContainerId}) vytvořen.", containerName, response.ID);

                if (await StartContainerAsync(response.ID)) // Použít metodu této služby
                {
                    return (response.ID, null);
                }
                _logger.LogError("Nepodařilo se spustit kontejner {ContainerId} po vytvoření.", response.ID);
                return (null, "Nepodařilo se spustit kontejner po vytvoření.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při vytváření kontejneru {ContainerName}", containerName);
                return (null, $"Chyba při vytváření kontejneru: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetContainerStatusAsync(string containerId)
        {
            try
            {
                var inspect = await _client.Containers.InspectContainerAsync(containerId);
                return inspect?.State?.Status;
            }
            catch (DockerContainerNotFoundException)
            {
                _logger.LogWarning("Kontejner {ContainerId} nenalezen při GetContainerStatusAsync.", containerId);
                return "not_found";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při získávání stavu kontejneru {ContainerId}.", containerId);
                return "error";
            }
        }

        /// <inheritdoc />
        public async Task<bool> StartContainerAsync(string containerId)
        {
            try
            {
                _logger.LogInformation("Pokouším se spustit kontejner {ContainerId}", containerId);
                var result = await _client.Containers.StartContainerAsync(containerId, null);
                if (result) _logger.LogInformation("Kontejner {ContainerId} úspěšně spuštěn.", containerId);
                else _logger.LogWarning("Příkaz ke spuštění kontejneru {ContainerId} nevrátil úspěch (výsledek false).", containerId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při spouštění kontejneru {ContainerId}.", containerId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> StopContainerAsync(string containerId)
        {
            try
            {
                _logger.LogInformation("Pokouším se zastavit kontejner {ContainerId}.", containerId);
                var result = await _client.Containers.StopContainerAsync(containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 15 });
                if (result) _logger.LogInformation("Kontejner {ContainerId} úspěšně zastaven.", containerId);
                else _logger.LogWarning("Příkaz k zastavení kontejneru {ContainerId} nevrátil úspěch (výsledek false).", containerId);
                return result;
            }
            catch (DockerContainerNotFoundException)
            {
                _logger.LogWarning("Kontejner {ContainerId} nenalezen při pokusu o zastavení.", containerId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při zastavování kontejneru {ContainerId}.", containerId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RemoveContainerAsync(string containerId, bool removeAssociatedVolume)
        {
            _logger.LogInformation("Požadavek na smazání kontejneru {ContainerId}. Smazat asociovaný volume: {RemoveVolumeFlag}", containerId, removeAssociatedVolume);
            ContainerInspectResponse? inspect = null;
            try
            {
                inspect = await _client.Containers.InspectContainerAsync(containerId);
            }
            catch (DockerContainerNotFoundException) { _logger.LogWarning("Kontejner {ContainerId} nenalezen při inspekci před smazáním.", containerId); }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při inspekci kontejneru {ContainerId} před smazáním.", containerId); }

            bool containerRemovedSuccessfully = false;
            try
            {
                await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true, RemoveVolumes = false });
                _logger.LogInformation("Kontejner {ContainerId} úspěšně smazán.", containerId);
                containerRemovedSuccessfully = true;
            }
            catch (DockerContainerNotFoundException)
            { _logger.LogWarning("Kontejner {ContainerId} nenalezen při pokusu o smazání (již byl smazán?).", containerId); containerRemovedSuccessfully = true; }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při mazání kontejneru {ContainerId}.", containerId); }

            if (removeAssociatedVolume && inspect?.Config?.Labels?.TryGetValue("com.respawn.lgsm.volume", out var volumeName) == true && !string.IsNullOrEmpty(volumeName))
            {
                _logger.LogInformation("Identifikován asociovaný volume '{VolumeName}' pro kontejner {ContainerId} z labelu.", volumeName, containerId);
                bool volumeRemoved = await _volumeManagementService.RemoveVolumeAsync(volumeName, true); // Použít IVolumeManagementService
                if (volumeRemoved) _logger.LogInformation("Asociovaný volume '{VolumeName}' úspěšně smazán.", volumeName);
                else _logger.LogWarning("Nepodařilo se smazat asociovaný volume '{VolumeName}'.", volumeName);
            }
            else if (removeAssociatedVolume)
            {
                _logger.LogWarning("Nepodařilo se identifikovat asociovaný volume pro kontejner {ContainerId} z labelu 'com.respawn.lgsm.volume' nebo inspekce selhala.", containerId);
            }
            return containerRemovedSuccessfully;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetContainerLogsAsync(string containerId, DateTime? since = null, uint lines = 200)
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
                    Since = since?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture)
                };
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
            catch (DockerContainerNotFoundException) { logLines.Add("Kontejner nenalezen."); }
            catch (Exception ex) { logLines.Add($"Chyba při získávání logů: {ex.Message}"); }
            return logLines;
        }

        /// <inheritdoc />
        public async Task StreamContainerLogsAsync(string containerId, Func<string, Task> onLogLineReceived, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Zahajuji streamování logů pro kontejner {ContainerId}", containerId);
            MultiplexedStream? logsStreamMultiplexed = null;
            try
            {
                var parameters = new ContainerLogsParameters
                { ShowStdout = true, ShowStderr = true, Follow = true, Timestamps = true, Tail = "50" };
                logsStreamMultiplexed = await _client.Containers.GetContainerLogsAsync(containerId, false, parameters, cancellationToken);

                if (logsStreamMultiplexed == null)
                { await onLogLineReceived($"[SYSTEM] Nepodařilo se připojit k logům kontejneru {containerId}."); return; }

                var buffer = new byte[8192];
                var stringBuilder = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await logsStreamMultiplexed.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (cancellationToken.IsCancellationRequested || result.EOF) break;

                    if (result.Count > 0)
                    {
                        string chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        stringBuilder.Append(chunk);
                        string allAvailable = stringBuilder.ToString();
                        int lastNewline;
                        while ((lastNewline = allAvailable.IndexOf('\n')) >= 0)
                        {
                            string line = allAvailable.Substring(0, lastNewline).TrimEnd('\r');
                            if (!string.IsNullOrEmpty(line)) await onLogLineReceived(line);
                            allAvailable = allAvailable.Substring(lastNewline + 1);
                        }
                        stringBuilder.Clear().Append(allAvailable);
                    }
                }
                if (stringBuilder.Length > 0 && !cancellationToken.IsCancellationRequested)
                {
                    await onLogLineReceived(stringBuilder.ToString().TrimEnd('\r', '\n'));
                }
                await onLogLineReceived($"[SYSTEM] Streamování logů pro kontejner {containerId} bylo {(cancellationToken.IsCancellationRequested ? "zrušeno" : "ukončeno")}.");
            }
            catch (OperationCanceledException) { await onLogLineReceived($"[SYSTEM] Streamování logů pro kontejner {containerId} bylo zrušeno."); }
            catch (DockerContainerNotFoundException) { await onLogLineReceived($"[SYSTEM] Kontejner {containerId} nenalezen."); }
            catch (IOException ioex) when (ioex.InnerException is SocketException se && (se.SocketErrorCode == SocketError.ConnectionAborted || se.SocketErrorCode == SocketError.OperationAborted || se.SocketErrorCode == SocketError.Interrupted))
            { await onLogLineReceived($"[SYSTEM] Streamování logů pro kontejner {containerId} bylo přerušeno."); }
            catch (Exception ex) { await onLogLineReceived($"[SYSTEM] Chyba při streamování logů: {ex.Message}"); }
            finally { logsStreamMultiplexed?.Dispose(); _logger.LogInformation("Streamování logů pro kontejner {ContainerId} bylo definitivně ukončeno.", containerId); }
        }

        /// <inheritdoc />
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
                    Labels = (Dictionary<string, string>)(c.Labels ?? new Dictionary<string, string>())
                }) ?? Enumerable.Empty<DockerContainerDto>();
            }
            catch (Exception ex) { _logger.LogError(ex, "Chyba při výpisu Docker kontejnerů."); return Enumerable.Empty<DockerContainerDto>(); }
        }
    }
}
