using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;

namespace RespawnApi.Application.Services
{
    /// <summary>
    /// Provides methods to query game server details based on the game type.
    /// </summary>
    public class GameServerQueryService : IGameServerQueryService
    {
        private readonly IGameServerInfoStrategyFactory _strategyFactory;
        private readonly ILogger<GameServerQueryService> _logger;

        public GameServerQueryService(
            IGameServerInfoStrategyFactory strategyFactory,
            ILogger<GameServerQueryService> logger)
        {
            _strategyFactory = strategyFactory;
            _logger = logger;
        }

        public async Task<GameServerDetailDto?> GetServerDetailsAsync(GameServer serverEntity, GameServerDto basicDto)
        {
            if (serverEntity == null)
            {
                _logger.LogWarning("GetServerDetailsAsync voláno s null serverEntity.");
                return null;
            }

            if (basicDto == null)
            {
                _logger.LogWarning("GetServerDetailsAsync voláno s null basicDto pro server {ServerId}.",
                    serverEntity.GameServerId);
                return null;
            }


            _logger.LogInformation("Získávám strategii pro GameType: {GameType} serveru {ServerName} ({ServerId})",
                serverEntity.GameType, serverEntity.Name, serverEntity.GameServerId);

            IGameServerInfoStrategy strategy = _strategyFactory.GetStrategy(serverEntity.GameType);

            if (strategy == null) // if the strategy is not found, log an error and return a default DTO
            {
                _logger.LogError(
                    "Nepodařilo se získat strategii z továrny pro GameType: {GameType}. Toto by se nemělo stát, pokud je továrna správně nakonfigurována s výchozí strategií.",
                    serverEntity.GameType);
                return new GameServerDetailDto
                {
                    GameServerId = basicDto.GameServerId,
                    Name = basicDto.Name,
                    GameType = basicDto.GameType,
                    Status = basicDto.Status,
                    IpAddress = basicDto.IpAddress,
                    Port = basicDto.Port,
                    ContainerId = basicDto.ContainerId,
                    CreatedAt = basicDto.CreatedAt,
                    StatusDetails = "Interní chyba: Nepodařilo se určit strategii pro získání detailů serveru.",
                    Players = new System.Collections.Generic.List<PlayerDetailDto>()
                };
            }

            try
            {
                _logger.LogInformation("Používám strategii {StrategyName} pro server {ServerName} ({GameType})",
                    strategy.GetType().Name, serverEntity.Name, serverEntity.GameType);

                
                return await strategy.GetServerDetailsAsync(serverEntity, basicDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba při provádění strategie {StrategyName} pro server {ServerName}",
                    strategy.GetType().Name, serverEntity.Name);

                return new GameServerDetailDto
                {
                    GameServerId = basicDto.GameServerId,
                    Name = basicDto.Name,
                    GameType = basicDto.GameType,
                    Status = basicDto.Status,
                    IpAddress = basicDto.IpAddress,
                    Port = basicDto.Port,
                    ContainerId = basicDto.ContainerId,
                    CreatedAt = basicDto.CreatedAt,
                    StatusDetails =
                        $"Chyba při získávání detailů serveru pomocí strategie {strategy.GetType().Name}: {ex.Message}",
                    Players = new System.Collections.Generic.List<PlayerDetailDto>()
                };
            }
        }
    }
}