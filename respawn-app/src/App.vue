<template>
  <v-app class="futuristic-app">
    <TheNavigationDrawer v-model="leftDrawerOpen" />

    <TheAppBar 
      @toggle-left-drawer="leftDrawerOpen = !leftDrawerOpen" 
      @toggle-right-drawer="rightDrawerOpen = !rightDrawerOpen" 
    />

    <v-main>
      <v-container fluid class="pa-0 main-content-area">
        <router-view v-slot="{ Component }">
          <v-fade-transition mode="out-in">
            <component :is="Component" />
          </v-fade-transition>
        </router-view>
      </v-container>
    </v-main>

    <v-navigation-drawer
      v-model="rightDrawerOpen"
      location="right"
      temporary width="300"
      class="user-list-drawer"
      v-if="authStore.isLoggedIn"
    >
      <UserList />
    </v-navigation-drawer>

  </v-app>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
// import { useDisplay } from 'vuetify'; // Již není potřeba pro leftDrawerOpen
import TheNavigationDrawer from '@/components/TheNavigationDrawer.vue';
import TheAppBar from '@/components/TheAppBar.vue';
import UserList from '@/components/UserList.vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();

// Levý drawer je nyní ve výchozím stavu zavřený na všech velikostech
const leftDrawerOpen = ref(false); 
const rightDrawerOpen = ref(false);

// Sledování přihlášení pro zavření pravého panelu při odhlášení
watch(() => authStore.isLoggedIn, (isLoggedIn) => {
  if (!isLoggedIn) {
    rightDrawerOpen.value = false;
    leftDrawerOpen.value = false; // Případně zavřít i levý panel při odhlášení
  }
});

</script>

<style>
/* Globální styly (převzato z předchozí verze App.vue) */
:root {
  --font-family-sans-serif: 'Inter', sans-serif;
  --font-family-headings-oxanium: 'Oxanium', cursive;
  --font-family-headings-exo2: 'Exo 2', sans-serif;
  --font-family-mono: 'Roboto Mono', monospace;
}

.futuristic-app {
  font-family: var(--font-family-sans-serif);
  background-color: var(--v-theme-background) !important;
  color: var(--v-theme-text-primary);
  min-height: 100vh;
}

.main-content-area {
  /* padding-top: 56px; /* Výška compact app baru */
}

.user-list-drawer .v-navigation-drawer__content {
  padding: 0 !important;
}
.user-list-drawer {
   background-color: rgba(var(--v-theme-surface-rgb), 0.85) !important;
   backdrop-filter: blur(8px);
   border-left: 1px solid rgba(var(--v-theme-primary-rgb), 0.25) !important;
}

::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}
::-webkit-scrollbar-track {
  background: rgba(var(--v-theme-surface-rgb), 0.5);
  border-radius: 4px;
}
::-webkit-scrollbar-thumb {
  background-color: rgba(var(--v-theme-primary-rgb), 0.5);
  border-radius: 4px;
  border: 2px solid transparent;
  background-clip: content-box;
}
::-webkit-scrollbar-thumb:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.7);
}

.v-fade-transition-leave-active,
.v-fade-transition-enter-active {
  transition: opacity 0.2s ease-out;
}
.v-fade-transition-enter-from,
.v-fade-transition-leave-to {
  opacity: 0;
}
</style>
