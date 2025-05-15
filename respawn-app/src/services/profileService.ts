// src/services/profileService.ts
import { useAuthStore } from '@/stores/authStore';
import Swal from 'sweetalert2';

const API_BASE_URL = 'http://localhost:5207/api/userprofile'; // Základní URL pro UserProfileController

// Helper pro SweetAlert2 (může být v shared utils)
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
      confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
    },
    buttonsStyling: false,
  };
};

interface UserProfileData {
  id: string;
  nickname: string;
  email: string;
  avatarUrl?: string;
  roles: string[];
}

interface UpdateUserProfilePayload {
  nickname: string;
  avatarUrl?: string;
}

interface UpdateResponse {
    message: string;
    userInfo: UserProfileData;
}


export const fetchUserProfile = async (): Promise<UserProfileData | null> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    console.error("FetchUserProfile: No auth token found.");
    return null;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/me`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
    });

    if (response.status === 401) {
        authStore.logout(); // Token je neplatný nebo vypršel
        Swal.fire({
            ...getFuturisticSwalOptions('Chyba autorizace'),
            icon: 'error',
            text: 'Vaše přihlášení vypršelo. Přihlaste se prosím znovu.',
        });
        return null;
    }

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: `Chyba serveru: ${response.statusText}` }));
      console.error("FetchUserProfile error:", errorData);
      Swal.fire({
        ...getFuturisticSwalOptions('Chyba'),
        icon: 'error',
        text: errorData.message || 'Nepodařilo se načíst profil.',
      });
      return null;
    }

    const data: UserProfileData = await response.json();
    // Aktualizujeme data ve store, pokud se liší (např. po externí změně)
    if (authStore.user?.nickname !== data.nickname || authStore.user?.avatarUrl !== data.avatarUrl) {
        authStore.user = { ...authStore.user, ...data }; // Sloučíme, abychom nepřepsali ID a role, pokud nejsou v odpovědi
        localStorage.setItem('authUser', JSON.stringify(authStore.user));
    }
    return data;
  } catch (error) {
    console.error('FetchUserProfile API error:', error);
    Swal.fire({
        ...getFuturisticSwalOptions('Chyba'),
        icon: 'error',
        text: 'Došlo k chybě při komunikaci se serverem.',
    });
    return null;
  }
};

export const updateUserProfile = async (payload: UpdateUserProfilePayload): Promise<UserProfileData | null> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    console.error("UpdateUserProfile: No auth token found.");
    return null;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/me`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });

    if (response.status === 401) {
        authStore.logout();
        Swal.fire({
            ...getFuturisticSwalOptions('Chyba autorizace'),
            icon: 'error',
            text: 'Vaše přihlášení vypršelo. Přihlaste se prosím znovu.',
        });
        return null;
    }
    
    const data: UpdateResponse = await response.json();

    if (!response.ok) {
      console.error("UpdateUserProfile error:", data);
      Swal.fire({
        ...getFuturisticSwalOptions('Chyba aktualizace'),
        icon: 'error',
        text: data.message || 'Nepodařilo se aktualizovat profil.',
      });
      return null;
    }
    
    // Aktualizuj uživatele ve store
    authStore.setAuthData(authStore.token, data.userInfo, authStore.expiresAt || new Date()); // Použijeme stávající token a expiraci
    
    Swal.fire({
        ...getFuturisticSwalOptions('Úspěch!'),
        icon: 'success',
        text: data.message || 'Profil byl úspěšně aktualizován.',
        timer: 2000,
        showConfirmButton: false,
    });
    return data.userInfo;
  } catch (error) {
    console.error('UpdateUserProfile API error:', error);
     Swal.fire({
        ...getFuturisticSwalOptions('Chyba'),
        icon: 'error',
        text: 'Došlo k chybě při komunikaci se serverem.',
    });
    return null;
  }
};
