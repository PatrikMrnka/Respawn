// File: haha/RespawnApi/RespawnApi/Application/Interfaces/IGameServerInfoStrategyFactory.cs
using RespawnApi.Domain.Enums;

namespace RespawnApi.Application.Interfaces
{
    public interface IGameServerInfoStrategyFactory
    {
        IGameServerInfoStrategy GetStrategy(GameType gameType);
    }
}
