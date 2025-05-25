using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Enums;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace RespawnApi.Hubs
{
    // Only users with Administrator or Spravce roles are authorized to access this hub
    [Authorize(Roles = $"{UserRoles.Administrator},{UserRoles.Spravce}")]
    public class ServerLogHub : Hub
    {
        private readonly IContainerManagementService _containerManagementService;
        private readonly IGameServerRepository _gameServerRepository;
        private readonly ILogger<ServerLogHub> _logger;
        private readonly IHubContext<ServerLogHub> _hubContext;

        // Stores active log streams per connection and per game server
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CancellationTokenSource>>
            _activeLogStreams = new();

        public ServerLogHub(
            IContainerManagementService containerManagementService,
            IGameServerRepository gameServerRepository,
            ILogger<ServerLogHub> logger,
            IHubContext<ServerLogHub> hubContext)
        {
            _containerManagementService = containerManagementService;
            _gameServerRepository = gameServerRepository;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Starts watching logs for a specific game server.
        /// </summary>
        /// <param name="gameServerId">The ID of the game server to watch logs for.</param>
        public async Task WatchLogs(string gameServerId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var connectionId = Context.ConnectionId;
            _logger.LogInformation(
                "User {UserId} (ConnectionId: {ConnectionId}) requests to watch logs for GameServerId: {GameServerId}",
                userId, connectionId, gameServerId);

            if (string.IsNullOrEmpty(gameServerId))
            {
                await Clients.Caller.SendAsync("ReceiveLogLine", "[SYSTEM ERROR] GameServerId cannot be empty.");
                return;
            }

            var server = await _gameServerRepository.GetByIdAsync(Guid.Parse(gameServerId));
            if (server == null || string.IsNullOrEmpty(server.ContainerId))
            {
                _logger.LogWarning(
                    "Server with GameServerId {GameServerId} or its ContainerId was not found for log watching.",
                    gameServerId);
                await Clients.Caller.SendAsync("ReceiveLogLine",
                    $"[SYSTEM ERROR] Server with ID {gameServerId} or its container was not found.");
                return;
            }

            var containerId = server.ContainerId;
            var cts = new CancellationTokenSource();

            // Get or create the dictionary of streams for this connection
            var connectionStreams = _activeLogStreams.GetOrAdd(connectionId,
                _ => new ConcurrentDictionary<string, CancellationTokenSource>());

            // If already watching logs for this server, cancel the old stream
            if (connectionStreams.TryGetValue(gameServerId, out var existingCts))
            {
                _logger.LogWarning(
                    "User {UserId} (ConnectionId: {ConnectionId}) is already watching logs for GameServerId: {GameServerId}. Old stream will be cancelled and replaced.",
                    userId, connectionId, gameServerId);
                existingCts.Cancel();
                existingCts.Dispose();
                connectionStreams.TryRemove(gameServerId, out _); // Remove old stream
            }

            // Register the new log stream
            if (!connectionStreams.TryAdd(gameServerId, cts))
            {
                _logger.LogError(
                    "Failed to register new log stream for {GameServerId} (ConnectionId: {ConnectionId}) after removing old one.",
                    gameServerId, connectionId);
                await Clients.Caller.SendAsync("ReceiveLogLine",
                    "[SYSTEM ERROR] Failed to start log watching (internal stream registration error).");
                cts.Dispose();
                return;
            }

            await Clients.Caller.SendAsync("ReceiveLogLine",
                $"[SYSTEM] Starting log watching for server: {server.Name} (Container: {containerId.Substring(0, Math.Min(12, containerId.Length))})...");

            // Start streaming logs in a background task
            _ = Task.Run(async () =>
            {
                try
                {
                    await _containerManagementService.StreamContainerLogsAsync(containerId, async (logLine) =>
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveLogLine", logLine,
                                cancellationToken: cts.Token);
                        }
                    }, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation(
                        "Log streaming for GameServerId {GameServerId}, ConnectionId {ConnectionId} was cancelled.",
                        gameServerId, connectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error while streaming logs for GameServerId {GameServerId}, ConnectionId {ConnectionId}",
                        gameServerId, connectionId);
                    try
                    {
                        if (!cts.IsCancellationRequested)
                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveLogLine",
                                $"[SYSTEM ERROR] Error while streaming logs: {ex.Message}");
                    }
                    catch (Exception sendEx)
                    {
                        _logger.LogError(sendEx,
                            "Error while sending error message about log streaming to client {ConnectionId}.",
                            connectionId);
                    }
                }
                finally
                {
                    // Remove the stream and dispose the cancellation token
                    if (connectionStreams.TryRemove(KeyValuePair.Create(gameServerId,
                            cts)))
                    {
                        cts.Dispose();
                    }

                    _logger.LogInformation(
                        "Log streaming for GameServerId {GameServerId}, ConnectionId {ConnectionId} has been definitively stopped and CancellationTokenSource disposed (if found).",
                        gameServerId, connectionId);
                }
            }, cts.Token);
        }

        /// <summary>
        /// Stops watching logs for a specific game server.
        /// </summary>
        /// <param name="gameServerId">The ID of the game server to stop watching logs for.</param>
        public Task UnwatchLogs(string gameServerId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var connectionId = Context.ConnectionId;
            _logger.LogInformation(
                "User {UserId} (ConnectionId: {ConnectionId}) requests to stop watching logs for GameServerId: {GameServerId}",
                userId, connectionId, gameServerId);

            if (_activeLogStreams.TryGetValue(connectionId, out var connectionStreams))
            {
                if (connectionStreams.TryRemove(gameServerId, out var cts))
                {
                    _logger.LogInformation(
                        "Cancelling log stream for GameServerId {GameServerId}, ConnectionId {ConnectionId}",
                        gameServerId,
                        connectionId);
                    cts.Cancel();
                    cts.Dispose();
                    _hubContext.Clients.Client(connectionId).SendAsync("ReceiveLogLine",
                        $"[SYSTEM] Log watching for server ID {gameServerId} has been stopped.").ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning(
                        "Attempt to stop log watching for GameServerId {GameServerId}, ConnectionId {ConnectionId}, but stream was not active or already removed.",
                        gameServerId, connectionId);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a client disconnects. Cleans up all active log streams for the connection.
        /// </summary>
        /// <param name="exception">The exception that triggered the disconnect, if any.</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation(
                "Client {ConnectionId} disconnected from ServerLogHub. Cancelling all their active log streams.",
                connectionId);
            if (_activeLogStreams.TryRemove(connectionId, out var connectionStreams))
            {
                foreach (var pair in connectionStreams)
                {
                    _logger.LogInformation(
                        "Cancelling log stream for GameServerId {GameServerId} due to client {ConnectionId} disconnect",
                        pair.Key, connectionId);
                    pair.Value.Cancel();
                    pair.Value.Dispose();
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}