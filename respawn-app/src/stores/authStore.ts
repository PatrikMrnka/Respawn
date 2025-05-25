import { defineStore } from 'pinia'
import Swal from 'sweetalert2'

export interface UserInfo {
  id: string
  nickname: string
  email: string
  avatarUrl?: string
  roles: string[]
}

interface AuthState {
  token: string | null
  user: UserInfo | null
  isAuthenticated: boolean
  expiresAt: Date | null
}

const getFuturisticSwalOptions = (title: string) => {
  return {
    titleText: title,
    background: '#1A2033',
    color: '#E0E0E0',
    confirmButtonColor: '#00E0FF',
    cancelButtonColor: '#FF5252',
    customClass: {
      popup: 'futuristic-swal-popup',
      title: 'futuristic-swal-title font-oxanium',
      htmlContainer: 'futuristic-swal-html-container font-inter',
      input: 'futuristic-swal-input',
      confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
      cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
      actions: 'futuristic-swal-actions',
    },
    buttonsStyling: false,
  }
}

export const useAuthStore = defineStore('auth', {
  /**
   * Authentication state
   * @returns Initial authentication state with data from localStorage
   */
  state: (): AuthState => ({
    /** Authentication token */
    token: localStorage.getItem('authToken') || null,
    /** User information */
    user: JSON.parse(localStorage.getItem('authUser') || 'null'),
    /** Flag indicating if user is authenticated */
    isAuthenticated: !!localStorage.getItem('authToken'),
    /** Token expiration date */
    expiresAt: localStorage.getItem('authExpiresAt')
      ? new Date(localStorage.getItem('authExpiresAt')!)
      : null,
  }),
  
  /**
   * Authentication getters
   */
  getters: {
    /** 
     * Checks if user is currently logged in
     * @returns Boolean indicating login status
     */
    isLoggedIn: (state) => state.isAuthenticated && state.token,
    
    /**
     * Gets current user information
     * @returns User object or null if not authenticated
     */
    currentUser: (state) => state.user,
    
    /**
     * Gets authentication token
     * @returns Current authentication token or null
     */
    getToken: (state) => state.token,
    
    /**
     * Generates user initials from nickname
     * @returns Two uppercase letters or '??' if no nickname available
     */
    userInitials: (state) => {
      if (state.user && state.user.nickname) {
        return state.user.nickname.substring(0, 2).toUpperCase()
      }
      return '??'
    },
  },
  
  /**
   * Authentication actions
   */
  actions: {
    /**
     * Sets authentication data after successful login
     * @param token - Authentication token
     * @param user - User information
     * @param expiresAt - Token expiration date
     */
    setAuthData(token: string, user: UserInfo, expiresAt: string | Date) {
      this.token = token
      this.user = user
      this.isAuthenticated = true
      this.expiresAt = new Date(expiresAt)

      localStorage.setItem('authToken', token)
      localStorage.setItem('authUser', JSON.stringify(user))
      localStorage.setItem('authExpiresAt', this.expiresAt.toISOString())
    },
    clearAuthData() {
      this.token = null
      this.user = null
      this.isAuthenticated = false
      this.expiresAt = null

      localStorage.removeItem('authToken')
      localStorage.removeItem('authUser')
      localStorage.removeItem('authExpiresAt')
    },
    async logout() {
      this.clearAuthData()
      Swal.fire({
        ...getFuturisticSwalOptions('Odhlášení'),
        icon: 'success',
        text: 'Byli jste úspěšně odhlášeni.',
        timer: 1500,
        showConfirmButton: false,
      })
    },
    // Actions to check token expiration and handle automatic logout
    checkTokenExpiration() {
      if (this.expiresAt && new Date() > this.expiresAt) {
        this.clearAuthData()
        console.warn('Auth token expired, user logged out.')
      }
    },
  },
})