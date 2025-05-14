using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Interfaces
{
    public interface IDockerService
    {
        Task<bool> DeployServerAsync(string serverId, GameType gameType, string dockerImage);
        /// <summary>
        /// Tries to start an existing (stopped) server with the given ID.
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns></returns>
        Task<bool> StartServerAsync(string serverId);
        /// <summary>
        /// Tries to stop a running server with the given ID.
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns></returns>
        Task<bool> StopServerAsync(string serverId);

        Task<bool> RestartServerAsync(string serverId);
        Task<bool> RemoveServerAsync(string serverId);
        /// <summary>
        /// Executes a command inside the server's container.
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<string> ExecuteCommandAsync(string serverId, string command);
    }
}
