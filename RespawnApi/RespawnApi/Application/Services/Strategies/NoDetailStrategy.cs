using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Services.Strategies
{
    /// <summary>
    /// Strategy for handling game servers that do not support detailed live information.
    /// </summary>
    public class NoDetailsStrategy : IGameServerInfoStrategy
    {
        private readonly ILogger<NoDetailsStrategy> _logger;

        public GameType SupportedGameType => (GameType)(-1); // Indicates it's a fallback, not for a specific game.

        public NoDetailsStrategy(ILogger<NoDetailsStrategy> logger)
        {
            _logger = logger;
        }

        public Task<GameServerDetailDto?> GetServerDetailsAsync(GameServer serverEntity, GameServerDto basicDto)
        {
            _logger.LogInformation(
                "NoDetailsStrategy použita pro server {ServerName} ({GameServerId}), typ: {GameType}. Nebudou načteny žádné další detaily.",
                serverEntity.Name, serverEntity.GameServerId, serverEntity.GameType);

            var detailDto = new GameServerDetailDto
            {
                // Copy properties from basicDto
                GameServerId = basicDto.GameServerId,
                Name = basicDto.Name,
                GameType = basicDto.GameType,
                Status = basicDto.Status,
                IpAddress = basicDto.IpAddress,
                Port = basicDto.Port,
                ContainerId = basicDto.ContainerId,
                CreatedAt = basicDto.CreatedAt,
                StatusDetails =
                    $"{basicDto.StatusDetails ?? ""} (Pro tento typ hry nejsou dostupné detailní live informace.)",

                // Default values for A2S specific fields
                GameName = basicDto.Name, // Fallback to DB name
                MapName = "N/A",
                CurrentPlayers = 0,
                MaxPlayers = 0,
                IsVacSecured = false,
                Players = new List<PlayerDetailDto>()
            };

            // If the server is not online, CurrentPlayers should definitely be 0.
            if (basicDto.Status != ServerStatus.Online)
            {
                detailDto.CurrentPlayers = 0;
            }

            return Task.FromResult<GameServerDetailDto?>(detailDto);
        }
    }
}