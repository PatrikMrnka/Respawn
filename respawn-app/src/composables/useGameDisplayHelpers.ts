import { GameType, ServerStatus } from '@/types/enums'
import {
  mdiServer,
  mdiAlphaTBoxOutline,
  mdiPuzzleOutline,
  mdiServerOff,
  mdiCircleSlice8, // Default/Unknown
  mdiPlayCircleOutline, // Starting
  mdiStopCircleOutline, // Stopping
  mdiAlertCircleOutline, // Error
  mdiTimerSand, // PendingCreation
  mdiSync, // Restarting
  mdiCheckCircleOutline, // Online
  mdiCloseCircleOutline, // Offline
} from '@mdi/js'

/**
 * Composable function providing helper methods for displaying game and server status information.
 */
export function useGameDisplayHelpers() {
  /**
   * Returns an icon for the given game type.
   * @param gameType Game type.
   * @returns Path to the SVG icon.
   */
  const getGameIcon = (gameType: GameType | undefined): string => {
    if (gameType === undefined) return mdiServerOff
    return (
      {
        [GameType.CounterStrike]: mdiServer,
        [GameType.TeamFortress2]: mdiAlphaTBoxOutline,
        [GameType.GarrysMod]: mdiPuzzleOutline,
      }[gameType] || mdiServerOff
    ) // Default icon if type is not found
  }

  /**
   * Returns a text description for the given game type.
   * @param gameType Game type.
   * @returns Text description of the game.
   */
  const getGameTypeText = (gameType: GameType | undefined): string => {
    if (gameType === undefined) return 'Unknown game'
    return (
      {
        [GameType.CounterStrike]: 'Counter-Strike 1.6',
        [GameType.TeamFortress2]: 'Team Fortress 2',
        [GameType.GarrysMod]: "Garry's Mod",
      }[gameType] || 'Unknown game'
    )
  }

  /**
   * Returns a color for the given server status.
   * @param status Server status.
   * @returns Color name (for Vuetify).
   */
  const getOverallStatusColor = (status: ServerStatus | undefined): string => {
    if (status === undefined) return 'grey-darken-1'
    return (
      {
        [ServerStatus.Online]: 'success',
        [ServerStatus.Offline]: 'error',
        [ServerStatus.Starting]: 'info',
        [ServerStatus.Stopping]: 'warning',
        [ServerStatus.Error]: 'deep-orange-accent-4',
        [ServerStatus.PendingCreation]: 'blue-grey-lighten-1',
        [ServerStatus.Unknown]: 'grey-darken-1',
        [ServerStatus.Restarting]: 'cyan',
      }[status] || 'grey-darken-1'
    ) // Default color
  }

  /**
   * Returns a text description for the given server status.
   * @param status Server status.
   * @returns Text description of the status.
   */
  const getServerStatusText = (status: ServerStatus | undefined): string => {
    if (status === undefined) return 'Unknown status'
    return (
      {
        [ServerStatus.Online]: 'Online',
        [ServerStatus.Offline]: 'Offline',
        [ServerStatus.Starting]: 'Starting',
        [ServerStatus.Stopping]: 'Stopping',
        [ServerStatus.Error]: 'Error',
        [ServerStatus.PendingCreation]: 'Pending creation',
        [ServerStatus.Unknown]: 'Unknown',
        [ServerStatus.Restarting]: 'Restarting',
      }[status] || 'Unknown status'
    )
  }

  /**
   * Returns an icon for the given server status.
   * @param status Server status.
   * @returns Path to the SVG icon.
   */
  const getServerStatusIcon = (status: ServerStatus | undefined): string => {
    if (status === undefined) return mdiCircleSlice8 // Default/Unknown icon
    return (
      {
        [ServerStatus.Online]: mdiCheckCircleOutline,
        [ServerStatus.Offline]: mdiCloseCircleOutline,
        [ServerStatus.Starting]: mdiPlayCircleOutline, // or mdiProgressClock, mdiAutorenew
        [ServerStatus.Stopping]: mdiStopCircleOutline, // or mdiProgressClock
        [ServerStatus.Error]: mdiAlertCircleOutline,
        [ServerStatus.PendingCreation]: mdiTimerSand,
        [ServerStatus.Unknown]: mdiCircleSlice8,
        [ServerStatus.Restarting]: mdiSync,
      }[status] || mdiCircleSlice8
    )
  }

  /**
   * Determines if the server is in a loading or transitional state.
   * @param status Server status.
   * @returns True if the server is in a loading/transitional state.
   */
  const isLoadingStatus = (status: ServerStatus | undefined): boolean => {
    if (status === undefined) return true // Treat undefined as loading to prevent actions
    return [
      ServerStatus.Starting,
      ServerStatus.Stopping,
      ServerStatus.PendingCreation,
      ServerStatus.Restarting,
    ].includes(status)
  }

  /**
   * Determines if server actions should be disabled based on its status.
   * @param status Server status.
   * @returns True if actions should be disabled.
   */
  const isActionDisabled = (status: ServerStatus | undefined): boolean => {
    if (status === undefined) return true
    return (
      isLoadingStatus(status) ||
      status === ServerStatus.Unknown ||
      status === ServerStatus.PendingCreation
    )
  }

  /**
   * Formats date and time.
   * @param dateString Date string or Date object.
   * @returns Formatted date and time string, or 'N/A'.
   */
  const formatFullDateTime = (dateString?: string | Date): string => {
    if (!dateString) return 'N/A'
    const date = new Date(dateString)
    if (isNaN(date.getTime())) return 'N/A' // Check for invalid date
    return date.toLocaleString('cs-CZ', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  /**
   * Truncates text to a given length and adds "..."
   * @param value Text to truncate.
   * @param length Maximum length.
   * @returns Truncated text.
   */
  const truncateText = (value: string | null | undefined, length: number = 50): string => {
    if (!value) return ''
    if (value.length <= length) return value
    return value.substring(0, length) + '...'
  }

  return {
    getGameIcon,
    getGameTypeText,
    getOverallStatusColor,
    getServerStatusText,
    getServerStatusIcon,
    isLoadingStatus,
    isActionDisabled,
    formatFullDateTime,
    truncateText,
  }
}
