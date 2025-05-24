// Application/Services/GameServerStatusMonitorService.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
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
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10); // Mírně zkrácený interval

        public GameServerStatusMonitorService(ILogger<GameServerStatusMonitorService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GameServerStatusMonitorService spuštěn.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var gameServerRepository = scope.ServiceProvider.GetRequiredService<IGameServerRepository>();
                        var dockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();
                        var gameServerHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameServerHub>>();

                        var serversToMonitor = await gameServerRepository.GetServersByStatusesAsync(
                            new[] { ServerStatus.PendingCreation, ServerStatus.Installing, ServerStatus.Starting, ServerStatus.Online, ServerStatus.Stopping, ServerStatus.Restarting, ServerStatus.Updating });

                        foreach (var server in serversToMonitor)
                        {
                            if (string.IsNullOrEmpty(server.ContainerId) && server.Status != ServerStatus.PendingCreation)
                            {
                                _logger.LogWarning("Server {ServerId} ({ServerName}) nemá ContainerId a není PendingCreation, přeskakuji monitorování.", server.GameServerId, server.Name);
                                if (server.Status != ServerStatus.Error) // Pokud už není Error, nastavíme ho
                                {
                                    server.Status = ServerStatus.Error;
                                    server.StatusDetails = "Chybí ID kontejneru pro monitorování.";
                                    server.LgsmServerStatus = "UNKNOWN";
                                    await gameServerRepository.UpdateAsync(server);
                                    await gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto
                                    {
                                        GameServerId = server.GameServerId,
                                        NewOverallStatus = server.Status,
                                        NewLgsmServerStatus = server.LgsmServerStatus,
                                        StatusDetails = server.StatusDetails
                                    });
                                }
                                continue;
                            }
                            if (server.Status == ServerStatus.PendingCreation) continue; // Tento stav řeší CreateGameServer

                            string? currentContainerStatus = await dockerService.GetContainerStatusAsync(server.ContainerId!);
                            string? currentLgsmStatus = server.LgsmServerStatus; // Současný LGSM status z DB
                            string? newLgsmStatus = null;
                            string? lgsmError = null;

                            ServerStatus newOverallStatus = server.Status;
                            string statusDetails = server.StatusDetails ?? string.Empty;

                            if (currentContainerStatus == "running")
                            {
                                if (server.Status == ServerStatus.Installing || server.Status == ServerStatus.Starting || server.Status == ServerStatus.Online || server.Status == ServerStatus.Unknown)
                                {
                                    (newLgsmStatus, lgsmError) = await dockerService.GetLgsmServerDetailsAsync(server.ContainerId!);
                                    if (!string.IsNullOrEmpty(lgsmError))
                                    {
                                        statusDetails = $"LGSM: {lgsmError}";
                                        // Pokud LGSM vrátí chybu, ale kontejner běží, můžeme nechat celkový stav jako Unknown nebo Error
                                        // newOverallStatus = ServerStatus.Unknown; // Nebo Error, pokud je chyba závažná
                                    }
                                    else if (newLgsmStatus != null)
                                    {
                                        statusDetails = $"LGSM: {newLgsmStatus}";
                                        currentLgsmStatus = newLgsmStatus; // Aktualizujeme LGSM status

                                        // Mapování LGSM stavu na celkový stav, pokud je to vhodné
                                        switch (newLgsmStatus.ToUpperInvariant())
                                        {
                                            case "ONLINE": newOverallStatus = ServerStatus.Online; break;
                                            case "STARTING": newOverallStatus = ServerStatus.Starting; break;
                                            case "INSTALLING": newOverallStatus = ServerStatus.Installing; break;
                                            case "UPDATING": newOverallStatus = ServerStatus.Updating; break;
                                            case "STOPPED": case "OFFLINE": newOverallStatus = ServerStatus.Offline; break; // LGSM je offline, i když kontejner běží
                                            default:
                                                if (server.Status == ServerStatus.Installing) newOverallStatus = ServerStatus.Installing; // Zůstáváme v Installing
                                                else newOverallStatus = ServerStatus.Unknown; // Neznámý LGSM stav
                                                break;
                                        }
                                    }
                                }
                                else
                                { // Kontejner běží, ale celkový stav je např. Stopping - to by nemělo nastat, ale pro jistotu
                                    newOverallStatus = ServerStatus.Online; // Předpokládáme, že pokud kontejner běží a není to specifický stav, je online
                                    currentLgsmStatus = "CHECKING"; // Nastavíme na kontrolu
                                }
                            }
                            else if (currentContainerStatus == "exited" || currentContainerStatus == "not_found" || currentContainerStatus == null)
                            {
                                newOverallStatus = ServerStatus.Offline;
                                currentLgsmStatus = "OFFLINE";
                                statusDetails = $"Kontejner neběží (stav: {currentContainerStatus ?? "neznámý"})";
                            }
                            else if (currentContainerStatus == "error")
                            {
                                newOverallStatus = ServerStatus.Error;
                                currentLgsmStatus = "ERROR";
                                statusDetails = "Chyba při komunikaci s Dockerem pro tento kontejner.";
                            }
                            else // created, restarting, paused
                            {
                                newOverallStatus = server.Status; // Ponecháme stávající celkový stav, dokud se nevyjasní
                                currentLgsmStatus = "UNKNOWN"; // LGSM stav je neznámý, dokud kontejner neběží stabilně
                                statusDetails = $"Stav Docker kontejneru: {currentContainerStatus}";
                            }

                            if (server.Status != newOverallStatus || server.LgsmServerStatus != currentLgsmStatus || server.StatusDetails != statusDetails)
                            {
                                _logger.LogInformation("Změna stavu serveru {ServerId} ({ServerName}): Kontejner={NewOverallStatus} (byl {OldOverallStatus}), LGSM={NewLgsmStatus} (byl {OldLgsmStatus}), Detail='{StatusDetails}'",
                                    server.GameServerId, server.Name, newOverallStatus, server.Status, currentLgsmStatus, server.LgsmServerStatus, statusDetails);
                                server.Status = newOverallStatus;
                                server.LgsmServerStatus = currentLgsmStatus;
                                server.StatusDetails = statusDetails;
                                await gameServerRepository.UpdateAsync(server);

                                await gameServerHubContext.Clients.All.SendAsync("ReceiveGameServerStatusUpdate", new GameServerStatusUpdateDto
                                {
                                    GameServerId = server.GameServerId,
                                    NewOverallStatus = server.Status,
                                    NewLgsmServerStatus = server.LgsmServerStatus,
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
                await Task.Delay(_checkInterval, stoppingToken);
            }
            _logger.LogInformation("GameServerStatusMonitorService zastaven.");
        }
    }
}
