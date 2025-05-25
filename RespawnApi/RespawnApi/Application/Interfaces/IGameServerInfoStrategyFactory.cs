using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Interfaces
{
    /// <summary>
    /// Factory interface for creating game server info strategies based on the game type.
    /// </summary>
    public interface IGameServerInfoStrategyFactory
    {
        /// <summary>
        /// Retrieves the appropriate <see cref="IGameServerInfoStrategy"/> implementation
        /// for the specified <see cref="GameType"/>.
        /// </summary>
        /// <param name="gameType">The type of game for which to obtain the strategy.</param>
        /// <returns>
        /// An instance of <see cref="IGameServerInfoStrategy"/> that supports the specified game type.
        /// </returns>
        IGameServerInfoStrategy GetStrategy(GameType gameType);
    }
}
