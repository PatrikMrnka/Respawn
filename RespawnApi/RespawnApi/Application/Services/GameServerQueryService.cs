// File: haha/RespawnApi/RespawnApi/Application/Services/GameServerQueryService.cs
using Microsoft.Extensions.Logging;
using RespawnApi.Application.DTOs.GameServer;
using RespawnApi.Application.Interfaces;
using RespawnApi.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace RespawnApi.Application.Services
{
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
                return null; // Nebo vrátit fallback DTO, pokud basicDto je k dispozici
            }
            if (basicDto == null) // basicDto by mělo být vždy dostupné, pokud serverEntity existuje
            {
                _logger.LogWarning("GetServerDetailsAsync voláno s null basicDto pro server {ServerId}.", serverEntity.GameServerId);
                // Můžete vytvořit basicDto z serverEntity zde, pokud je to nutné
                // Prozatím předpokládáme, že volající poskytne platné basicDto
                return null;
            }


            _logger.LogInformation("Získávám strategii pro GameType: {GameType} serveru {ServerName} ({ServerId})",
                serverEntity.GameType, serverEntity.Name, serverEntity.GameServerId);

            IGameServerInfoStrategy strategy = _strategyFactory.GetStrategy(serverEntity.GameType);

            // Továrna by měla vždy vrátit nějakou strategii (alespoň NoDetailsStrategy)
            // Kontrola na null je spíše pro robustnost, pokud by továrna mohla selhat.
            if (strategy == null)
            {
                _logger.LogError("Nepodařilo se získat strategii z továrny pro GameType: {GameType}. Toto by se nemělo stát, pokud je továrna správně nakonfigurována s výchozí strategií.", serverEntity.GameType);
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

                // Předáme serverEntity i basicDto. Strategie se může rozhodnout, co použije.
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
                    StatusDetails = $"Chyba při získávání detailů serveru pomocí strategie {strategy.GetType().Name}: {ex.Message}",
                    Players = new System.Collections.Generic.List<PlayerDetailDto>()
                };
            }
        }
    }
}
