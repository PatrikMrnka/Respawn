// Hubs/GameServerHub.cs
using Microsoft.AspNetCore.SignalR;
using RespawnApi.Application.DTOs.GameServer;
using System.Threading.Tasks;

namespace RespawnApi.Hubs
{
    public class GameServerHub : Hub
    {
        // Metoda, kterou bude server volat k odeslání aktualizace stavu herního serveru
        public async Task BroadcastGameServerUpdate(GameServerDto server)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveGameServerUpdate", server);
            }
        }

        public async Task BroadcastGameServerStatus(GameServerStatusUpdateDto statusUpdate)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveGameServerStatusUpdate", statusUpdate);
            }
        }

        public async Task BroadcastGameServerRemoval(Guid gameServerId)
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("ReceiveGameServerRemoval", gameServerId);
            }
        }
    }
}