// File: haha/respawn-app/src/composables/useGameDisplayHelpers.ts
import { GameType, ServerStatus } from '@/types/enums'; // Ujistěte se, že cesta k enums je správná
import { 
    mdiServer, mdiAlphaTBoxOutline, mdiPuzzleOutline, mdiServerOff,
    mdiCircleSlice8, // Default/Unknown
    mdiPlayCircleOutline, // Starting
    mdiStopCircleOutline, // Stopping
    mdiAlertCircleOutline, // Error
    mdiTimerSand, // PendingCreation
    mdiSync, // Restarting
    mdiCheckCircleOutline, // Online
    mdiCloseCircleOutline // Offline
} from '@mdi/js';

/**
 * Composable funkce poskytující pomocné metody pro zobrazení informací o hře a stavu serveru.
 */
export function useGameDisplayHelpers() {
    /**
     * Vrátí ikonu pro daný typ hry.
     * @param gameType Typ hry.
     * @returns Cesta k SVG ikoně.
     */
    const getGameIcon = (gameType: GameType | undefined): string => {
        if (gameType === undefined) return mdiServerOff;
        return {
            [GameType.CounterStrike]: mdiServer,
            [GameType.TeamFortress2]: mdiAlphaTBoxOutline,
            [GameType.GarrysMod]: mdiPuzzleOutline,
        }[gameType] || mdiServerOff; // Výchozí ikona, pokud typ není nalezen
    };

    /**
     * Vrátí textový popis pro daný typ hry.
     * @param gameType Typ hry.
     * @returns Textový popis hry.
     */
    const getGameTypeText = (gameType: GameType | undefined): string => {
        if (gameType === undefined) return "Neznámá hra";
        return {
            [GameType.CounterStrike]: "Counter-Strike 1.6",
            [GameType.TeamFortress2]: "Team Fortress 2",
            [GameType.GarrysMod]: "Garry's Mod",
        }[gameType] || "Neznámá hra";
    };

    /**
     * Vrátí barvu pro daný stav serveru.
     * @param status Stav serveru.
     * @returns Název barvy (pro Vuetify).
     */
    const getOverallStatusColor = (status: ServerStatus | undefined): string => {
        if (status === undefined) return 'grey-darken-1';
        return {
            [ServerStatus.Online]: 'success',
            [ServerStatus.Offline]: 'error',
            [ServerStatus.Starting]: 'info',
            [ServerStatus.Stopping]: 'warning',
            [ServerStatus.Error]: 'deep-orange-accent-4',
            [ServerStatus.PendingCreation]: 'blue-grey-lighten-1',
            [ServerStatus.Unknown]: 'grey-darken-1',
            [ServerStatus.Restarting]: 'cyan',
        }[status] || 'grey-darken-1'; // Výchozí barva
    };

    /**
     * Vrátí textový popis pro daný stav serveru.
     * @param status Stav serveru.
     * @returns Textový popis stavu.
     */
    const getServerStatusText = (status: ServerStatus | undefined): string => {
        if (status === undefined) return "Neznámý stav";
        return {
            [ServerStatus.Online]: "Online",
            [ServerStatus.Offline]: "Offline",
            [ServerStatus.Starting]: "Spouští se",
            [ServerStatus.Stopping]: "Zastavuje se",
            [ServerStatus.Error]: "Chyba",
            [ServerStatus.PendingCreation]: "Čeká na vytvoření",
            [ServerStatus.Unknown]: "Neznámý",
            [ServerStatus.Restarting]: "Restartuje se",
        }[status] || "Neznámý stav";
    };
    
    /**
     * Vrátí ikonu pro daný stav serveru.
     * @param status Stav serveru.
     * @returns Cesta k SVG ikoně.
     */
    const getServerStatusIcon = (status: ServerStatus | undefined): string => {
        if (status === undefined) return mdiCircleSlice8; // Default/Unknown icon
         return {
            [ServerStatus.Online]: mdiCheckCircleOutline,
            [ServerStatus.Offline]: mdiCloseCircleOutline,
            [ServerStatus.Starting]: mdiPlayCircleOutline, // or mdiProgressClock, mdiAutorenew
            [ServerStatus.Stopping]: mdiStopCircleOutline, // or mdiProgressClock
            [ServerStatus.Error]: mdiAlertCircleOutline,
            [ServerStatus.PendingCreation]: mdiTimerSand,
            [ServerStatus.Unknown]: mdiCircleSlice8,
            [ServerStatus.Restarting]: mdiSync,
        }[status] || mdiCircleSlice8;
    };

    /**
     * Určuje, zda je server ve stavu, kdy se načítá nebo provádí přechod.
     * @param status Stav serveru.
     * @returns True, pokud je server ve stavu načítání/přechodu.
     */
    const isLoadingStatus = (status: ServerStatus | undefined): boolean => {
        if (status === undefined) return true; // Treat undefined as loading to prevent actions
        return [
            ServerStatus.Starting, ServerStatus.Stopping,
            ServerStatus.PendingCreation, ServerStatus.Restarting
        ].includes(status);
    };

    /**
     * Určuje, zda by akce se serverem měly být zakázány na základě jeho stavu.
     * @param status Stav serveru.
     * @returns True, pokud by akce měly být zakázány.
     */
    const isActionDisabled = (status: ServerStatus | undefined): boolean => {
        if (status === undefined) return true;
        return isLoadingStatus(status) || status === ServerStatus.Unknown || status === ServerStatus.PendingCreation;
    };

    /**
     * Formátuje datum a čas.
     * @param dateString Řetězec s datem nebo Date objekt.
     * @returns Formátovaný řetězec data a času, nebo 'N/A'.
     */
    const formatFullDateTime = (dateString?: string | Date): string => {
        if (!dateString) return 'N/A';
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return 'N/A'; // Check for invalid date
        return date.toLocaleString('cs-CZ', {
            day: '2-digit', month: '2-digit', year: 'numeric',
            hour: '2-digit', minute: '2-digit'
        });
    };

    /**
     * Zkrátí text na danou délku a přidá "..."
     * @param value Text k zkrácení.
     * @param length Maximální délka.
     * @returns Zkrácený text.
     */
    const truncateText = (value: string | null | undefined, length: number = 50): string => {
        if (!value) return '';
        if (value.length <= length) return value;
        return value.substring(0, length) + '...';
    };


    return {
        getGameIcon,
        getGameTypeText,
        getOverallStatusColor,
        getServerStatusText,
        getServerStatusIcon,
        isLoadingStatus,
        isActionDisabled,
        formatFullDateTime,
        truncateText
    };
}
