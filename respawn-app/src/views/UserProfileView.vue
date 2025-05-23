<template>
  <v-container class="futuristic-page">
    <v-card class="pa-md-8 pa-4 futuristic-card" elevation="2">
      <v-card-title class="text-h4 font-oxanium mb-6 text-center">
        Uživatelský Profil
      </v-card-title>

      <v-row justify="center" v-if="loading">
        <v-col cols="12" class="text-center">
          <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
          <p class="mt-4 font-inter">Načítání profilu...</p>
        </v-col>
      </v-row>

      <v-alert v-if="error" type="error" prominent class="mb-6 font-inter">
        {{ error }}
      </v-alert>

      <v-form @submit.prevent="handleProfileUpdate" v-if="!loading && userProfile" ref="profileForm">
        <v-row>
          <v-col cols="12" md="4" class="text-center">
            <v-avatar size="150" class="mb-4 elevation-3 futuristic-avatar-border">
              <v-img :src="avatarPreview || userProfile.avatarUrl || defaultAvatar" alt="Avatar">
                <template v-slot:placeholder>
                  <v-row class="fill-height ma-0" align="center" justify="center">
                    <v-icon :icon="mdiImage" size="50" color="grey-lighten-1"></v-icon>
                  </v-row>
                </template>
              </v-img>
            </v-avatar>
            <v-file-input
              v-model="avatarFile"
              label="Změnit avatar"
              prepend-icon=""
              :prepend-inner-icon="mdiImage" variant="outlined"
              dense
              accept="image/*"
              @change="onFileChange"
              class="mb-4 futuristic-input"
            ></v-file-input>
             <small class="text-grey font-inter">Nahráním nového obrázku se změní URL avataru.</small>
          </v-col>

          <v-col cols="12" md="8">
            <v-text-field
              v-model="editableProfile.nickname"
              label="Přezdívka"
              :prepend-inner-icon="mdiAccount" variant="outlined"
              class="mb-4 futuristic-input"
              :rules="[rules.required, rules.minLength(3), rules.maxLength(50)]"
              counter="50"
            ></v-text-field>

            <v-text-field
              :model-value="userProfile.email"
              label="Email"
              :prepend-inner-icon="mdiEmail" variant="outlined"
              class="mb-4 futuristic-input"
              readonly
              disabled
              hint="Email nelze změnit"
              persistent-hint
            ></v-text-field>

            <v-text-field
              v-model="editableProfile.avatarUrl"
              label="URL Avataru (volitelné)"
              :prepend-inner-icon="mdiImage" variant="outlined"
              class="mb-4 futuristic-input"
              :rules="[rules.maxLength(500), rules.url]"
              counter="500"
              placeholder="https://example.com/avatar.png"
              hint="Zadejte URL nebo nahrajte soubor výše."
            ></v-text-field>

            <v-card-actions class="pa-0 mt-6">
              <v-spacer></v-spacer>
              <v-btn
                type="submit"
                color="primary"
                size="large"
                :loading="isUpdating"
                class="futuristic-btn font-exo2"
                min-width="150"
              >
                Uložit změny
              </v-btn>
            </v-card-actions>
          </v-col>
        </v-row>
      </v-form>
    </v-card>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive, watch } from 'vue';
import { useAuthStore, type UserInfo } from '@/stores/authStore';
import { fetchUserProfile, updateUserProfile } from '@/services/profileService';
import Swal from 'sweetalert2';

// Explicitní import MDI ikon, které způsobovaly chybu
import { mdiAccount, mdiEmail, mdiImage } from '@mdi/js';

// Typ pro data profilu, která může uživatel editovat
interface EditableProfileData {
  nickname: string;
  avatarUrl?: string;
}

const authStore = useAuthStore();
const userProfile = ref<UserInfo | null>(null);
const editableProfile = reactive<EditableProfileData>({
  nickname: '',
  avatarUrl: '',
});

const loading = ref(true);
const isUpdating = ref(false);
const error = ref<string | null>(null);
const avatarFile = ref<File[]>([]); // Pro v-file-input
const avatarPreview = ref<string | null>(null);
const defaultAvatar = 'https://via.placeholder.com/150/1A2033/E0E0E0?text=Avatar'; // Záložní avatar

const profileForm = ref<any>(null); // Reference na v-form

// Pravidla pro validaci
const rules = {
  required: (value: string) => !!value || 'Toto pole je povinné.',
  minLength: (length: number) => (value: string) => (value && value.length >= length) || `Minimálně ${length} znaků.`,
  maxLength: (length: number) => (value: string) => (value && value.length <= length) || `Maximálně ${length} znaků.`,
  url: (value: string | undefined) => {
    if (!value) return true; // URL je volitelné
    try {
      new URL(value);
      return true;
    } catch (_) {
      return 'Neplatný formát URL.';
    }
  }
};

// Načtení profilu uživatele
const loadUserProfile = async () => {
  loading.value = true;
  error.value = null;
  try {
    const profile = await fetchUserProfile();
    if (profile) {
      userProfile.value = profile;
      editableProfile.nickname = profile.nickname;
      editableProfile.avatarUrl = profile.avatarUrl || '';
    } else {
      error.value = 'Nepodařilo se načíst profil.';
    }
  } catch (err: any) {
    console.error('Chyba při načítání profilu:', err);
    error.value = err.message || 'Došlo k chybě při komunikaci se serverem.';
  } finally {
    loading.value = false;
  }
};

onMounted(loadUserProfile);

// Sledování změn v authStore.user pro případ, že se změní po přihlášení/aktualizaci jinde
watch(() => authStore.user, (newUser) => {
  if (newUser) {
    userProfile.value = newUser;
    editableProfile.nickname = newUser.nickname;
    editableProfile.avatarUrl = newUser.avatarUrl || '';
  }
}, { deep: true });


// Zpracování nahrání souboru avataru
const onFileChange = () => {
  if (avatarFile.value && avatarFile.value.length > 0) {
    const file = avatarFile.value[0];
    // Vytvoření náhledu
    const reader = new FileReader();
    reader.onload = (e) => {
      avatarPreview.value = e.target?.result as string;
    };
    reader.readAsDataURL(file);
    // Zde byste normálně nahráli soubor na server a získali URL
    // Prozatím nastavíme placeholder URL nebo necháme uživatele zadat ručně
    // editableProfile.avatarUrl = `local_preview_for_${file.name}`; // Toto je jen pro ukázku
    // V reálné aplikaci byste zde volali API pro nahrání souboru
    // a po úspěšném nahrání by API vrátilo URL, které byste nastavili do editableProfile.avatarUrl
    // Pro tento příklad, pokud uživatel vybere soubor, vymažeme pole URL, aby se soustředil na nahrání
    editableProfile.avatarUrl = '';
     Swal.fire({
        icon: 'info',
        titleText: 'Avatar připraven',
        text: 'Avatar bude nahrán a URL aktualizováno po uložení změn profilu. Prozatím se URL avataru vymaže, pokud bylo zadáno.',
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
        timer: 3000,
        timerProgressBar: true
    });
  } else {
    avatarPreview.value = null;
  }
};


// Zpracování aktualizace profilu
const handleProfileUpdate = async () => {
  if (!profileForm.value) return;
  const { valid } = await profileForm.value.validate();

  if (!valid) {
    Swal.fire({
        icon: 'error',
        titleText: 'Chyba validace',
        text: 'Prosím, opravte chyby ve formuláři.',
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
    });
    return;
  }

  isUpdating.value = true;
  error.value = null;

  const payload: { nickname: string; avatarUrl?: string } = {
    nickname: editableProfile.nickname,
  };

  // TODO: Implementace nahrávání souboru avataru na server
  // Pokud je avatarFile nahrán, měli byste ho zde odeslat na server,
  // získat URL a to pak použít v payload.avatarUrl.
  // Prozatím, pokud je avatarUrl v editableProfile vyplněno, použije se to.
  // Pokud je avatarFile vybrán, předpokládáme, že backend API si poradí s nahráním
  // nebo že by zde byla logika pro nahrání a získání URL před voláním updateUserProfile.

  if (avatarFile.value && avatarFile.value.length > 0) {
      // Zde by byla logika pro nahrání souboru na server a získání URL
      // Například: const uploadedAvatarUrl = await uploadAvatarToServer(avatarFile.value[0]);
      // payload.avatarUrl = uploadedAvatarUrl;
      // Prozatím necháme na updateUserProfile, aby si s tím poradil, nebo to vyžaduje úpravu API
      // Pokud API neumí přijmout soubor přímo s profile daty, je třeba nahrát soubor zvlášť.
      // V tomto příkladu, pokud je avatarUrl prázdné a soubor je vybrán,
      // necháme avatarUrl v payloadu undefined, což by mohlo znamenat "nechat stávající"
      // nebo pokud je API chytřejší, mohlo by to znamenat "vymazat avatar".
      // Pro tento příklad, pokud je avatarUrl prázdné (protože jsme ho vymazali při výběru souboru),
      // a soubor je vybrán, tak URL neposíláme, aby se nepokoušelo nastavit prázdné URL.
      // Ideálně by API mělo endpoint pro nahrání avataru, který vrátí URL.
      console.warn("Nahrávání souboru avataru není plně implementováno v tomto příkladu. Použije se zadané URL nebo stávající.");
      if (editableProfile.avatarUrl) {
        payload.avatarUrl = editableProfile.avatarUrl;
      }
  } else if (editableProfile.avatarUrl) {
    payload.avatarUrl = editableProfile.avatarUrl;
  } else {
    // Pokud ani URL není zadáno a ani soubor není vybrán, a chceme explicitně smazat avatar
    // payload.avatarUrl = ""; // Odeslat prázdný řetězec pro smazání (záleží na API)
    // Nebo nechat undefined, pokud API interpretuje absenci jako "neměnit"
  }


  try {
    const updatedUser = await updateUserProfile(payload);
    if (updatedUser) {
      // SweetAlert pro úspěch je již v profileService
      userProfile.value = { ...authStore.user }; // Aktualizujeme lokální userProfile z authStore, který byl aktualizován
      editableProfile.nickname = updatedUser.nickname;
      editableProfile.avatarUrl = updatedUser.avatarUrl || '';
      avatarFile.value = []; // Reset file input
      avatarPreview.value = null; // Reset preview
    } else {
      // Chybová zpráva by měla být zobrazena z profileService
      // error.value = 'Nepodařilo se aktualizovat profil.'; // Záložní
    }
  } catch (err: any) {
    console.error('Chyba při aktualizaci profilu:', err);
    error.value = err.message || 'Došlo k chybě při komunikaci se serverem.';
    Swal.fire({
        icon: 'error',
        titleText: 'Chyba aktualizace',
        text: error.value,
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
    });
  } finally {
    isUpdating.value = false;
  }
};
</script>

<style scoped>
.futuristic-page {
  padding-top: 20px;
  max-width: 900px; /* Omezení šířky pro lepší čitelnost na velkých obrazovkách */
  margin: auto;
}

.futuristic-card {
  background-color: var(--v-theme-surface);
  border: 1px solid rgba(var(--v-theme-primary-rgb), 0.2);
  box-shadow: 0 0 20px rgba(var(--v-theme-primary-rgb), 0.15);
}

.futuristic-avatar-border {
  border: 3px solid var(--v-theme-primary);
}

.futuristic-input .v-field {
  background-color: rgba(var(--v-theme-background-rgb), 0.7) !important;
}
.futuristic-input .v-field__outline {
  color: rgba(var(--v-theme-primary-rgb), 0.4) !important;
}
.futuristic-input.v-text-field--dirty .v-field__outline,
.futuristic-input .v-input--is-focused .v-field__outline {
  color: var(--v-theme-primary) !important;
}

.futuristic-btn {
  /* Styly jsou již v global/futuristic.css */
}
</style>
