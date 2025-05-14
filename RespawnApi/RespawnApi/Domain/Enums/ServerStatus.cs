namespace RespawnApi.Domain.Enums
{
    public enum ServerStatus
    {
        UNKNOWN = 0, // undefined
        NOTINSTALLED = 1, // exists only in db
        INSTALLING = 2, // installing
        STOPPED = 3, // stopped
        STARTING = 4, // starting
        RUNNING = 5, // running
        STOPPING = 6, // stopping
        UPDATING = 7, // updating
        ERROR = 8, // error
        REMOVED = 9
    }
}
