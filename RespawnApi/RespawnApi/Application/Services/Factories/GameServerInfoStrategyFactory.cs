// File: haha/RespawnApi/RespawnApi/Application/Services/Factories/GameServerInfoStrategyFactory.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RespawnApi.Application.Interfaces;
using RespawnApi.Application.Services.Strategies;
using RespawnApi.Domain.Enums;
using System;

namespace RespawnApi.Application.Services.Factories
{
    public class GameServerInfoStrategyFactory : IGameServerInfoStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameServerInfoStrategyFactory> _logger;

        public GameServerInfoStrategyFactory(IServiceProvider serviceProvider, ILogger<GameServerInfoStrategyFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IGameServerInfoStrategy GetStrategy(GameType gameType)
        {
            _logger.LogDebug("Továrna žádá strategii pro GameType: {GameType}", gameType);
            switch (gameType)
            {
                case GameType.CounterStrike:
                    _logger.LogDebug("Vracím A2SGoldSourceStrategy pro CounterStrike.");
                    return _serviceProvider.GetRequiredService<A2SGoldSourceStrategy>();

                // Příklad pro budoucí hru, která by mohla používat jinou A2S strategii (např. Source engine)
                // case GameType.TeamFortress2:
                // _logger.LogDebug("Vracím A2SSourceStrategy pro TeamFortress2.");
                // return _serviceProvider.GetRequiredService<A2SSourceStrategy>(); // Předpokládá se existence A2SSourceStrategy

                // Příklad pro hru, která by mohla používat RCON
                // case GameType.Minecraft: // Předpokládáme, že Minecraft je přidán do GameType enum
                // _logger.LogDebug("Vracím RconMinecraftStrategy pro Minecraft.");
                // return _serviceProvider.GetRequiredService<RconMinecraftStrategy>(); // Předpokládá se existence RconMinecraftStrategy

                default:
                    _logger.LogWarning("Pro GameType {GameType} nebyla nalezena specifická strategie, použije se NoDetailsStrategy.", gameType);
                    return _serviceProvider.GetRequiredService<NoDetailsStrategy>();
            }
        }
    }
}
