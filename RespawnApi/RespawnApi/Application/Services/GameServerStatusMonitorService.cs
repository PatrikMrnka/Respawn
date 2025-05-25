using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs;

namespace RespawnApi.Application.Services
{
    /// <summary>
    /// Service for monitoring the status of game servers.
    /// </summary>
    public class GameServerStatusMonitorService : BackgroundService
    {
        private readonly ILogger<GameServerStatusMonitorService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

        public GameServerStatusMonitorService(ILogger<GameServerStatusMonitorService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GameServerStatusMonitorService (Simplified) spuštěn.");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested) // while cancellation is not requested
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var gameServerRepository =
                            scope.ServiceProvider
                                .GetRequiredService<IGameServerRepository>(); // get the game server repository

                        var containerManagementService =
                            scope.ServiceProvider
                                .GetRequiredService<
                                    IContainerManagementService>(); // get the container management service

                        var gameServerHubContext =
                            scope.ServiceProvider
                                .GetRequiredService<IHubContext<GameServerHub>>(); // get the game server hub context

                        var serversToMonitor =
                            await gameServerRepository
                                .GetServersByStatusesAsync( // which statuses to monitor
                                    new[]
                                    {
                                        ServerStatus.PendingCreation,
                                        ServerStatus.Starting,
                                        ServerStatus.Online,
                                        ServerStatus.Stopping,
                                        ServerStatus.Restarting,
                                        ServerStatus.Unknown
                                    });

                        foreach (var server in serversToMonitor) // iterate through each server to monitor
                        {
                            if (stoppingToken.IsCancellationRequested)
                            {
                                break;
                            }

                            string? containerId = server.ContainerId;
                            if (string.IsNullOrEmpty(containerId))
                            {
                                if (server.Status != ServerStatus.PendingCreation &&
                                    server.Status != ServerStatus.Error) // if server is not pending creation or error
                                {
                                    _logger.LogWarning(
                                        "Server {ServerId} ({ServerName}) nemá ContainerId. Nastavuji Error.",
                                        server.GameServerId, server.Name);
                                    server.Status = ServerStatus.Error;
                                    server.StatusDetails = "Chybí ID kontejneru pro monitorování.";
                                    await gameServerRepository.UpdateAsync(server); // update server status
                                    await gameServerHubContext.Clients.All.SendAsync(
                                        "ReceiveGameServerStatusUpdate", // update clients
                                        new GameServerStatusUpdateDto
                                        {
                                            GameServerId = server.GameServerId, NewOverallStatus = server.Status,
                                            StatusDetails = server.StatusDetails
                                        });
                                }

                                continue;
                            }

                            ServerStatus originalOverallStatus = server.Status;
                            string? originalStatusDetails = server.StatusDetails;

                            string? containerDockerStatus =
                                await containerManagementService.GetContainerStatusAsync(containerId);
                            ServerStatus newOverallStatus = server.Status;
                            string statusDetails = $"Docker: {containerDockerStatus ?? "neznámý"}";

                            switch (containerDockerStatus?.ToLowerInvariant())
                            {
                                case "running":
                                    newOverallStatus = ServerStatus.Online;
                                    statusDetails = "Kontejner běží.";
                                    break;
                                case "exited":
                                case "dead":
                                case "not_found":
                                    newOverallStatus = ServerStatus.Offline;
                                    statusDetails =
                                        $"Kontejner neběží (stav Dockeru: {containerDockerStatus ?? "neznámý"})";
                                    break;
                                case "restarting":
                                    newOverallStatus = ServerStatus.Restarting;
                                    statusDetails = "Kontejner se restartuje.";
                                    break;
                                case "paused":
                                    newOverallStatus = ServerStatus.Unknown;
                                    statusDetails = "Kontejner je pozastaven.";
                                    break;
                                case "created":
                                    newOverallStatus = ServerStatus.Offline;
                                    statusDetails = "Kontejner je vytvořen, ale neběží.";
                                    break;
                                case "error":
                                    newOverallStatus = ServerStatus.Error;
                                    statusDetails = "Chyba při získávání stavu kontejneru z Dockeru.";
                                    break;
                                default: // unknown or transitional state
                                    if (server.Status != ServerStatus.Stopping)
                                    {
                                        newOverallStatus = ServerStatus.Unknown;
                                        statusDetails = $"Neznámý/přechodný stav Dockeru: {containerDockerStatus}";
                                    }

                                    break;
                            }

                            if (originalOverallStatus != newOverallStatus ||
                                originalStatusDetails != statusDetails) // if status has changed
                            {
                                _logger.LogInformation(
                                    "Změna stavu serveru {ServerId} ({ServerName}): Nový={NewOverallStatus} (byl {OldOverallStatus}), Detail='{StatusDetails}'",
                                    server.GameServerId, server.Name, newOverallStatus, originalOverallStatus,
                                    statusDetails);
                                server.Status = newOverallStatus;
                                server.StatusDetails = statusDetails;
                                await gameServerRepository.UpdateAsync(server); // update server status in repository

                                await gameServerHubContext.Clients.All.SendAsync(
                                    "ReceiveGameServerStatusUpdate", // notify clients
                                    new GameServerStatusUpdateDto
                                    {
                                        GameServerId = server.GameServerId,
                                        NewOverallStatus = server.Status,
                                        StatusDetails = server.StatusDetails
                                    });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Chyba v GameServerStatusMonitorService.");
                }

                await Task.Delay(_checkInterval, stoppingToken); // Používáme jednotný interval
            }

            _logger.LogInformation("GameServerStatusMonitorService zastaven.");
        }
    }
}