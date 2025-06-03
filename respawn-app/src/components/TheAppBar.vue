<template>
  <v-app-bar app color="surface" density="compact" class="futuristic-app-bar" elevation="2">
    <v-app-bar-nav-icon
      @click.stop="emitToggleLeftDrawer"
      class="futuristic-icon-btn"
    ></v-app-bar-nav-icon>

    <v-toolbar-title class="font-oxanium app-title">
      <router-link to="/" class="text-decoration-none text-primary">
        <v-icon :icon="mdiRocketLaunchOutline" start></v-icon>
        Respawn
      </router-link>
    </v-toolbar-title>

    <v-spacer></v-spacer>

    <template v-if="!authStore.isLoggedIn">
      <v-btn
        @click="showLoginModal"
        class="futuristic-btn auth-btn mx-1"
        :prepend-icon="mdiLoginVariant"
      >
        Přihlásit se
      </v-btn>
      <v-btn
        @click="showRegisterModal"
        class="futuristic-btn-secondary auth-btn mx-1"
        :prepend-icon="mdiAccountPlusOutline"
      >
        Registrovat
      </v-btn>
    </template>
    <template v-else>
      <span class="mr-3 font-inter text-subtitle-2 d-none d-sm-inline"
        >Vítej, {{ authStore.user?.nickname }}!</span
      >
      <v-btn
        @click="handleLogout"
        class="futuristic-btn error-btn mx-1"
        :prepend-icon="mdiLogoutVariant"
      >
        Odhlásit se
      </v-btn>
    </template>

    <v-app-bar-nav-icon
      @click.stop="emitToggleRightDrawer"
      class="futuristic-icon-btn"
      v-if="authStore.isLoggedIn"
    >
      <v-icon :icon="mdiAccountGroup"></v-icon>
    </v-app-bar-nav-icon>
  </v-app-bar>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/authStore'
import { displayLoginModal, displayRegisterModal } from '@/services/authService'
import {
  mdiRocketLaunchOutline,
  mdiLoginVariant,
  mdiAccountPlusOutline,
  mdiLogoutVariant,
  mdiAccountGroup,
} from '@mdi/js'

const authStore = useAuthStore()

const emit = defineEmits(['toggle-left-drawer', 'toggle-right-drawer'])

const emitToggleLeftDrawer = () => {
  emit('toggle-left-drawer')
}

const emitToggleRightDrawer = () => {
  emit('toggle-right-drawer')
}

const showLoginModal = async () => {
  try {
    await displayLoginModal()
  } catch (error) {
    console.debug('Login modal was closed or failed.', error)
  }
}

const showRegisterModal = async () => {
  try {
    await displayRegisterModal()
  } catch (error) {
    console.debug('Register modal was closed or failed.', error)
  }
}

const handleLogout = async () => {
  await authStore.logout()
}
</script>

<style scoped>
.futuristic-app-bar {
  border-bottom: 1px solid rgba(var(--v-theme-primary-rgb), 0.2) !important;
  background: linear-gradient(
    to right,
    rgba(var(--v-theme-surface-rgb), 0.95),
    rgba(var(--v-theme-surface-rgb), 0.85)
  );
  backdrop-filter: blur(10px);
}

.app-title .v-icon {
  color: var(--v-theme-primary);
  animation: pulse-glow 2s infinite alternate;
}

@keyframes pulse-glow {
  0% {
    text-shadow: 0 0 5px rgba(var(--v-theme-primary-rgb), 0.5);
  }
  100% {
    text-shadow: 0 0 15px rgba(var(--v-theme-primary-rgb), 1);
  }
}

.futuristic-btn,
.futuristic-btn-secondary,
.error-btn {
  font-family: var(--font-family-headings-exo2);
  font-weight: 600;
  border-radius: 6px;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  transition: all 0.3s ease-in-out;
}

.futuristic-btn {
  background: linear-gradient(45deg, var(--v-theme-primary), var(--v-theme-secondary)) !important;
  color: aliceblue !important;
  border: none;
  box-shadow: 0 2px 8px rgba(var(--v-theme-primary-rgb), 0.4);
}
.futuristic-btn:hover {
  box-shadow: 0 4px 15px rgba(var(--v-theme-primary-rgb), 0.6);
  transform: translateY(-2px);
}

.futuristic-btn-secondary {
  background-color: transparent !important;
  border: 1px solid var(--v-theme-secondary) !important;
  color: var(--v-theme-secondary) !important;
}
.futuristic-btn-secondary:hover {
  background-color: rgba(var(--v-theme-secondary-rgb), 0.1) !important;
  transform: translateY(-2px);
}

.error-btn {
  background-color: var(--v-theme-error) !important;
  color: white !important;
  border: none;
  box-shadow: 0 2px 8px rgba(var(--v-theme-error-rgb), 0.4);
}
.error-btn:hover {
  background-color: var(--v-theme-error-darken-1) !important;
  box-shadow: 0 4px 15px rgba(var(--v-theme-error-rgb), 0.6);
  transform: translateY(-2px);
}

.auth-btn {
  min-width: 120px;
}

.futuristic-icon-btn {
  color: var(--v-theme-text-secondary);
}
.futuristic-icon-btn:hover {
  color: var(--v-theme-primary);
}
</style>
