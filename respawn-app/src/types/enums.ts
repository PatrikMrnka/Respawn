// src/types/enums.ts

// Mělo by odpovídat enumu na backendu: Domain/Enums/GameType.cs
export enum GameType 
{
    CS = 1
}

// Mělo by odpovídat enumu na backendu: Domain/Enums/ServerStatus.cs
export enum ServerStatus 
{
    NOT_INSTALLED = 1,
}

export enum UserRoles 
{
    Administrator = "Administrátor",
    Spravce = "Správce",
    Uzivatel = "Hráč",
}