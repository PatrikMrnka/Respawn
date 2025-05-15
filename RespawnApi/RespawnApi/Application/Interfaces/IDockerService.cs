using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Interfaces
{
    public interface IDockerService
    {
        Task<bool> DeployServerAsync(string serverId, GameType gameType, string dockerImage);
        Task<bool> StartServerAsync(string serverId);
        Task<bool> StopServerAsync(string serverId);

        Task<bool> RestartServerAsync(string serverId);
        Task<bool> RemoveServerAsync(string serverId);
        Task<string> ExecuteCommandAsync(string serverId, string command);
    }
}
