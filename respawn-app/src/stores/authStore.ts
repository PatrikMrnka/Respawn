import { defineStore } from 'pinia';
import Swal from 'sweetalert2'; // For potential notifications within the store

// Define the structure of UserInfo based on your AuthResponseDto.UserDto
interface UserInfo {
  id: string;
  nickname: string;
  email: string;
  avatarUrl?: string;
  roles: string[];
}

interface AuthState {
  token: string | null;
  user: UserInfo | null;
  isAuthenticated: boolean;
  expiresAt: Date | null;
}

// Helper function to apply futuristic theme to SweetAlert2 (can be moved to a shared utility)
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
  };
};


export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem('authToken') || null,
    user: JSON.parse(localStorage.getItem('authUser') || 'null'),
    isAuthenticated: !!localStorage.getItem('authToken'),
    expiresAt: localStorage.getItem('authExpiresAt') ? new Date(localStorage.getItem('authExpiresAt')!) : null,
  }),
  getters: {
    isLoggedIn: (state) => state.isAuthenticated && state.token,
    currentUser: (state) => state.user,
    getToken: (state) => state.token,
    userInitials: (state) => {
      if (state.user && state.user.nickname) {
        return state.user.nickname.substring(0, 2).toUpperCase();
      }
      return '??';
    },
  },
  actions: {
    setAuthData(token: string, user: UserInfo, expiresAt: string | Date) {
      this.token = token;
      this.user = user;
      this.isAuthenticated = true;
      this.expiresAt = new Date(expiresAt);

      localStorage.setItem('authToken', token);
      localStorage.setItem('authUser', JSON.stringify(user));
      localStorage.setItem('authExpiresAt', this.expiresAt.toISOString());
    },
    clearAuthData() {
      this.token = null;
      this.user = null;
      this.isAuthenticated = false;
      this.expiresAt = null;

      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
      localStorage.removeItem('authExpiresAt');
    },
    async logout() {
      this.clearAuthData();
      // Zde by mohlo být volání API pro invalidaci tokenu na serveru, pokud je implementováno
      Swal.fire({
        ...getFuturisticSwalOptions('Odhlášení'),
        icon: 'success',
        text: 'Byli jste úspěšně odhlášeni.',
        timer: 1500,
        showConfirmButton: false,
      });
      // Přesměrování může být řešeno v komponentě, která volá logout
    },
    // Akce pro kontrolu expirace tokenu (volatelná při startu aplikace)
    checkTokenExpiration() {
        if (this.expiresAt && new Date() > this.expiresAt) {
            this.clearAuthData();
            console.warn('Auth token expired, user logged out.');
        }
    }
  },
});
