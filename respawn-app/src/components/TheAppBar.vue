<template>
  <v-app-bar app color="surface" elevation="4" class="futuristic-app-bar">
    <v-app-bar-nav-icon
      @click.stop="$emit('toggle-drawer')"
      class="text-primary"
    ></v-app-bar-nav-icon>
    <v-toolbar-title
      class="font-oxanium text-primary app-title-clickable"
      @click="$emit('navigate-home')"
    >
      Respawn
    </v-toolbar-title>
    <v-spacer></v-spacer>

    <template v-if="!authStore.isLoggedIn">
      <v-btn class="futuristic-btn mr-2" variant="outlined" @click="$emit('login')">
        <v-icon left class="mr-1">{{ mdiLogin }}</v-icon>
        Přihlásit se
      </v-btn>
      <v-btn class="futuristic-btn-filled" color="primary" @click="$emit('register')">
        <v-icon left class="mr-1">{{ mdiAccountPlus }}</v-icon>
        Registrovat
      </v-btn>
    </template>
    <template v-else>
      <v-menu offset-y>
        <template v-slot:activator="{ props }">
          <v-btn icon class="text-primary ml-2" v-bind="props">
            <v-avatar color="secondary" size="36">
              <v-img
                v-if="authStore.currentUser?.avatarUrl"
                :src="authStore.currentUser.avatarUrl"
                :alt="
                  authStore.currentUser.nickname
                    ? `${authStore.currentUser.nickname} avatar`
                    : 'User avatar'
                "
                cover
                @error="avatarImageError"
              ></v-img>
              <span v-else class="text-white font-weight-bold">{{ authStore.userInitials }}</span>
            </v-avatar>
          </v-btn>
        </template>
        <v-list bg-color="surface" class="futuristic-menu" density="compact">
          <v-list-item class="menu-item-disabled">
            <v-list-item-title class="font-inter text-text-secondary">
              Přihlášen jako:
              <strong class="font-oxanium">{{ authStore.currentUser?.nickname }}</strong>
            </v-list-item-title>
          </v-list-item>
          <v-divider></v-divider>
          <v-list-item @click="$emit('navigate', '/profile')" class="menu-item">
            <template v-slot:prepend>
              <v-icon :color="isActiveRoute('/profile') ? 'primary' : 'text-primary'">{{
                mdiAccountCog
              }}</v-icon>
            </template>
            <v-list-item-title
              class="font-inter"
              :class="isActiveRoute('/profile') ? 'text-primary' : 'text-text-primary'"
              >Můj Profil</v-list-item-title
            >
          </v-list-item>
          <v-list-item @click="$emit('logout')" class="menu-item">
            <template v-slot:prepend>
              <v-icon color="text-text-primary">{{ mdiLogout }}</v-icon>
            </template>
            <v-list-item-title class="font-inter text-text-primary">Odhlásit se</v-list-item-title>
          </v-list-item>
        </v-list>
      </v-menu>
    </template>
  </v-app-bar>
</template>

<script lang="ts" setup>
import { useAuthStore } from '@/stores/authStore'
import { useRoute } from 'vue-router'
import { mdiLogin, mdiAccountPlus, mdiAccountCog, mdiLogout } from '@mdi/js'

defineEmits(['toggle-drawer', 'login', 'register', 'logout', 'navigate', 'navigate-home'])

const authStore = useAuthStore()
const route = useRoute()

const isActiveRoute = (path: string) => {
  return route.path === path
}

const avatarImageError = (event: Event) => {
  const imgElement = event.target as HTMLImageElement
  imgElement.src = 'https://placehold.co/36x36/7F00FF/E0E0E0?text=' + authStore.userInitials
}
</script>

<style scoped>
.futuristic-app-bar {
  border-bottom: 1px solid rgba(var(--v-theme-primary-rgb), 0.3) !important;
}
.app-title-clickable {
  cursor: pointer;
  transition: opacity 0.2s ease-in-out;
}
.app-title-clickable:hover {
  opacity: 0.8;
}

.user-avatar-button .v-avatar {
  overflow: hidden;
}

.user-avatar-button .v-avatar .v-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.futuristic-btn {
  border-color: rgba(var(--v-theme-border-color-rgb), 0.7);
  color: var(--v-theme-primary);
  font-family: 'Exo 2', sans-serif;
}
.futuristic-btn:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.1);
  border-color: var(--v-theme-primary);
  box-shadow: 0 0 10px 0px var(--v-theme-glow-color);
}
.futuristic-btn-filled {
  font-family: 'Exo 2', sans-serif;
  box-shadow: 0 0 8px 0px transparent;
}
.futuristic-btn-filled:hover {
  box-shadow: 0 0 12px 2px var(--v-theme-glow-color);
  transform: translateY(-2px);
}
.futuristic-menu .v-list-item-title {
  font-size: 0.95rem !important;
}
.futuristic-menu .v-list-item {
  cursor: pointer;
}
.futuristic-menu .v-list-item:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.1) !important;
}
.futuristic-menu .v-list-item:hover .v-list-item-title,
.futuristic-menu .v-list-item:hover .v-icon {
  color: var(--v-theme-primary) !important;
}
.menu-item-disabled {
  opacity: 0.7;
  pointer-events: none;
}
.menu-item .v-list-item-title {
  font-family: 'Exo 2', sans-serif;
  transition: color 0.2s ease-in-out;
}
.menu-item:not(.v-list-item--active) .v-list-item-title {
  color: var(--v-theme-text-primary) !important;
}
.menu-item:not(.v-list-item--active) .v-icon {
  color: var(--v-theme-text-primary) !important;
}
.menu-item.v-list-item--active .v-list-item-title,
.menu-item.v-list-item--active .v-icon {
  color: var(--v-theme-primary) !important;
}
.v-avatar .text-white {
  color: #ffffff !important;
}
</style>
