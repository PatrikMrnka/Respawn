/**
 * Poll Service Module
 * This module provides functions to interact with the poll API endpoints
 */

import { useAuthStore } from '@/stores/authStore'
import Swal, { type SweetAlertOptions } from 'sweetalert2'

// Base URL for the polls API
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL + '/api/polls'

/**
 * Represents a single poll option
 */
export interface PollOption {
  optionId: string
  text: string
  imageUrl?: string
  voteCount?: number
}

/**
 * Payload for updating a poll option
 */
export interface UpdatePollOptionPayload {
  optionId?: string
  text: string
  imageUrl?: string
}

/**
 * Full poll data structure with all details
 */
export interface Poll {
  pollId: string
  question: string
  endTime: string
  isClosed: boolean
  imageUrl?: string
  creatorUserId: string
  creatorNickname?: string
  options: PollOption[]
  isMultipleChoice: boolean
  userVotedOptionIds?: string[]
  totalVotes?: number
}

/**
 * Data transfer object for creating a new poll
 */
export interface CreatePollDto {
  question: string
  endTime: string
  imageUrl?: string
  options: Array<{ text: string; imageUrl?: string }>
  isMultipleChoice: boolean
}

/**
 * Data transfer object for updating an existing poll
 */
export interface UpdatePollDto {
  question: string
  endTime: string
  imageUrl?: string
  options: UpdatePollOptionPayload[]
}

/**
 * Data transfer object for submitting a vote
 */
export interface SubmitVoteDto {
  optionIds: string[]
}

/**
 * Returns SweetAlert2 options with futuristic styling
 * @param title - The title for the alert
 * @returns SweetAlertOptions configuration
 */
const getFuturisticSwalOptions = (title: string): SweetAlertOptions => ({
  titleText: title,
  background: '#1A2033',
  color: '#E0E0E0',
  confirmButtonColor: '#00E0FF',
  cancelButtonColor: '#FF5252',
  customClass: {
    popup: 'futuristic-swal-popup',
    title: 'futuristic-swal-title font-oxanium',
    htmlContainer: 'futuristic-swal-html-container font-inter',
    confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
    cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
  },
  buttonsStyling: false,
  heightAuto: false,
})

/**
 * Fetches all available polls
 * @returns Promise with an array of polls or empty array on failure
 */
export const getAllPolls = async (): Promise<Poll[]> => {
  const authStore = useAuthStore()
  if (!authStore.token) {
    console.error('getAllPolls: Missing authentication token.')
    return []
  }
  try {
    const response = await fetch(API_BASE_URL, {
      headers: { Authorization: `Bearer ${authStore.token}` },
    })
    if (!response.ok) {
      const errorData = await response
        .json()
        .catch(() => ({ message: `Server error: ${response.statusText}` }))
      throw new Error(errorData.message || 'Nepodařilo se načíst ankety.')
    }
    return await response.json()
  } catch (error: any) {
    console.error('getAllPolls API error:', error)
    Swal.fire({
      ...getFuturisticSwalOptions('Error načítání anket'),
      text: error.message,
      icon: 'error',
    })
    return []
  }
}

/**
 * Fetches a specific poll by ID
 * @param pollId - ID of the poll to fetch
 * @returns Promise with poll data or null on failure
 */
export const getPollById = async (pollId: string): Promise<Poll | null> => {
  const authStore = useAuthStore()
  if (!authStore.token) return null
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}`, {
      headers: { Authorization: `Bearer ${authStore.token}` },
    })
    if (!response.ok) throw new Error('Failed to load poll details.')
    return await response.json()
  } catch (error: any) {
    console.error(`getPollById (${pollId}) API error:`, error)
    Swal.fire({ ...getFuturisticSwalOptions('Error'), text: error.message, icon: 'error' })
    return null
  }
}

/**
 * Creates a new poll
 * @param pollData - Data for creating the poll
 * @returns Promise with created poll data or null on failure
 */
export const createPoll = async (pollData: CreatePollDto): Promise<Poll | null> => {
  const authStore = useAuthStore()
  if (!authStore.token) return null
  try {
    const response = await fetch(API_BASE_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authStore.token}`,
      },
      body: JSON.stringify(pollData),
    })
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Nepodařilo se vytvořit anketu.' }))
      throw new Error(errorData.message || 'Nepodařilo se vytvořit anketu.')
    }
    Swal.fire({
      ...getFuturisticSwalOptions('Úspěch!'),
      text: 'Anketa byla úspěšně vytvořena.',
      icon: 'success',
      timer: 2000,
      showConfirmButton: false,
    })
    return await response.json()
  } catch (error: any) {
    console.error('createPoll API error:', error)
    Swal.fire({
      ...getFuturisticSwalOptions('Error vytváření ankety'),
      text: error.message,
      icon: 'error',
    })
    return null
  }
}

/**
 * Updates an existing poll
 * @param pollId - ID of the poll to update
 * @param pollData - Updated poll data
 * @returns Promise with updated poll data or null on failure
 */
export const updatePoll = async (pollId: string, pollData: UpdatePollDto): Promise<Poll | null> => {
  const authStore = useAuthStore()
  if (!authStore.token) return null
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authStore.token}`,
      },
      body: JSON.stringify(pollData),
    })
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Nepodařilo se aktualizovat anketu.' }))
      throw new Error(errorData.message || 'Nepodařilo se aktualizovat anketu.')
    }
    Swal.fire({
      ...getFuturisticSwalOptions('Úspěch!'),
      text: 'Anketa byla úspěšně aktualizována.',
      icon: 'success',
      timer: 2000,
      showConfirmButton: false,
    })
    return await response.json() // API is expected to return the updated poll
  } catch (error: any) {
    console.error(`updatePoll (${pollId}) API error:`, error)
    Swal.fire({
      ...getFuturisticSwalOptions('Error upravování ankety'),
      text: error.message,
      icon: 'error',
    })
    return null
  }
}

/**
 * Deletes a poll
 * @param pollId - ID of the poll to delete
 * @returns Promise with boolean indicating success or failure
 */
export const deletePoll = async (pollId: string): Promise<boolean> => {
  const authStore = useAuthStore()
  if (!authStore.token) return false
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${authStore.token}` },
    })
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Nepodařilo se odstranit anketu.' }))
      throw new Error(errorData.message || 'Nepodařilo se odstranit anketu.')
    }
    Swal.fire({
      ...getFuturisticSwalOptions('Smazáno!'),
      text: 'Anketa byla úspěšně smazána.',
      icon: 'success',
      timer: 2000,
      showConfirmButton: false,
    })
    return true
  } catch (error: any) {
    console.error(`deletePoll (${pollId}) API error:`, error)
    Swal.fire({
      ...getFuturisticSwalOptions('Error mazání ankety'),
      text: error.message,
      icon: 'error',
    })
    return false
  }
}

/**
 * Submits a vote for a poll
 * @param pollId - ID of the poll to vote on
 * @param voteData - Vote data containing selected option IDs
 * @returns Promise with updated poll data or null on failure
 */
export const submitVote = async (pollId: string, voteData: SubmitVoteDto): Promise<Poll | null> => {
  const authStore = useAuthStore()
  if (!authStore.token) return null
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}/vote`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authStore.token}`,
      },
      body: JSON.stringify(voteData),
    })
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Chyba při hlasování.' }))
      throw new Error(errorData.message || 'Chyba při hlasování.')
    }
    Swal.fire({
      ...getFuturisticSwalOptions('Hlas zaznamenán!'),
      text: 'Váš hlas byl úspěšně zaznamenán.',
      icon: 'success',
      timer: 2000,
      showConfirmButton: false,
    })
    return await response.json()
  } catch (error: any) {
    console.error(`submitVote (${pollId}) API error:`, error)
    Swal.fire({ ...getFuturisticSwalOptions('Error hlasování'), text: error.message, icon: 'error' })
    return null
  }
}
