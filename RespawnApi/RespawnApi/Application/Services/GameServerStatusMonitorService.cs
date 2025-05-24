// Application/Services/GameServerStatusMonitorService.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using RespawnApi.Hubs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RespawnApi.Application.Services
{
    public class GameServerStatusMonitorService : BackgroundService
    {
        private readonly ILogger<GameServerStatusMonitorService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);

        public GameServerStatusMonitorService(ILogger<GameServerStatusMonitorService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GameServerStatusMonitorService (Simplified) spuštěn.");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var gameServerRepository = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                        var dockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();
                        var gameServerHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();

                        // Monitorujeme servery, které nejsou definitivně Offline nebo v Chybě (bez kontejneru)
                        var serversToMonitor = await gameServerRepository.GetServersByStatusesAsync(
                            new[] {
                                ServerStatus.PendingCreation,
                                ServerStatus.Starting, ServerStatus.Online,
                                ServerStatus.Stopping, ServerStatus.Restarting,ServerStatus.Unknown
                            });

                        foreach (var server in serversToMonitor)
                        {
                            if (stoppingToken.IsCancellationRequested) break;

                            string? containerId = server.ContainerId;
                            if (string.IsNullOrEmpty(containerId))
                            {
                                if (server.Status != ServerStatus.PendingCreation && server.Status != ServerStatus.Error)
                                {
                                    _logger.LogWarning("Server {ServerId} ({ServerName}) nemá ContainerId. Nastavuji Error.", server.GameServerId, server.Name);
                                    server.Status = ServerStatus.Error;
                                    server.StatusDetails = "Chybí ID kontejneru pro monitorování.";
                                    await gameServerRepository.UpdateAsync(server);
                                    await gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto { GameServerId = server.GameServerId, NewOverallStatus = server.Status, StatusDetails = server.StatusDetails });
                                }
                                continue;
                            }

                            ServerStatus originalOverallStatus = server.Status;
                            string? originalStatusDetails = server.StatusDetails;

                            string? containerDockerStatus = await dockerService.GetContainerStatusAsync(containerId);
                            ServerStatus newOverallStatus = server.Status;
                            string statusDetails = $"Docker: {containerDockerStatus ?? "neznámý"}";

                            switch (containerDockerStatus?.ToLowerInvariant())
                            {
                                case "running":
                                    // Pokud kontejner běží, server je považován za Online.
                                    // Stavy Installing/Starting jsou přechodné a měly by se vyřešit na Online, jakmile kontejner běží.
                                    newOverallStatus = ServerStatus.Online;
                                    statusDetails = "Kontejner běží.";
                                    break;
                                case "exited":
                                case "dead":
                                case "not_found":
                                    newOverallStatus = ServerStatus.Offline;
                                    statusDetails = $"Kontejner neběží (stav Dockeru: {containerDockerStatus ?? "neznámý"})";
                                    break;
                                case "restarting":
                                    newOverallStatus = ServerStatus.Restarting; // Kontejner se restartuje
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
                                case "error": // Pokud DockerService vrátí "error"
                                    newOverallStatus = ServerStatus.Error;
                                    statusDetails = "Chyba při získávání stavu kontejneru z Dockeru.";
                                    break;
                                default: // Ostatní stavy Dockeru
                                    if (server.Status != ServerStatus.Stopping )
                                    {
                                        newOverallStatus = ServerStatus.Unknown;
                                        statusDetails = $"Neznámý/přechodný stav Dockeru: {containerDockerStatus}";
                                    }
                                    break;
                            }

                            if (originalOverallStatus != newOverallStatus || originalStatusDetails != statusDetails)
                            {
                                _logger.LogInformation("Změna stavu serveru {ServerId} ({ServerName}): Nový={NewOverallStatus} (byl {OldOverallStatus}), Detail='{StatusDetails}'",
                                    server.GameServerId, server.Name, newOverallStatus, originalOverallStatus, statusDetails);
                                server.Status = newOverallStatus;
                                server.StatusDetails = statusDetails;
                                // server.LgsmServerStatus = null; // LgsmServerStatus již neexistuje v entitě
                                await gameServerRepository.UpdateAsync(server);

                                await gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto
                                {
                                    GameServerId = server.GameServerId,
                                    NewOverallStatus = server.Status,
                                    // NewLgsmServerStatus = null, // Odstraněno
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
