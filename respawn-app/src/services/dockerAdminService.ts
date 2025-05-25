import { useAuthStore } from '@/stores/authStore'
import type { DockerVolumeDto, DockerContainerDto, DockerImageDto } from '@/types/dockerAdmin'
import Swal from 'sweetalert2'

// Base URL for Docker admin API
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL + '/api/dockeradmin'

/**
 * Gets authentication headers for API requests
 * @returns Object with Authorization and Content-Type headers
 */
const getAuthHeaders = () => {
  const authStore = useAuthStore()
  return {
    Authorization: `Bearer ${authStore.token}`,
    'Content-Type': 'application/json',
  }
}

/**
 * Handles API error responses
 * @param response - The fetch Response object
 * @param defaultMessage - Default error message if response parsing fails
 * @returns Parsed JSON response or null for 204 responses
 */
const handleApiError = async (response: Response, defaultMessage: string) => {
  if (!response.ok) {
    try {
      const errorData = await response.json()
      throw new Error(errorData.message || errorData.title || defaultMessage)
    } catch (e) {
      // If response is not JSON or json() fails
      throw new Error(`${defaultMessage} (Status: ${response.status} ${response.statusText})`)
    }
  }
  // If response is OK but might be empty (e.g., 204 No Content)
  if (response.status === 204) return null
  return response.json()
}

// ----- Volumes API -----

/**
 * Fetches all Docker volumes
 * @returns Promise with array of Docker volumes
 */
export const getVolumes = async (): Promise<DockerVolumeDto[]> => {
  const response = await fetch(`${API_BASE_URL}/volumes`, { headers: getAuthHeaders() })
  return handleApiError(response, 'Failed to load Docker volumes.')
}

/**
 * Deletes a Docker volume
 * @param volumeName - Name of the volume to delete
 * @param force - Whether to force deletion
 * @returns Promise resolving to success boolean
 */
export const deleteVolume = async (
  volumeName: string,
  force: boolean = false,
): Promise<boolean> => {
  const response = await fetch(
    `${API_BASE_URL}/volumes/${encodeURIComponent(volumeName)}?force=${force}`,
    {
      method: 'DELETE',
      headers: getAuthHeaders(),
    },
  )
  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: `Error deleting volume ${volumeName}.` }))
    Swal.fire('Error', errorData.message, 'error')
    return false
  }
  return true
}

// ----- Containers API -----

/**
 * Fetches all Docker containers
 * @param all - Whether to include stopped containers
 * @returns Promise with array of Docker containers
 */
export const getContainers = async (all: boolean = true): Promise<DockerContainerDto[]> => {
  const response = await fetch(`${API_BASE_URL}/containers?all=${all}`, {
    headers: getAuthHeaders(),
  })
  return handleApiError(response, 'Failed to load Docker containers.')
}

/**
 * Starts a Docker container
 * @param containerId - ID of the container to start
 * @returns Promise resolving to success boolean
 */
export const startContainer = async (containerId: string): Promise<boolean> => {
  const response = await fetch(`${API_BASE_URL}/containers/${containerId}/start`, {
    method: 'POST',
    headers: getAuthHeaders(),
  })
  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: `Error starting container ${containerId}.` }))
    Swal.fire('Error', errorData.message, 'error')
    return false
  }
  return true
}

/**
 * Stops a Docker container
 * @param containerId - ID of the container to stop
 * @returns Promise resolving to success boolean
 */
export const stopContainer = async (containerId: string): Promise<boolean> => {
  const response = await fetch(`${API_BASE_URL}/containers/${containerId}/stop`, {
    method: 'POST',
    headers: getAuthHeaders(),
  })
  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: `Error stopping container ${containerId}.` }))
    Swal.fire('Error', errorData.message, 'error')
    return false
  }
  return true
}

/**
 * Deletes a Docker container
 * @param containerId - ID of the container to delete
 * @param removeVolume - Whether to remove associated volumes
 * @returns Promise resolving to success boolean
 */
export const deleteContainer = async (
  containerId: string,
  removeVolume: boolean = false,
): Promise<boolean> => {
  const response = await fetch(
    `${API_BASE_URL}/containers/${containerId}?removeVolume=${removeVolume}`,
    {
      method: 'DELETE',
      headers: getAuthHeaders(),
    },
  )
  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: `Error deleting container ${containerId}.` }))
    Swal.fire('Error', errorData.message, 'error')
    return false
  }
  return true
}

/**
 * Gets logs from a Docker container
 * @param containerId - ID of the container
 * @param tail - Number of log lines to retrieve
 * @returns Promise with log text
 */
export const getContainerLogs = async (
  containerId: string,
  tail: number = 200,
): Promise<string> => {
  const response = await fetch(`${API_BASE_URL}/containers/${containerId}/logs?tail=${tail}`, {
    headers: getAuthHeaders(),
  })
  if (!response.ok) {
    const errorText = await response.text() // Logs might be text, not JSON
    throw new Error(errorText || `Failed to load logs for container ${containerId}.`)
  }
  return response.text()
}

// ----- Images API -----

/**
 * Fetches Docker images
 * @param all - Whether to include intermediate images
 * @returns Promise with array of Docker images
 */
export const getImages = async (all: boolean = false): Promise<DockerImageDto[]> => {
  const response = await fetch(`${API_BASE_URL}/images?all=${all}`, { headers: getAuthHeaders() })
  return handleApiError(response, 'Failed to load Docker images.')
}

/**
 * Deletes a Docker image
 * @param imageId - ID of the image to delete
 * @param force - Whether to force deletion
 * @param pruneChildren - Whether to remove dependent child images
 * @returns Promise resolving to success boolean
 */
export const deleteImage = async (
  imageId: string,
  force: boolean = false,
  pruneChildren: boolean = false,
): Promise<boolean> => {
  const response = await fetch(
    `${API_BASE_URL}/images/${encodeURIComponent(imageId)}?force=${force}&pruneChildren=${pruneChildren}`,
    {
      method: 'DELETE',
      headers: getAuthHeaders(),
    },
  )
  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: `Error deleting image ${imageId}.` }))
    Swal.fire('Error', errorData.message, 'error')
    return false
  }
  return true
}
