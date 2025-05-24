// Domain/Enums/ServerStatus.cs
namespace RespawnApi.Domain.Enums
{
    public enum ServerStatus
    {
        Unknown = 0,
        Offline = 1,
        Online = 2,
        Starting = 3,
        Stopping = 4,
        Restarting = 5,
        Installing = 6,
        Updating = 7,
        Error = 8,
        PendingCreation = 9,
        Deleting = 10
    }
}