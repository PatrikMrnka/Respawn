// File: haha/RespawnApi/RespawnApi/Application/Services/Strategies/NoDetailsStrategy.cs
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using RespawnApi.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RespawnApi.Application.Services.Strategies
{
    public class NoDetailsStrategy : IGameServerInfoStrategy
    {
        private readonly ILogger<NoDetailsStrategy> _logger;

        // This strategy could be made generic or have a specific "Unsupported" GameType
        // For now, let's assume it might be assigned to various game types if no specific strategy exists.
        // Or, the factory could decide not to assign it a specific GameType and use it as a default.
        public GameType SupportedGameType => (GameType)(-1); // Indicates it's a fallback, not for a specific game.

        public NoDetailsStrategy(ILogger<NoDetailsStrategy> logger)
        {
            _logger = logger;
        }

        public Task<GameServerDetailDto?> GetServerDetailsAsync(GameServer serverEntity, GameServerDto basicDto)
        {
            _logger.LogInformation("NoDetailsStrategy použita pro server {ServerName} ({GameServerId}), typ: {GameType}. Nebudou načteny žádné další detaily.",
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
                StatusDetails = $"{basicDto.StatusDetails ?? ""} (Pro tento typ hry nejsou dostupné detailní live informace.)",

                // Default values for A2S specific fields
                GameName = basicDto.Name, // Fallback to DB name
                MapName = "N/A",
                CurrentPlayers = 0, // Or keep basicDto.Status to determine if we can assume 0
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
