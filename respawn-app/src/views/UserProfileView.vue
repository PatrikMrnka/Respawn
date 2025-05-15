<template>
  <v-container class="profile-view">
    <h1 class="font-exo2 page-title text-primary mb-8">Můj Profil</h1>

    <v-row justify="center">
      <v-col cols="12" md="8" lg="6">
        <v-card class="futuristic-card pa-4 pa-md-6" shaped elevation="8">
          <v-card-text v-if="loading" class="text-center">
            <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
            <p class="mt-4 font-inter">Načítání profilu...</p>
          </v-card-text>

          <v-form v-else-if="userProfile" ref="profileForm" @submit.prevent="saveProfile">
            <v-row>
              <v-col cols="12" class="text-center mb-6">
                <v-avatar color="surface-variant" size="120" class="profile-avatar">
                  <v-img 
                    :src="editableProfile.avatarUrl || defaultAvatar" 
                    alt="Avatar"
                    cover
                    @error="onAvatarError"
                  >
                    <template v-slot:placeholder>
                      <v-row class="fill-height ma-0" align="center" justify="center">
                        <v-progress-circular indeterminate color="grey-lighten-4"></v-progress-circular>
                      </v-row>
                    </template>
                  </v-img>
                </v-avatar>
                 <p class="font-roboto-mono text-text-secondary mt-2">ID: {{ userProfile.id }}</p>
              </v-col>

              <v-col cols="12">
                <v-text-field
                  v-model="editableProfile.nickname"
                  label="Přezdívka"
                  prepend-inner-icon="mdi-account"
                  variant="outlined"
                  class="futuristic-input"
                  :rules="[rules.required, rules.nicknameLength]"
                  clearable
                ></v-text-field>
              </v-col>

              <v-col cols="12">
                <v-text-field
                  v-model="userProfile.email"
                  label="Email"
                  prepend-inner-icon="mdi-email"
                  variant="outlined"
                  class="futuristic-input"
                  readonly
                  disabled
                  hint="Email nelze v tuto chvíli změnit"
                  persistent-hint
                ></v-text-field>
              </v-col>

              <v-col cols="12">
                <v-text-field
                  v-model="editableProfile.avatarUrl"
                  label="URL Avataru (volitelné)"
                  prepend-inner-icon="mdi-image"
                  variant="outlined"
                  class="futuristic-input"
                  clearable
                  placeholder="https://example.com/avatar.png"
                  :rules="[rules.urlFormat]"
                ></v-text-field>
              </v-col>

              <v-col cols="12" v-if="userProfile.roles && userProfile.roles.length > 0">
                 <p class="font-inter text-text-secondary mb-2">Role:</p>
                 <v-chip-group>
                    <v-chip
                        v-for="role in userProfile.roles"
                        :key="role"
                        color="secondary"
                        label
                        class="futuristic-chip"
                    >
                        {{ role }}
                    </v-chip>
                 </v-chip-group>
              </v-col>
            </v-row>

            <v-card-actions class="mt-6">
              <v-spacer></v-spacer>
              <v-btn 
                class="futuristic-btn" 
                variant="text" 
                @click="resetForm"
                :disabled="!isFormChanged || submitting"
              >
                Zrušit změny
              </v-btn>
              <v-btn
                type="submit"
                class="futuristic-btn-filled ml-4"
                color="primary"
                :loading="submitting"
                :disabled="!isFormChanged || !isFormValid"
              >
                <v-icon left class="mr-1">{{ mdiContentSave }}</v-icon>
                Uložit profil
              </v-btn>
            </v-card-actions>
          </v-form>
          
          <v-alert v-else type="error" variant="tonal" class="futuristic-alert">
            Nepodařilo se načíst profil uživatele. Zkuste to prosím později.
          </v-alert>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
import { ref, onMounted, reactive, watch, computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { fetchUserProfile, updateUserProfile } from '@/services/profileService';
import { mdiAccount, mdiEmail, mdiImage, mdiContentSave } from '@mdi/js';
import Swal from 'sweetalert2'; // Pro případné další notifikace, i když service je používá

interface UserProfile {
  id: string;
  nickname: string;
  email: string;
  avatarUrl?: string;
  roles: string[];
}

const authStore = useAuthStore();
const userProfile = ref<UserProfile | null>(null);
const editableProfile = reactive({
  nickname: '',
  avatarUrl: '',
});

const loading = ref(true);
const submitting = ref(false);
const profileForm = ref<any>(null); // Pro přístup k validaci formuláře Vuetify 3
const isFormValid = ref(false);

const defaultAvatar = 'https://placehold.co/120x120/1A2033/00E0FF?text=:-) '; // Placeholder

const rules = {
  required: (value: string) => !!value || 'Toto pole je povinné.',
  nicknameLength: (value: string) => (value && value.length >= 3 && value.length <= 50) || 'Přezdívka musí mít 3-50 znaků.',
  urlFormat: (value: string | null | undefined) => {
    if (!value || value.trim() === '') return true; // URL je volitelné
    try {
      new URL(value);
      return true;
    } catch (_) {
      return 'Neplatný formát URL.';
    }
  }
};

const loadUserProfile = async () => {
  loading.value = true;
  const profileData = await fetchUserProfile();
  if (profileData) {
    userProfile.value = profileData;
    editableProfile.nickname = profileData.nickname;
    editableProfile.avatarUrl = profileData.avatarUrl || '';
  } else {
    // Chyba byla již ošetřena v service, ale můžeme zobrazit obecnou zprávu
  }
  loading.value = false;
};

const onAvatarError = () => {
    // Pokud se obrázek avatara nenačte, můžeme nastavit výchozí
    // Toto je spíše pro případ, kdy by avatarUrl byl neplatný, ale ne prázdný
    if (editableProfile.avatarUrl !== defaultAvatar) { // Zabraňte smyčce, pokud i placeholder selže
        console.warn("Avatar image failed to load, using placeholder.");
        // Není třeba explicitně měnit, v-img by měl zobrazit placeholder
    }
};

const isFormChanged = computed(() => {
  if (!userProfile.value) return false;
  return (
    editableProfile.nickname !== userProfile.value.nickname ||
    (editableProfile.avatarUrl || '') !== (userProfile.value.avatarUrl || '') // Porovnání s prázdným řetězcem pro konzistenci
  );
});

// Sledování validity formuláře
watch(editableProfile, async () => {
  if (profileForm.value) {
    const { valid } = await profileForm.value.validate();
    isFormValid.value = valid;
  }
}, { deep: true });


const resetForm = () => {
  if (userProfile.value) {
    editableProfile.nickname = userProfile.value.nickname;
    editableProfile.avatarUrl = userProfile.value.avatarUrl || '';
    profileForm.value?.resetValidation(); // Reset validace Vuetify formuláře
  }
};

const saveProfile = async () => {
  if (!profileForm.value) return;
  const { valid } = await profileForm.value.validate();
  if (!valid) {
    Swal.fire({
        titleText: 'Chyba validace',
        text: 'Prosím, opravte chyby ve formuláři.',
        icon: 'error',
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

  submitting.value = true;
  const updatedData = await updateUserProfile({
    nickname: editableProfile.nickname,
    avatarUrl: editableProfile.avatarUrl || undefined, // Poslat undefined, pokud je prázdné, aby API mohlo nastavit null
  });

  if (updatedData) {
    userProfile.value = updatedData; // Aktualizuj lokální data po úspěšném uložení
    // authStore se aktualizuje v updateUserProfile
  }
  submitting.value = false;
};

onMounted(() => {
  if (authStore.isLoggedIn) {
    loadUserProfile();
  } else {
    // Pokud uživatel není přihlášen, neměl by se na tuto stránku dostat (řešeno v routeru)
    loading.value = false;
    // Můžete přidat přesměrování nebo zprávu
  }
});
</script>

<style scoped>
.profile-view {
  max-width: 900px;
  margin: auto;
}
.page-title {
  text-align: center;
}
.profile-avatar {
  border: 3px solid var(--v-theme-primary);
  box-shadow: 0 0 15px rgba(var(--v-theme-primary-rgb), 0.5);
}
.futuristic-input .v-field__outline {
    color: rgba(var(--v-theme-primary-rgb), 0.4) !important;
}
.futuristic-input.v-text-field--dirty .v-field__outline,
.futuristic-input .v-text-field--focused .v-field__outline {
    color: var(--v-theme-primary) !important;
}
.futuristic-chip {
    font-family: 'Roboto Mono', monospace;
    background-color: rgba(var(--v-theme-secondary-rgb), 0.2) !important;
    color: var(--v-theme-secondary) !important;
    border: 1px solid rgba(var(--v-theme-secondary-rgb), 0.5);
}
.futuristic-alert {
    border-color: var(--v-theme-error) !important;
}
</style>
