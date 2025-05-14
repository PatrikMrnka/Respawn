using RespawnApi.Application.Interfaces;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Services
{
    public class DockerService : IDockerService
    {
        private readonly ILogger<DockerService> _logger;
        private readonly IGameServerRepository _gameServerRepository;

        public DockerService(ILogger<DockerService> logger, IGameServerRepository gameServerRepository)
        {
            _logger = logger;
            _gameServerRepository = gameServerRepository;
        }

        public Task<bool> DeployServerAsync(string serverId, GameType gameType, string dockerImage)
        {
            throw new NotImplementedException();
        }

        public Task<string> ExecuteCommandAsync(string serverId, string command)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveServerAsync(string serverId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RestartServerAsync(string serverId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartServerAsync(string serverId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> StopServerAsync(string serverId)
        {
            throw new NotImplementedException();
        }
    }
}
