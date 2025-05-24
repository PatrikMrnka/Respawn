// File: haha/RespawnApi/RespawnApi/Hubs/ServerLogHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace RespawnApi.Hubs
{
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
    public class ServerLogHub : Hub
    {
        private readonly IDockerService _dockerService;
        private readonly IGameServerRepository _gameServerRepository;
        private readonly ILogger<ServerLogHub> _logger;
        private readonly IHubContext<ServerLogHub> _hubContext; // Injektovaný IHubContext

        // Slovník pro sledování aktivních CancellationTokenSource pro každé spojení a gameServerId
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>> _activeLogStreams = new();

        public ServerLogHub(
            IDockerService dockerService,
            IGameServerRepository gameServerRepository,
            ILogger<ServerLogHub> logger,
            IHubContext<ServerLogHub> hubContext) // Injektovat IHubContext
        {
            _dockerService = dockerService;
            _gameServerRepository = gameServerRepository;
            _logger = logger;
            _hubContext = hubContext; // Přiřadit injektovaný IHubContext
        }

        public async Task WatchLogs(string gameServerId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var connectionId = Context.ConnectionId; // Uložit connectionId pro použití v callbacku
            _logger.LogInformation("Uživatel {UserId} (ConnectionId: {ConnectionId}) žádá o sledování logů pro GameServerId: {GameServerId}", userId, connectionId, gameServerId);

            if (string.IsNullOrEmpty(gameServerId))
            {
                await Clients.Caller.SendAsync("ReceiveLogLine", "[SYSTEM ERROR] GameServerId nemůže být prázdný.");
                return;
            }

            var server = await _gameServerRepository.GetByIdAsync(Guid.Parse(gameServerId));
            if (server == null || string.IsNullOrEmpty(server.ContainerId))
            {
                _logger.LogWarning("Server s GameServerId {GameServerId} nebo jeho ContainerId nebyl nalezen pro sledování logů.", gameServerId);
                await Clients.Caller.SendAsync("ReceiveLogLine", $"[SYSTEM ERROR] Server s ID {gameServerId} nebo jeho kontejner nebyl nalezen.");
                return;
            }

            var containerId = server.ContainerId;
            var cts = new CancellationTokenSource();

            var connectionStreams = _activeLogStreams.GetOrAdd(connectionId, _ => new ConcurrentDictionary<string, CancellationTokenSource>());

            // Pokud již existuje stream pro tento gameServerId a connectionId, zrušíme ho a nahradíme novým
            if (connectionStreams.TryGetValue(gameServerId, out var existingCts))
            {
                _logger.LogWarning("Uživatel {UserId} (ConnectionId: {ConnectionId}) již sleduje logy pro GameServerId: {GameServerId}. Starý stream bude zrušen a nahrazen.", userId, connectionId, gameServerId);
                existingCts.Cancel();
                existingCts.Dispose();
                connectionStreams.TryRemove(gameServerId, out _); // Odstranit starý
            }

            if (!connectionStreams.TryAdd(gameServerId, cts))
            {
                _logger.LogError("Nepodařilo se zaregistrovat nový stream logů pro {GameServerId} (ConnectionId: {ConnectionId}) po případném odstranění starého.", gameServerId, connectionId);
                await Clients.Caller.SendAsync("ReceiveLogLine", "[SYSTEM ERROR] Nepodařilo se spustit sledování logů (interní chyba registrace streamu).");
                cts.Dispose();
                return;
            }

            await Clients.Caller.SendAsync("ReceiveLogLine", $"[SYSTEM] Zahajuji sledování logů pro server: {server.Name} (Kontejner: {containerId.Substring(0, Math.Min(12, containerId.Length))})...");

            _ = Task.Run(async () =>
            {
                try
                {
                    // Použijeme _hubContext pro odeslání zprávy klientovi
                    await _dockerService.StreamContainerLogsAsync(containerId, async (logLine) =>
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            // Použít _hubContext a uložené connectionId
                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveLogLine", logLine, cancellationToken: cts.Token);
                        }
                    }, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Streamování logů pro GameServerId {GameServerId}, ConnectionId {ConnectionId} bylo zrušeno.", gameServerId, connectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Chyba při streamování logů pro GameServerId {GameServerId}, ConnectionId {ConnectionId}", gameServerId, connectionId);
                    try
                    {
                        if (!cts.IsCancellationRequested)
                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveLogLine", $"[SYSTEM ERROR] Chyba při streamování logů: {ex.Message}");
                    }
                    catch (Exception sendEx)
                    {
                        _logger.LogError(sendEx, "Chyba při odesílání chybové zprávy o streamování logů klientovi {ConnectionId}.", connectionId);
                    }
                }
                finally
                {
                    if (connectionStreams.TryRemove(KeyValuePair.Create(gameServerId, cts))) // Odstranit pouze pokud je to stále ten samý CTS
                    {
                        cts.Dispose();
                    }
                    _logger.LogInformation("Streamování logů pro GameServerId {GameServerId}, ConnectionId {ConnectionId} bylo definitivně ukončeno a CancellationTokenSource byl uvolněn (pokud byl nalezen).", gameServerId, connectionId);
                }
            }, cts.Token);
        }

        public Task UnwatchLogs(string gameServerId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Uživatel {UserId} (ConnectionId: {ConnectionId}) žádá o zastavení sledování logů pro GameServerId: {GameServerId}", userId, connectionId, gameServerId);

            if (_activeLogStreams.TryGetValue(connectionId, out var connectionStreams))
            {
                if (connectionStreams.TryRemove(gameServerId, out var cts))
                {
                    _logger.LogInformation("Ruším stream logů pro GameServerId {GameServerId}, ConnectionId {ConnectionId}", gameServerId, connectionId);
                    cts.Cancel();
                    cts.Dispose();
                    // Odeslání potvrzení klientovi
                    _hubContext.Clients.Client(connectionId).SendAsync("ReceiveLogLine", $"[SYSTEM] Sledování logů pro server ID {gameServerId} bylo zastaveno.").ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning("Pokus o zastavení sledování logů pro GameServerId {GameServerId}, ConnectionId {ConnectionId}, ale stream nebyl aktivní nebo již byl odstraněn.", gameServerId, connectionId);
                }
            }
            return Task.CompletedTask;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Klient {ConnectionId} se odpojil z ServerLogHub. Ruším všechny jeho aktivní streamy logů.", connectionId);
            if (_activeLogStreams.TryRemove(connectionId, out var connectionStreams))
            {
                foreach (var pair in connectionStreams)
                {
                    _logger.LogInformation("Ruším stream logů pro GameServerId {GameServerId} z důvodu odpojení klienta {ConnectionId}", pair.Key, connectionId);
                    pair.Value.Cancel();
                    pair.Value.Dispose();
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
