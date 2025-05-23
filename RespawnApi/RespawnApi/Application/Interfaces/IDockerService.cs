using RespawnApi.Domain.Enums;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Application.Interfaces
{
    public interface IDockerService
    {
        Task<(bool Success, string? ContainerId, string? ErrorMessage)> CreateAndStartServerAsync(GameServer serverConfig);
        Task<bool> StartServerAsync(string serverId);
        Task<bool> StopServerAsync(string serverId);
        Task<bool> RestartServerAsync(string serverId);
        Task<bool> RemoveServerAsync(string serverId); // Odstraní kontejner
        Task<string> ExecuteCommandAsync(string serverId, string command);
        Task<string?> GetContainerLogsAsync(string serverId);
        Task<(ServerStatus Status, string DetailsJson)?> GetContainerStatusAsync(string serverId);
    }
}