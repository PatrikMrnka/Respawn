// File: haha/respawn-app/src/services/gameServerService.ts
import { useAuthStore } from '@/stores/authStore';
import type { GameServerDto } from '@/views/ServersView.vue';
import Swal from 'sweetalert2';

// Frontend DTOs (mirroring backend DTOs)
export interface PlayerDetailDtoFE {
  name: string;
  score: number;
  duration: number;
}

export interface GameServerDetailDtoFE extends GameServerDto { // Extends the basic DTO from ServersView
  gameName?: string;
  mapName?: string;
  maxPlayers?: number;
  currentPlayers?: number;
  isVacSecured?: boolean;
  players: PlayerDetailDtoFE[];
}


const API_BASE_URL = 'http://localhost:5207/api/gameservers';

const getFuturisticSwalOptions = (title: string, text: string = '', icon: 'success' | 'error' | 'warning' | 'info' | 'question' = 'info') => {
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
  };
};

export const getGameServerDetails = async (serverId: string): Promise<GameServerDetailDtoFE | null> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    console.error('getGameServerDetails: Chybí autentizační token.');
    Swal.fire(getFuturisticSwalOptions('Chyba autentizace', 'Pro zobrazení detailů serveru je nutné přihlášení.', 'error'));
    return null;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/${serverId}/details`, {
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
    });

    if (response.status === 401) {
      authStore.logout(); // Or handle re-authentication
      Swal.fire(getFuturisticSwalOptions('Chyba autorizace', 'Vaše přihlášení vypršelo. Přihlaste se prosím znovu.', 'error'));
      return null;
    }
    if (response.status === 404) {
      Swal.fire(getFuturisticSwalOptions('Server nenalezen', 'Požadovaný herní server nebyl nalezen.', 'error'));
      return null;
    }
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: `Chyba serveru: ${response.statusText}` }));
      throw new Error(errorData.message || 'Nepodařilo se načíst detaily serveru.');
    }
    
    const data: GameServerDetailDtoFE = await response.json();
    return data;

  } catch (error: any) {
    console.error(`getGameServerDetails (${serverId}) API error:`, error);
    Swal.fire(getFuturisticSwalOptions('Chyba načítání', error.message || 'Došlo k chybě při komunikaci se serverem.', 'error'));
    return null;
  }
};

// Add other game server related service functions here if needed (e.g., for start, stop, delete if not in dockerAdminService)
// For now, dockerAdminService handles start/stop/delete of containers, which is linked to game servers.
// The GameServersController on backend handles the logic of mapping these actions to game servers.
// So, calls for start/stop/delete from ServersView.txt can remain as they are,
// or be refactored into this service if preferred for consistency.
