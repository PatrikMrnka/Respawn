import Swal, { type SweetAlertOptions } from 'sweetalert2'
import { useAuthStore } from '@/stores/authStore'

// Base URL for authentication API endpoints
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL + '/api/auth'

/**
 * Returns customized SweetAlert2 options with futuristic styling
 * @param title - The title to display in the alert
 * @returns SweetAlertOptions configuration object
 */
const getFuturisticSwalOptions = (title: string): SweetAlertOptions => {
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
      validationMessage: 'futuristic-swal-validation-message font-inter',
    },
    buttonsStyling: false,
    allowEnterKey: true,
    heightAuto: false,
  }
}

/**
 * Interface for the authentication response from the API
 */
interface AuthResponse {
  token: string
  isSuccess: boolean
  message: string
  userInfo: {
    id: string
    nickname: string
    email: string
    avatarUrl?: string
    roles: string[]
  }
  expiresAt: string
}

/**
 * Displays a login modal and handles the authentication process
 * @returns Object with success status and user nickname if login was successful
 */
export const displayLoginModal = async (): Promise<{
  success: boolean
  nickname?: string
} | null> => {
  const authStore = useAuthStore()

  const { value: formValues, isConfirmed } = await Swal.fire({
    ...getFuturisticSwalOptions('Přihlášení'),
    html: `
      <div class="swal-form-container">
        <label for="swal-nickname" class="swal-label font-inter">Přezdívka</label>
        <input id="swal-nickname" class="swal2-input futuristic-swal-input" placeholder="TvojePrezdivka" autocomplete="username">

        <label for="swal-password" class="swal-label font-inter">Heslo</label>
        <input id="swal-password" type="password" class="swal2-input futuristic-swal-input" placeholder="•••••" autocomplete="current-password">
      </div>
    `,
    focusConfirm: false,
    showCancelButton: true,
    confirmButtonText: 'Přihlásit se',
    cancelButtonText: 'Zrušit',
    allowOutsideClick: () => !Swal.isLoading(),
    showLoaderOnConfirm: true,
    // Set up input focus and Enter key handling for form inputs
    didOpen: () => {
      const nicknameInput = document.getElementById('swal-nickname') as HTMLInputElement
      const passwordInput = document.getElementById('swal-password') as HTMLInputElement

      nicknameInput?.focus()

      const inputs = [nicknameInput, passwordInput]
      inputs.forEach((input) => {
        input?.addEventListener('keypress', (event) => {
          if (event.key === 'Enter') {
            event.preventDefault()
            Swal.clickConfirm()
          }
        })
      })
    },
    // Validate form data and send login request to API
    preConfirm: async () => {
      const nicknameInput = document.getElementById('swal-nickname') as HTMLInputElement
      const passwordInput = document.getElementById('swal-password') as HTMLInputElement
      if (!nicknameInput || !passwordInput) {
        Swal.showValidationMessage('Chyba pri nacitani formulare')
        return false
      }
      const nickname = nicknameInput.value
      const password = passwordInput.value
      if (!nickname || !password) {
        Swal.showValidationMessage('Prosim, vyplnte prezdivku i heslo')
        return false
      }

      try {
        const response = await fetch(`${API_BASE_URL}/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ nickname, password }),
        })
        const data: AuthResponse = await response.json()
        if (!response.ok || !data.isSuccess) {
          Swal.showValidationMessage(data.message || `Chyba: ${response.statusText}`)
          return false
        }
        return data
      } catch (error) {
        console.error('Login API chyba:', error)
        Swal.showValidationMessage('Došlo k chybě při komunikaci se serverem.')
        return false
      }
    },
  })

  // Handle successful login by storing auth data and showing success message
  if (isConfirmed && formValues) {
    const authData = formValues as AuthResponse
    authStore.setAuthData(authData.token, authData.userInfo, authData.expiresAt)
    Swal.fire({
      ...getFuturisticSwalOptions('Úspěch!!'),
      icon: 'success',
      text: `Vitej zpet, ${authData.userInfo.nickname}!`,
      timer: 2000,
      showConfirmButton: false,
    })
    return { success: true, nickname: authData.userInfo.nickname }
  }
  return null
}

/**
 * Displays a registration modal and handles the user registration process
 * @returns Object with success status and user nickname if registration was successful
 */
export const displayRegisterModal = async (): Promise<{
  success: boolean
  nickname?: string
} | null> => {
  const authStore = useAuthStore()

  const { value: formValues, isConfirmed } = await Swal.fire({
    ...getFuturisticSwalOptions('Registrace'),
    html: `
      <div class="swal-form-container">
        <label for="swal-reg-nickname" class="swal-label font-inter">Přezdívka</label>
        <input id="swal-reg-nickname" class="swal2-input futuristic-swal-input" placeholder="TvojePrezdivka" autocomplete="username">

        <label for="swal-reg-email" class="swal-label font-inter">Email</label>
        <input id="swal-reg-email" type="email" class="swal2-input futuristic-swal-input" placeholder="email@example.com" autocomplete="email">

        <label for="swal-reg-password" class="swal-label font-inter">Heslo</label>
        <input id="swal-reg-password" type="password" class="swal2-input futuristic-swal-input" placeholder="Min. 5 znaku, cislo, male pismeno" autocomplete="new-password">

        <label for="swal-reg-confirm-password" class="swal-label font-inter">Potvrzení hesla</label>
        <input id="swal-reg-confirm-password" type="password" class="swal2-input futuristic-swal-input" placeholder="•••••" autocomplete="new-password">
      </div>
    `,
    focusConfirm: false,
    showCancelButton: true,
    confirmButtonText: 'Zaregistrovat se',
    cancelButtonText: 'Zrušit',
    allowOutsideClick: () => !Swal.isLoading(),
    showLoaderOnConfirm: true,
    // Set up input focus and Enter key handling for form inputs
    didOpen: () => {
      const nicknameInput = document.getElementById('swal-reg-nickname') as HTMLInputElement
      const emailInput = document.getElementById('swal-reg-email') as HTMLInputElement
      const passwordInput = document.getElementById('swal-reg-password') as HTMLInputElement
      const confirmPasswordInput = document.getElementById(
        'swal-reg-confirm-password',
      ) as HTMLInputElement

      nicknameInput?.focus()

      const inputs = [nicknameInput, emailInput, passwordInput, confirmPasswordInput]
      inputs.forEach((input) => {
        input?.addEventListener('keypress', (event) => {
          if (event.key === 'Enter') {
            event.preventDefault()
            Swal.clickConfirm()
          }
        })
      })
    },
    // Validate registration data, perform client-side validation, and send request to API
    preConfirm: async () => {
      const nicknameInput = document.getElementById('swal-reg-nickname') as HTMLInputElement
      const emailInput = document.getElementById('swal-reg-email') as HTMLInputElement
      const passwordInput = document.getElementById('swal-reg-password') as HTMLInputElement
      const confirmPasswordInput = document.getElementById(
        'swal-reg-confirm-password',
      ) as HTMLInputElement

      if (!nicknameInput || !emailInput || !passwordInput || !confirmPasswordInput) {
        Swal.showValidationMessage('Chyba pri nacitani formulare')
        return false
      }
      const nickname = nicknameInput.value
      const email = emailInput.value
      const password = passwordInput.value
      const confirmPassword = confirmPasswordInput.value

      if (!nickname || !email || !password || !confirmPassword) {
        Swal.showValidationMessage('Prosim, vyplnte vsechna pole')
        return false
      }
      // Basic client-side password validation
      if (password.length < 5) {
        Swal.showValidationMessage('Heslo musi mit alespon 5 znaku.')
        return false
      }
      if (!/\d/.test(password)) {
        Swal.showValidationMessage('Heslo musi obsahovat alespon jednu cislici.')
        return false
      }
      if (!/[a-z]/.test(password)) {
        Swal.showValidationMessage('Heslo musi obsahovat alespon jedno male pismeno.')
        return false
      }

      if (password !== confirmPassword) {
        Swal.showValidationMessage('Hesla se neshoduji')
        passwordInput.value = ''
        confirmPasswordInput.value = ''
        passwordInput.focus()
        return false
      }

      // Send registration request to API
      try {
        const response = await fetch(`${API_BASE_URL}/register`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ nickname, email, password, confirmPassword }),
        })
        const data: AuthResponse = await response.json()
        if (!response.ok || !data.isSuccess) {
          Swal.showValidationMessage(data.message || `Chyba: ${response.statusText}`)
          return false
        }
        return data
      } catch (error) {
        console.error('Register API chyba:', error)
        Swal.showValidationMessage('Došlo k chybě při komunikaci se serverem.')
        return false
      }
    },
  })

  // Handle successful registration by storing auth data and showing success message
  if (isConfirmed && formValues) {
    const authData = formValues as AuthResponse
    if (authData.token && authData.userInfo) {
      authStore.setAuthData(authData.token, authData.userInfo, authData.expiresAt)
    }
    Swal.fire({
      ...getFuturisticSwalOptions('Registrace uspesna!'),
      icon: 'success',
      text: authData.message || `Vitej, ${authData.userInfo.nickname}! Byl jsi zaregistrovan.`,
      timer: 2500,
      showConfirmButton: false,
    })
    return { success: true, nickname: authData.userInfo.nickname }
  }
  return null
}
