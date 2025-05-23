// src/services/adminService.ts
import { useAuthStore } from '@/stores/authStore';
import Swal, { type SweetAlertOptions } from 'sweetalert2';
import type { UserInfo } from '@/stores/authStore'; // Assuming UserInfo is exported or define it here

const API_BASE_URL = 'http://localhost:5207/api/admin'; // Předpokládaná URL pro admin API

const getFuturisticSwalOptions = (title: string): SweetAlertOptions => {
  return {
    titleText: title,
    background: '#1A2033', // Tmavě modré pozadí
    color: '#E0E0E0', // Světlý text
    confirmButtonColor: '#00E0FF', // Tyrkysové potvrzovací tlačítko
    cancelButtonColor: '#FF5252', // Červené tlačítko zrušení
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
  };
};

interface AdminUserDto extends UserInfo {
  // Můžete rozšířit o další pole specifická pro admin výpis, pokud API vrací více
  // Například:
  // lastLogin?: string;
  // isLockedOut?: boolean;
}

interface ApiResponse {
  isSuccess: boolean;
  message: string;
  data?: any; // Pro obecná data, pokud API vrací
}


export const getAllUsers = async (): Promise<AdminUserDto[]> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    console.error('getAllUsers: Chybi autentizacni token.');
    Swal.fire(getFuturisticSwalOptions('Chyba').text = 'Chybi autentizacni token.');
    return [];
  }

  try {
    const response = await fetch(`${API_BASE_URL}/users`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
    });

    if (response.status === 401 || response.status === 403) {
      authStore.logout();
      Swal.fire({
        ...(getFuturisticSwalOptions('Chyba autorizace')),
        icon: 'error',
        text: 'Nemate opravneni k pristupu nebo vase prihlaseni vyprselo.',
      });
      return [];
    }

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: `Chyba serveru: ${response.statusText}` }));
      console.error('getAllUsers chyba:', errorData);
      Swal.fire({
        ...(getFuturisticSwalOptions('Chyba')),
        icon: 'error',
        text: errorData.message || 'Nepodarilo se nacist uzivatele.',
      });
      return [];
    }
    // Předpokládáme, že API vrací pole uživatelů přímo
    const users: AdminUserDto[] = await response.json();
    return users;
  } catch (error) {
    console.error('getAllUsers API chyba:', error);
    Swal.fire({
      ...(getFuturisticSwalOptions('Chyba API')),
      icon: 'error',
      text: 'Doslo k chybe pri komunikaci se serverem.',
    });
    return [];
  }
};

export const updateUserRoles = async (userId: string, roles: string[]): Promise<boolean> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    Swal.fire(getFuturisticSwalOptions('Chyba').text = 'Chybi autentizacni token.');
    return false;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/user/${userId}/roles`, { // Upraveno API endpoint
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(roles), // Odesíláme pole rolí
    });

    const data: ApiResponse = await response.json();

    if (!response.ok || !data.isSuccess) {
      Swal.fire({
        ...(getFuturisticSwalOptions('Chyba aktualizace roli')),
        icon: 'error',
        text: data.message || 'Nepodarilo se aktualizovat role uzivatele.',
      });
      return false;
    }
    Swal.fire({
      ...(getFuturisticSwalOptions('Uspech')),
      icon: 'success',
      text: data.message || 'Role uzivatele byly uspesne aktualizovany.',
      timer: 2000,
      showConfirmButton: false,
    });
    return true;
  } catch (error) {
    console.error('updateUserRoles API chyba:', error);
    Swal.fire(getFuturisticSwalOptions('Chyba API').text = 'Doslo k chybe pri komunikaci se serverem.');
    return false;
  }
};


export const deleteUser = async (userId: string): Promise<boolean> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    Swal.fire(getFuturisticSwalOptions('Chyba').text = 'Chybi autentizacni token.');
    return false;
  }

  const result = await Swal.fire({
    ...getFuturisticSwalOptions('Potvrdit smazani'),
    text: `Opravdu chcete smazat uzivatele s ID: ${userId}? Tato akce je nevratna.`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Ano, smazat',
    cancelButtonText: 'Zrusit',
  });

  if (!result.isConfirmed) {
    return false;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/user/${userId}`, { // Upraveno API endpoint
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
      },
    });
    const data: ApiResponse = await response.json();
    if (!response.ok || !data.isSuccess) {
      Swal.fire(getFuturisticSwalOptions('Chyba').text = data.message || 'Nepodarilo se smazat uzivatele.');
      return false;
    }
    Swal.fire(getFuturisticSwalOptions('Uspech').text = data.message || 'Uzivatel byl uspesne smazan.');
    return true;
  } catch (error) {
    console.error('deleteUser API chyba:', error);
    Swal.fire(getFuturisticSwalOptions('Chyba API').text = 'Doslo k chybe pri komunikaci se serverem.');
    return false;
  }
};

export const resetDatabase = async (): Promise<boolean> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    Swal.fire(getFuturisticSwalOptions('Chyba').text = 'Chybi autentizacni token.');
    return false;
  }

  const { value: confirmText } = await Swal.fire({
    ...getFuturisticSwalOptions('POTVRDIT RESET DATABAZE'),
    html: `
      <p class="text-red-400 font-bold">TATO AKCE JE EXTREMNE NEBEZPECNA A NEVRATNA!</p>
      <p>Pro potvrzeni resetu databaze napiste "RESETDB" do pole nize:</p>
    `,
    input: 'text',
    inputPlaceholder: 'RESETDB',
    icon: 'error',
    showCancelButton: true,
    confirmButtonText: 'Resetovat databazi',
    cancelButtonText: 'Zrusit',
    inputValidator: (value) => {
      if (value !== 'RESETDB') {
        return 'Pro potvrzeni musite napsat RESETDB';
      }
      return null;
    }
  });

  if (confirmText !== 'RESETDB') {
    return false;
  }

  try {
    Swal.fire({
        ...getFuturisticSwalOptions('Probíhá reset...'),
        text: 'Databáze se resetuje, prosím čekejte.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    const response = await fetch(`${API_BASE_URL}/database/reset`, {
      method: 'POST', // Nebo DELETE, podle vaší API specifikace
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
      },
    });
    const data: ApiResponse = await response.json();
    Swal.close();
    if (!response.ok || !data.isSuccess) {
      Swal.fire(getFuturisticSwalOptions('Chyba').text = data.message || 'Nepodarilo se resetovat databazi.');
      return false;
    }
    Swal.fire(getFuturisticSwalOptions('Uspech').text = data.message || 'Databaze byla uspesne resetovana.');
    return true;
  } catch (error) {
    Swal.close();
    console.error('resetDatabase API chyba:', error);
    Swal.fire(getFuturisticSwalOptions('Chyba API').text = 'Doslo k chybe pri komunikaci se serverem.');
    return false;
  }
};

export const deleteDatabase = async (): Promise<boolean> => {
  const authStore = useAuthStore();
  if (!authStore.token) {
    Swal.fire(getFuturisticSwalOptions('Chyba').text = 'Chybi autentizacni token.');
    return false;
  }

   const { value: confirmText } = await Swal.fire({
    ...getFuturisticSwalOptions('POTVRDIT SMAZANI DATABAZE'),
    html: `
      <p class="text-red-500 font-bold text-xl">!!! EXTREMNE NEBEZPECNA AKCE !!!</p>
      <p class="text-red-400">Tato akce kompletne a nevratne smaze celou databazi!</p>
      <p>Pro potvrzeni smazani databaze napiste "SMAZATDB" do pole nize:</p>
    `,
    input: 'text',
    inputPlaceholder: 'SMAZATDB',
    icon: 'error',
    showCancelButton: true,
    confirmButtonText: 'Ano, SMAZAT DATABAZI',
    confirmButtonColor: '#d33',
    cancelButtonText: 'Zrusit',
    inputValidator: (value) => {
      if (value !== 'SMAZATDB') {
        return 'Pro potvrzeni musite napsat SMAZATDB';
      }
      return null;
    }
  });

  if (confirmText !== 'SMAZATDB') {
    return false;
  }

  try {
    Swal.fire({
        ...getFuturisticSwalOptions('Probíhá mazání...'),
        text: 'Databáze se maže, prosím čekejte.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    const response = await fetch(`${API_BASE_URL}/database/delete-all`, { // Upraveno API endpoint
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${authStore.token}`,
      },
    });
    const data: ApiResponse = await response.json();
    Swal.close();
    if (!response.ok || !data.isSuccess) {
      Swal.fire(getFuturisticSwalOptions('Chyba').text = data.message || 'Nepodarilo se smazat databazi.');
      return false;
    }
    Swal.fire(getFuturisticSwalOptions('Uspech').text = data.message || 'Databaze byla uspesne smazana.');
    // Po smazání databáze by se měl administrátor pravděpodobně odhlásit nebo aplikace restartovat
    authStore.logout();
    // router.push('/'); // nebo jiná vhodná akce
    return true;
  } catch (error) {
    Swal.close();
    console.error('deleteDatabase API chyba:', error);
    Swal.fire(getFuturisticSwalOptions('Chyba API').text = 'Doslo k chybe pri komunikaci se serverem.');
    return false;
  }
};

// Zde můžete přidat další funkce pro správu uživatelů, např. updateUser, atd.
// Například:
// export const updateUser = async (userId: string, userData: Partial<AdminUserDto>): Promise<boolean> => { ... }
