// src/services/pollService.ts
import { useAuthStore } from '@/stores/authStore';
import Swal, { type SweetAlertOptions } from 'sweetalert2';

const API_BASE_URL = 'http://localhost:5207/api/polls';

// Upraveno: PollOption může mít voteCount (přichází z API)
export interface PollOption {
  optionId: string;
  text: string;
  imageUrl?: string;
  voteCount?: number;
}

// Rozhraní pro DTO posílané při úpravě, každá možnost má potenciálně ID
export interface UpdatePollOptionPayload {
    optionId?: string; // Bude přítomno pro existující, chybí pro nové
    text: string;
    imageUrl?: string;
}

export interface Poll {
  pollId: string;
  question: string;
  endTime: string;
  isClosed: boolean;
  imageUrl?: string;
  creatorUserId: string;
  creatorNickname?: string;
  options: PollOption[];
  isMultipleChoice: boolean;
  userVotedOptionIds?: string[];
  totalVotes?: number;
}

export interface CreatePollDto {
  question: string;
  endTime: string;
  imageUrl?: string;
  options: Array<{ text: string; imageUrl?: string }>; // Nové možnosti nemají ID
  isMultipleChoice: boolean;
}

// Upraveno: UpdatePollDto nyní obsahuje pole UpdatePollOptionPayload
export interface UpdatePollDto {
  question: string;
  endTime: string;
  imageUrl?: string;
  options: UpdatePollOptionPayload[]; // Seznam možností k aktualizaci/přidání/smazání
  // isMultipleChoice se nemění při úpravě
}

export interface SubmitVoteDto {
  optionIds: string[];
}

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
});

export const getAllPolls = async (): Promise<Poll[]> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    console.error('getAllPolls: Chybí autentizační token.');
    return [];
  }
  try {
    const response = await fetch(API_BASE_URL, {
      headers: { Authorization: `Bearer ${authStore.token}` },
    });
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: `Chyba serveru: ${response.statusText}` }));
      throw new Error(errorData.message || 'Nepodařilo se načíst ankety.');
    }
    return await response.json();
  } catch (error: any) {
    console.error('getAllPolls API error:', error);
    Swal.fire({ ...getFuturisticSwalOptions('Chyba načítání anket'), text: error.message, icon: 'error' });
    return [];
  }
};

export const getPollById = async (pollId: string): Promise<Poll | null> => {
  const authStore = useAuthStore();
   if (!authStore.token) return null;
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}`, {
      headers: { Authorization: `Bearer ${authStore.token}` },
    });
    if (!response.ok) throw new Error('Nepodařilo se načíst detail ankety.');
    return await response.json();
  } catch (error: any) {
    console.error(`getPollById (${pollId}) API error:`, error);
    Swal.fire({ ...getFuturisticSwalOptions('Chyba'), text: error.message, icon: 'error' });
    return null;
  }
};

export const createPoll = async (pollData: CreatePollDto): Promise<Poll | null> => {
  const authStore = useAuthStore();
  if (!authStore.token) return null;
  try {
    const response = await fetch(API_BASE_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authStore.token}`,
      },
      body: JSON.stringify(pollData),
    });
    if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Nepodařilo se vytvořit anketu.' }));
        throw new Error(errorData.message || 'Nepodařilo se vytvořit anketu.');
    }
    Swal.fire({ ...getFuturisticSwalOptions('Úspěch!'), text: 'Anketa byla úspěšně vytvořena.', icon: 'success', timer: 2000, showConfirmButton: false });
    return await response.json();
  } catch (error: any) {
    console.error('createPoll API error:', error);
    Swal.fire({ ...getFuturisticSwalOptions('Chyba vytváření ankety'), text: error.message, icon: 'error' });
    return null;
  }
};

export const updatePoll = async (pollId: string, pollData: UpdatePollDto): Promise<Poll | null> => {
  const authStore = useAuthStore();
  if (!authStore.token) return null;
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authStore.token}`,
      },
      body: JSON.stringify(pollData),
    });
    if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Nepodařilo se aktualizovat anketu.' }));
        throw new Error(errorData.message || 'Nepodařilo se aktualizovat anketu.');
    }
    Swal.fire({ ...getFuturisticSwalOptions('Úspěch!'), text: 'Anketa byla úspěšně aktualizována.', icon: 'success', timer: 2000, showConfirmButton: false });
    return await response.json(); // Očekáváme, že API vrátí aktualizovanou anketu
  } catch (error: any) {
    console.error(`updatePoll (${pollId}) API error:`, error);
    Swal.fire({ ...getFuturisticSwalOptions('Chyba aktualizace ankety'), text: error.message, icon: 'error' });
    return null;
  }
};

export const deletePoll = async (pollId: string): Promise<boolean> => {
  const authStore = useAuthStore();
  if (!authStore.token) return false;
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${authStore.token}` },
    });
    if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Nepodařilo se smazat anketu.' }));
        throw new Error(errorData.message || 'Nepodařilo se smazat anketu.');
    }
    Swal.fire({ ...getFuturisticSwalOptions('Smazáno!'), text: 'Anketa byla úspěšně smazána.', icon: 'success', timer: 2000, showConfirmButton: false });
    return true;
  } catch (error: any) {
    console.error(`deletePoll (${pollId}) API error:`, error);
    Swal.fire({ ...getFuturisticSwalOptions('Chyba smazání ankety'), text: error.message, icon: 'error' });
    return false;
  }
};

export const submitVote = async (pollId: string, voteData: SubmitVoteDto): Promise<Poll | null> => {
  const authStore = useAuthStore();
  if (!authStore.token) return null;
  try {
    const response = await fetch(`${API_BASE_URL}/${pollId}/vote`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${authStore.token}`,
      },
      body: JSON.stringify(voteData),
    });
    if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Nepodařilo se odeslat hlas.' }));
        throw new Error(errorData.message || 'Nepodařilo se odeslat hlas.');
    }
    Swal.fire({ ...getFuturisticSwalOptions('Hlasováno!'), text: 'Váš hlas byl úspěšně zaznamenán.', icon: 'success', timer: 2000, showConfirmButton: false });
    return await response.json();
  } catch (error: any) {
    console.error(`submitVote (${pollId}) API error:`, error);
    Swal.fire({ ...getFuturisticSwalOptions('Chyba hlasování'), text: error.message, icon: 'error' });
    return null;
  }
};
