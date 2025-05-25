using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.GameServer;

namespace RespawnApi.Hubs
{
    /// <summary>
    /// SignalR hub for broadcasting game server updates, status changes, and removals to connected clients.
    /// </summary>
    public class GameServerHub : Hub
    {
        /// <summary>
        /// Broadcasts an update about a game server to all connected clients.
        /// </summary>
        /// <param name="server">The updated game server information.</param>
        public async Task BroadcastGameServerUpdate(GameServerDto server)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveGameServerUpdate", server);
            }
        }

        /// <summary>
        /// Broadcasts a status update for a game server to all connected clients.
        /// </summary>
        /// <param name="statusUpdate">The status update information for the game server.</param>
        public async Task BroadcastGameServerStatus(GameServerStatusUpdateDto statusUpdate)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveGameServerStatusUpdate", statusUpdate);
            }
        }

        /// <summary>
        /// Broadcasts the removal of a game server to all connected clients.
        /// </summary>
        /// <param name="gameServerId">The unique identifier of the removed game server.</param>
        public async Task BroadcastGameServerRemoval(Guid gameServerId)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveGameServerRemoval", gameServerId);
            }
        }
    }
}