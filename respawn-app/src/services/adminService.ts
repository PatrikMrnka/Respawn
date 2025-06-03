import { useAuthStore } from '@/stores/authStore'
import Swal, { type SweetAlertOptions } from 'sweetalert2'
import type { UserInfo } from '@/stores/authStore'

// Base URL for admin API endpoints
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL + '/api/admin'

/**
 * Creates custom SweetAlert options with futuristic styling
 * @param title - Alert title text
 * @returns SweetAlert configuration options
 */
const getFuturisticSwalOptions = (title: string): SweetAlertOptions => {
  return {
    titleText: title,
    background: '#1A2033', // Dark blue background
    color: '#E0E0E0', // Light text
    confirmButtonColor: '#00E0FF', // Turquoise confirm button
    cancelButtonColor: '#FF5252', // Red cancel button
    customClass: {
      popup: 'futuristic-swal-popup',
      title: 'futuristic-swal-title font-oxanium',
      htmlContainer: 'futuristic-swal-html-container font-inter',
      confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
      cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
      actions: 'futuristic-swal-actions',
      validationMessage: 'futuristic-swal-validation-message font-inter',
    },
    buttonsStyling: false,
    heightAuto: false,
  }
}

// Standard API response interface
interface ApiResponse {
  isSuccess: boolean
  message: string
  data?: any
}

/**
 * Fetches all users from the system
 * @returns Promise containing array of UserInfo objects
 */
export const getAllUsers = async (): Promise<UserInfo[]> => {
  const authStore = useAuthStore()
  if (!authStore.token) {
    console.error('getAllUsers: Chybí token!')
    Swal.fire((getFuturisticSwalOptions('Error').text = 'Chybí token!'))
    return []
  }

  try {
    const response = await fetch(`${API_BASE_URL}/users`, {
      method: 'GET',
      headers: {
        Authorization: `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
    })

    // Handle authentication errors
    if (response.status === 401 || response.status === 403) {
      authStore.logout()
      Swal.fire({
        ...getFuturisticSwalOptions('Autorizační Error'),
        icon: 'error',
        text: 'Nemáte oprávnění k přístupu k tomuto zdroji nebo vaše relace vypršela.',
      })
      return []
    }

    // Handle general errors
    if (!response.ok) {
      const errorData = await response
        .json()
        .catch(() => ({ message: `Server error: ${response.statusText}` }))
      console.error('getAllUsers error:', errorData)
      Swal.fire({
        ...getFuturisticSwalOptions('Error'),
        icon: 'error',
        text: errorData.message || 'Nepodařilo se načíst uživatele.',
      })
      return []
    }
    const users: UserInfo[] = await response.json()
    return users
  } catch (error) {
    console.error('getAllUsers API error:', error)
    Swal.fire({
      ...getFuturisticSwalOptions('API Error'),
      icon: 'error',
      text: 'Při komunikaci se serverem došlo k chybě..',
    })
    return []
  }
}

/**
 * Updates roles for a specific user
 * @param userId - The ID of the user to update
 * @param roles - Array of role names to assign
 * @returns Promise resolving to success status
 */
export const updateUserRoles = async (userId: string, roles: string[]): Promise<boolean> => {
  const authStore = useAuthStore()
  if (!authStore.token) {
    Swal.fire((getFuturisticSwalOptions('Error').text = 'Chybí token!'))
    return false
  }

  try {
    const response = await fetch(`${API_BASE_URL}/user/${userId}/roles`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(roles),
    })

    const data: ApiResponse = await response.json()

    if (!response.ok || !data.isSuccess) {
      Swal.fire({
        ...getFuturisticSwalOptions('Úprava role Error'),
        icon: 'error',
        text: data.message || 'Nepodařilo se aktualizovat uživatelské role.',
      })
      return false
    }
    Swal.fire({
      ...getFuturisticSwalOptions('Úspěch!'),
      icon: 'success',
      text: data.message || 'Uživatelské role byly úspěšně aktualizovány.',
      timer: 2000,
      showConfirmButton: false,
    })
    return true
  } catch (error) {
    console.error('updateUserRoles API error:', error)
    Swal.fire(
      (getFuturisticSwalOptions('API Error').text =
        'Při komunikaci se serverem došlo k chybě.'),
    )
    return false
  }
}

/**
 * Deletes a user from the system after confirmation
 * @param userId - ID of the user to delete
 * @returns Promise resolving to success status
 */
export const deleteUser = async (userId: string): Promise<boolean> => {
  const authStore = useAuthStore()
  if (!authStore.token) {
    Swal.fire((getFuturisticSwalOptions('Error').text = 'Chybí token.'))
    return false
  }

  try {
    const response = await fetch(`${API_BASE_URL}/user/${userId}`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${authStore.token}`,
      },
    })
    const data: ApiResponse = await response.json()
    if (!response.ok || !data.isSuccess) {
      Swal.fire((getFuturisticSwalOptions('Error').text = data.message || 'Nepodařilo se odstranit uživatele.'))
      return false
    }
    Swal.fire(
      (getFuturisticSwalOptions('Úspěch!').text =
        data.message || 'Uživatel byl úspěšně odstraněn.'),
    )
    return true
  } catch (error) {
    console.error('deleteUser API error:', error)
    Swal.fire(
      (getFuturisticSwalOptions('API Error').text =
        'Při komunikaci se serverem došlo k chybě.'),
    )
    return false
  }
}
