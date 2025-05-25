// src/types/enums.ts

// Mělo by odpovídat enumu na backendu: Domain/Enums/GameType.cs
export enum GameType 
{
        CounterStrike = 1,
        TeamFortress2 = 2,
        GarrysMod = 3
}

// Mělo by odpovídat enumu na backendu: Domain/Enums/ServerStatus.cs
export enum ServerStatus 
{
        Unknown = 0,
        Offline = 1,
        Online = 2,
        Starting = 3,
        Stopping = 4,
        Restarting = 5,
        Error = 8,
        PendingCreation = 9,
}

export enum UserRoles 
{
    Administrator = "Administrátor",
    Spravce = "Správce",
    Uzivatel = "Hráč",
}