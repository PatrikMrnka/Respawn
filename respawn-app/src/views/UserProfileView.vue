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

      <v-form
        @submit.prevent="handleProfileUpdate"
        v-if="!loading && userProfile"
        ref="profileForm"
      >
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
          </v-col>

          <v-col cols="12" md="8">
            <v-text-field
              v-model="editableProfile.nickname"
              label="Přezdívka"
              :prepend-inner-icon="mdiAccount"
              variant="outlined"
              class="mb-4 futuristic-input"
              :rules="[rules.required, rules.minLength(3), rules.maxLength(50)]"
              counter="50"
            ></v-text-field>

            <v-text-field
              :model-value="userProfile.email"
              label="Email"
              :prepend-inner-icon="mdiEmail"
              variant="outlined"
              class="mb-4 futuristic-input"
              readonly
              disabled
              hint="Email nelze změnit"
              persistent-hint
            ></v-text-field>

            <v-text-field
              v-model="editableProfile.avatarUrl"
              label="URL Avataru (volitelné)"
              :prepend-inner-icon="mdiImage"
              variant="outlined"
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
import { ref, onMounted, reactive, watch } from 'vue'
import { useAuthStore, type UserInfo } from '@/stores/authStore'
import { fetchUserProfile, updateUserProfile } from '@/services/profileService'
import Swal from 'sweetalert2'

import { mdiAccount, mdiEmail, mdiImage } from '@mdi/js'

// Interface for editable profile data
interface EditableProfileData {
  nickname: string
  avatarUrl?: string
}

const authStore = useAuthStore()
const userProfile = ref<UserInfo | null>(null)
const editableProfile = reactive<EditableProfileData>({
  nickname: '',
  avatarUrl: '',
})

const loading = ref(true)
const isUpdating = ref(false)
const error = ref<string | null>(null)
const avatarFile = ref<File[]>([]) // For v-file-input
const avatarPreview = ref<string | null>(null)
const defaultAvatar =
  'https://gravatar.com/avatar/eb1fc2dbf0e8cd5e8517916a58c608e0?s=400&d=robohash&r=x' // Fallback avatar

const profileForm = ref<any>(null) // Reference to v-form

// Validation rules
const rules = {
  required: (value: string) => !!value || 'This field is required.',
  minLength: (length: number) => (value: string) =>
    (value && value.length >= length) || `Minimum ${length} characters required.`,
  maxLength: (length: number) => (value: string) =>
    (value && value.length <= length) || `Maximum ${length} characters allowed.`,
  url: (value: string | undefined) => {
    if (!value) return true // URL is optional
    try {
      new URL(value)
      return true
    } catch (_) {
      return 'Invalid URL format.'
    }
  },
}

// Load user profile data
const loadUserProfile = async () => {
  loading.value = true
  error.value = null
  try {
    const profile = await fetchUserProfile()
    if (profile) {
      userProfile.value = profile
      editableProfile.nickname = profile.nickname
      editableProfile.avatarUrl = profile.avatarUrl || ''
    } else {
      error.value = 'Failed to load profile.'
    }
  } catch (err: any) {
    console.error('Error loading profile:', err)
    error.value = err.message || 'Server communication error occurred.'
  } finally {
    loading.value = false
  }
}

onMounted(loadUserProfile)

// Watch for changes in authStore.user in case it changes after login/update elsewhere
watch(
  () => authStore.user,
  (newUser) => {
    if (newUser) {
      userProfile.value = newUser
      editableProfile.nickname = newUser.nickname
      editableProfile.avatarUrl = newUser.avatarUrl || ''
    }
  },
  { deep: true },
)

// Handle profile update submission
const handleProfileUpdate = async () => {
  if (!profileForm.value) return
  const { valid } = await profileForm.value.validate()

  if (!valid) {
    Swal.fire({
      icon: 'error',
      titleText: 'Validation Error',
      text: 'Please fix the errors in the form.',
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
    })
    return
  }

  isUpdating.value = true
  error.value = null

  const payload: { nickname: string; avatarUrl?: string } = {
    nickname: editableProfile.nickname,
  }

  try {
    const updatedUser = await updateUserProfile(payload)
    if (updatedUser) {
      // Update local user profile with data from auth store
      if (
        authStore.user &&
        authStore.user.id &&
        authStore.user.nickname &&
        authStore.user.email &&
        authStore.user.roles
      ) {
        userProfile.value = {
          id: authStore.user.id,
          nickname: authStore.user.nickname,
          email: authStore.user.email,
          avatarUrl: authStore.user.avatarUrl,
          roles: authStore.user.roles,
        }
      }
      editableProfile.nickname = updatedUser.nickname
      editableProfile.avatarUrl = updatedUser.avatarUrl || ''
      avatarFile.value = [] // Reset file input
      avatarPreview.value = null // Reset preview
    } else {
      // Handle empty response case
    }
  } catch (err: any) {
    console.error('Error updating profile:', err)
    error.value = err.message || 'Server communication error occurred.'
    Swal.fire({
      icon: 'error',
      titleText: 'Update Error',
      text: error.value || 'Unknown error',
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
    })
  } finally {
    isUpdating.value = false
  }
}
</script>

<style scoped>
.futuristic-page {
  padding-top: 20px;
  max-width: 900px;
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
</style>
