using RespawnApi.Application.Interfaces;
using RespawnApi.Application.Services.Strategies;
using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Services.Factories
{
    /// <summary>
    /// Factory for creating instances of IGameServerInfoStrategy based on the GameType.
    /// </summary>
    public class GameServerInfoStrategyFactory : IGameServerInfoStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameServerInfoStrategyFactory> _logger;

        public GameServerInfoStrategyFactory(IServiceProvider serviceProvider,
            ILogger<GameServerInfoStrategyFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the appropriate <see cref="IGameServerInfoStrategy"/> implementation
        /// for the specified <see cref="GameType"/>.
        /// </summary>
        /// <param name="gameType">The type of game for which to obtain the strategy.</param>
        /// <returns>
        /// An instance of <see cref="IGameServerInfoStrategy"/> that supports the specified game type.
        /// </returns>
        public IGameServerInfoStrategy GetStrategy(GameType gameType)
        {
            _logger.LogDebug("Továrna žádá strategii pro GameType: {GameType}", gameType);
            switch (gameType)
            {
                case GameType.CounterStrike:
                    _logger.LogDebug("Vracím A2SGoldSourceStrategy pro CounterStrike.");
                    return _serviceProvider.GetRequiredService<A2SGoldSourceStrategy>();


                // case GameType.TeamFortress2:
                // _logger.LogDebug("Vracím A2SSourceStrategy pro TeamFortress2.");
                // return _serviceProvider.GetRequiredService<A2SSourceStrategy>();

                // case GameType.Minecraft:
                // _logger.LogDebug("Vracím RconMinecraftStrategy pro Minecraft.");
                // return _serviceProvider.GetRequiredService<RconMinecraftStrategy>();

                // and so on

                default:
                    _logger.LogWarning(
                        "Pro GameType {GameType} nebyla nalezena specifická strategie, použije se NoDetailsStrategy.",
                        gameType);
                    return _serviceProvider.GetRequiredService<NoDetailsStrategy>();
            }
        }
    }
}