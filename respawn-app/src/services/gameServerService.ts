import type { GameServerDto } from '@/components/ServerCard.vue'
import { useAuthStore } from '@/stores/authStore'
import Swal from 'sweetalert2'

// Interface for player details shown in the frontend
export interface PlayerDetailDtoFE {
  name: string
  score: number
  duration: number
}

// Extended interface for game server details including player information
export interface GameServerDetailDtoFE extends GameServerDto {
  gameName?: string
  mapName?: string
  maxPlayers?: number
  currentPlayers?: number
  isVacSecured?: boolean
  players: PlayerDetailDtoFE[]
}

// Base URL for game server API endpoints
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL + '/api/gameservers'

/**
 * Creates a customized SweetAlert2 configuration with futuristic styling
 * @param title - Alert title
 * @param text - Alert message
 * @param icon - Alert icon type
 * @returns SweetAlert2 configuration object
 */
const getFuturisticSwalOptions = (
  title: string,
  text: string = '',
  icon: 'success' | 'error' | 'warning' | 'info' | 'question' = 'info',
) => {
  return {
    titleText: title,
    text: text,
    icon: icon,
    background: '#1A2033',
    color: '#E0E0E0',
    confirmButtonColor: '#00E0FF',
    customClass: {
      popup: 'futuristic-swal-popup',
      title: 'futuristic-swal-title font-oxanium',
      htmlContainer: 'futuristic-swal-html-container font-inter',
      confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
    },
    buttonsStyling: false,
    heightAuto: false,
  }
}

/**
 * Fetches detailed information about a specific game server
 * @param serverId - The ID of the game server to fetch details for
 * @returns Promise resolving to server details or null if request fails
 */
export const getGameServerDetails = async (
  serverId: string,
): Promise<GameServerDetailDtoFE | null> => {
  const authStore = useAuthStore()
  // Check if user is authenticated
  if (!authStore.token) {
    console.error('getGameServerDetails: Authentication token missing.')
    Swal.fire(
      getFuturisticSwalOptions(
        'Authentication Error',
        'Login required to view server details.',
        'error',
      ),
    )
    return null
  }

  try {
    // Make API request to fetch server details
    const response = await fetch(`${API_BASE_URL}/${serverId}/details`, {
      headers: {
        Authorization: `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
    })

    // Handle different response statuses
    if (response.status === 401) {
      authStore.logout()
      Swal.fire(
        getFuturisticSwalOptions(
          'Authorization Error',
          'Your session has expired. Please log in again.',
          'error',
        ),
      )
      return null
    }
    if (response.status === 404) {
      Swal.fire(
        getFuturisticSwalOptions(
          'Server Not Found',
          'The requested game server was not found.',
          'error',
        ),
      )
      return null
    }
    if (!response.ok) {
      const errorData = await response
        .json()
        .catch(() => ({ message: `Server error: ${response.statusText}` }))
      throw new Error(errorData.message || 'Failed to load server details.')
    }

    // Parse and return server details
    const data: GameServerDetailDtoFE = await response.json()
    return data
  } catch (error: any) {
    console.error(`getGameServerDetails (${serverId}) API error:`, error)
    Swal.fire(
      getFuturisticSwalOptions(
        'Loading Error',
        error.message || 'An error occurred while communicating with the server.',
        'error',
      ),
    )
    return null
  }
}
