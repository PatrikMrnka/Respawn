// Domain/Enums/ServerStatus.cs
namespace RespawnApi.Domain.Enums
{
    public enum ServerStatus
    {
        Unknown = 0,        // Neznámý nebo nedefinovaný stav
        Offline = 1,        // Kontejner neběží (exited, dead, not_found, created)
        Online = 2,         // Kontejner běží (running)
        Starting = 3,       // Kontejner se spouští (přechodný stav)
        Stopping = 4,       // Kontejner se zastavuje (přechodný stav)
        Restarting = 5,     // Kontejner se restartuje (přechodný stav)
        Error = 8,          // Došlo k chybě s kontejnerem nebo Docker API
        PendingCreation = 9 // Server je v DB, ale kontejner se teprve vytváří
    }
}