<template>
  <v-app class="futuristic-app">
    <v-app-bar app color="surface" elevation="4" class="futuristic-app-bar">
      <v-app-bar-nav-icon @click.stop="drawer = !drawer" class="text-primary"></v-app-bar-nav-icon>
      <v-toolbar-title 
        class="font-oxanium text-primary app-title-clickable" 
        @click="navigateTo('/')"
      >
        Respawn
      </v-toolbar-title>
      <v-spacer></v-spacer>

      <template v-if="!authStore.isLoggedIn">
        <v-btn class="futuristic-btn mr-2" variant="outlined" @click="handleLogin">
          <v-icon left class="mr-1">{{ mdiLogin }}</v-icon>
          Přihlásit se
        </v-btn>
        <v-btn class="futuristic-btn-filled" color="primary" @click="handleRegister">
           <v-icon left class="mr-1">{{ mdiAccountPlus }}</v-icon>
          Registrovat
        </v-btn>
      </template>
      <template v-else>
        <v-menu offset-y>
          <template v-slot:activator="{ props }">
            <v-btn icon class="text-primary ml-2" v-bind="props">
              <v-avatar color="secondary" size="36">
                <img v-if="authStore.currentUser?.avatarUrl" :src="authStore.currentUser.avatarUrl" :alt="authStore.currentUser.nickname" @error="avatarImageError" />
                <span v-else class="white--text font-weight-bold">{{ authStore.userInitials }}</span>
              </v-avatar>
            </v-btn>
          </template>
          <v-list bg-color="surface" class="futuristic-menu" density="compact">
            <v-list-item class="menu-item-disabled">
                <v-list-item-title class="font-inter text-text-secondary">
                    Přihlášen jako: <strong class="font-oxanium">{{ authStore.currentUser?.nickname }}</strong>
                </v-list-item-title>
            </v-list-item>
            <v-divider></v-divider>
            <v-list-item @click="navigateTo('/profile')" class="menu-item">
              <template v-slot:prepend>
                <v-icon :color="iconColor('/profile')">{{ mdiAccountCog }}</v-icon>
              </template>
              <v-list-item-title class="font-inter" :class="isActiveRoute('/profile') ? 'text-primary' : 'text-text-primary'">Můj Profil</v-list-item-title>
            </v-list-item>
            <v-list-item @click="handleLogout" class="menu-item">
               <template v-slot:prepend>
                <v-icon :color="iconColor('/logout')">{{ mdiLogout }}</v-icon> </template>
              <v-list-item-title class="font-inter text-text-primary">Odhlásit se</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-menu>
      </template>
    </v-app-bar>

    <v-navigation-drawer
      v-model="drawer"
      app
      temporary
      color="surface"
      class="futuristic-drawer"
    >
      <v-list nav dense>
        <v-list-item
          v-for="(item, i) in filteredMenuItems"
          :key="i"
          @click="navigateTo(item.path)"
          class="menu-item"
          :active="isActiveRoute(item.path)"
        >
            <template v-slot:prepend>
                 <v-icon :icon="item.icon" :color="iconColor(item.path)"></v-icon>
            </template>
             <v-list-item-title :class="isActiveRoute(item.path) ? 'text-primary' : 'text-text-primary'">{{ item.title }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-navigation-drawer>

    <v-main>
      <v-container fluid class="pa-md-8 pa-4">
        <router-view v-slot="{ Component }">
          <transition name="fade-transform" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>

        <v-row v-if="showWelcomeMessage" class="text-center mt-16">
          <v-col cols="12">
            <h1 class="font-exo2 page-title text-primary mb-6">Vítejte v Respawn LAN Organizer!</h1>
            <p class="font-inter text-text-secondary mb-8" style="font-size: 1.2rem;">
              Přihlaste se nebo zaregistrujte pro plný přístup k funkcím aplikace.
            </p>
            <div>
                <v-btn class="futuristic-btn-filled mx-2" color="primary" size="large" @click="handleRegister">
                    <v-icon left class="mr-2">{{ mdiAccountPlus }}</v-icon>
                    Registrovat
                </v-btn>
                <v-btn class="futuristic-btn mx-2" variant="outlined" size="large" @click="handleLogin">
                    <v-icon left class="mr-1">{{ mdiLogin }}</v-icon>
                    Přihlásit se
                </v-btn>
            </div>
          </v-col>
        </v-row>
         <v-row class="mt-8" v-if="showDashboardContent">
           <v-col cols="12">
            <h1 class="font-exo2 page-title text-primary mb-6">Přehled</h1>
          </v-col>
          <v-col cols="12" md="6" lg="4">
            <v-card class="futuristic-card" shaped elevation="8">
              <div class="hexagon-decoration top-left"></div>
              <div class="hexagon-decoration bottom-right"></div>
              <v-card-title class="font-oxanium card-title">
                <v-icon left class="mr-2">{{ mdiServer }}</v-icon>
                Stav Serverů
              </v-card-title>
              <v-card-text class="font-inter card-text">
                <p>CS 1.6 Server: <span class="text-success">Online</span> - Hráčů: 12/16</p>
                <p>Minecraft Server: <span class="text-warning">Restartuje se</span></p>
                <p class="font-roboto-mono mt-2">Poslední ping: 15ms</p>
              </v-card-text>
              <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn class="futuristic-btn" variant="outlined">
                  <v-icon left class="mr-1">{{ mdiCog }}</v-icon>
                  Spravovat
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>

          <v-col cols="12" md="6" lg="4">
            <v-card class="futuristic-card" shaped elevation="8">
              <v-card-title class="font-oxanium card-title">
                <v-icon left class="mr-2">{{ mdiPoll }}</v-icon>
                Aktivní Ankety
              </v-card-title>
              <v-card-text class="font-inter card-text">
                <p>Kterou mapu dnes večer? (Zbývá: 2h 15m)</p>
                <ul class="pl-4">
                  <li>Dust 2 (Hlasů: 8)</li>
                  <li>Inferno (Hlasů: 5)</li>
                </ul>
                 <p class="font-roboto-mono mt-2">ID Ankety: #A4F8C</p>
              </v-card-text>
              <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn class="futuristic-btn" variant="tonal" color="primary">
                  Hlasovat
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>

          <v-col cols="12" md="6" lg="4">
            <v-card class="futuristic-card" shaped elevation="8">
               <div class="hexagon-decoration top-right"></div>
              <div class="hexagon-decoration bottom-left"></div>
              <v-card-title class="font-oxanium card-title">
                <v-icon left class="mr-2">{{ mdiChartBar }}</v-icon>
                Moje Statistiky
              </v-card-title>
              <v-card-text class="font-inter card-text">
                <p>Odehráno hodin: <strong>42</strong></p>
                <p>Výher/Proher: <strong>25/17</strong></p>
                <p class="font-roboto-mono mt-2">K/D Ratio: 1.47</p>
              </v-card-text>
               <v-card-actions>
                <v-spacer></v-spacer>
                <v-btn class="futuristic-btn" variant="text" color="secondary">
                  Detail
                </v-btn>
              </v-card-actions>
            </v-card>
          </v-col>
        </v-row>
      </v-container>
    </v-main>

    <v-footer app color="surface" class="futuristic-footer" elevation="4">
      <span class="font-inter text-text-secondary">&copy; {{ new Date().getFullYear() }} Respawn LAN Organizer - Všechna práva vyhrazena.</span>
    </v-footer>
  </v-app>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { displayLoginModal, displayRegisterModal } from '@/services/authService';
import { useAuthStore } from '@/stores/authStore';
// MDI SVG Icons import
import { 
  mdiAccountCircle, // Keep this if you plan to use it as a default avatar icon
  mdiServer, 
  mdiPoll, 
  mdiChartBar, 
  mdiCog, 
  mdiViewDashboard, 
  mdiCogs, 
  mdiLogout,
  mdiInformationOutline,
  mdiLogin,
  mdiAccountPlus,
  mdiAccountCog
} from '@mdi/js';

const drawer = ref(false);
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

onMounted(() => {
  authStore.checkTokenExpiration();
});

const allMenuItems = ref([
  { title: 'Hlavní Panel', icon: mdiViewDashboard, path: '/', requiresAuth: false },
  { title: 'O Aplikaci', icon: mdiInformationOutline, path: '/about', requiresAuth: false },
  { title: 'Servery', icon: mdiServer, path: '/servers', requiresAuth: true },
  { title: 'Ankety', icon: mdiPoll, path: '/polls', requiresAuth: true },
  { title: 'Statistiky', icon: mdiChartBar, path: '/stats', requiresAuth: true },
]);

const filteredMenuItems = computed(() => {
  if (authStore.isLoggedIn) {
    return allMenuItems.value;
  } else {
    return allMenuItems.value.filter(item => !item.requiresAuth);
  }
});

const navigateTo = async (path: string) => {
  const menuItem = allMenuItems.value.find(item => item.path === path);
  // Pokud je to hlavní název stránky (path === '/'), vždy naviguj
  if (path === '/') {
      router.push(path);
      drawer.value = false; // Zavřít drawer, pokud byl otevřený
      return;
  }

  if (menuItem?.requiresAuth && !authStore.isLoggedIn) {
    drawer.value = false; 
    const loginResult = await displayLoginModal();
    if (loginResult && loginResult.success) {
      router.push(path);
    } else {
      if (route.path !== '/') router.push('/');
    }
    return;
  }
  router.push(path);
  drawer.value = false;
};

const isActiveRoute = (path: string) => {
  return route.path === path;
};

const iconColor = (path: string) => {
  // Pro ikonu odhlášení v uživatelském menu můžeme chtít jinou logiku, pokud je potřeba
  if (path === '/logout') return 'text-text-primary'; // Nebo specifická barva pro logout
  return isActiveRoute(path) ? 'primary' : 'text-primary';
};

const handleLogin = async () => {
  await displayLoginModal();
};

const handleRegister = async () => {
  await displayRegisterModal();
};

const handleLogout = async () => {
  await authStore.logout(); 
  router.push('/'); 
};

const avatarImageError = (event: Event) => {
  const imgElement = event.target as HTMLImageElement;
  imgElement.src = 'https://placehold.co/36x36/7F00FF/E0E0E0?text=' + authStore.userInitials;
};

const showWelcomeMessage = computed(() => {
    return route.path === '/' && !authStore.isLoggedIn;
});
const showDashboardContent = computed(() => {
    return route.path === '/' && authStore.isLoggedIn;
});

</script>

<style scoped>
/* Scoped styles specific to App.vue, additional global styles are in futuristic-theme.css */

.app-title-clickable {
  cursor: pointer; /* Add pointer cursor to indicate it's clickable */
  transition: opacity 0.2s ease-in-out;
}
.app-title-clickable:hover {
  opacity: 0.8; /* Slight visual feedback on hover */
}

.page-title {
  font-size: 2.5rem;
  font-weight: 700;
  letter-spacing: 1px;
  text-shadow: 0 0 8px var(--v-theme-glow-color);
}

.card-title {
  color: var(--v-theme-primary);
  border-bottom: 1px solid rgba(var(--v-theme-border-color-rgb), var(--v-border-opacity));
  padding-bottom: 0.5em;
  font-size: 1.25rem;
}

.card-text p {
  margin-bottom: 0.5em;
  color: var(--v-theme-text-primary);
  font-size: 0.95rem;
}
.card-text p .text-success {
  color: var(--v-theme-success) !important;
  font-weight: bold;
}
.card-text p .text-warning {
  color: var(--v-theme-warning) !important;
  font-weight: bold;
}
.card-text ul {
  list-style-type: none;
  padding-left: 0;
}
.card-text li::before {
  content: "»"; 
  color: var(--v-theme-primary);
  margin-right: 0.5em;
  font-weight: bold;
}

.futuristic-card {
  background-color: var(--v-theme-surface);
  border: 1px solid rgba(var(--v-theme-border-color-rgb), var(--v-border-opacity));
  transition: all 0.3s ease-in-out;
  position: relative;
  overflow: hidden;
}

.futuristic-card:hover {
  border-color: var(--v-theme-primary);
  box-shadow: 0 0 15px 2px var(--v-theme-glow-color), 0 4px 20px rgba(0,0,0,0.3) !important;
  transform: translateY(-3px);
}

.hexagon-decoration {
  position: absolute;
  width: 50px; 
  height: 28.87px; 
  background-color: rgba(var(--v-theme-primary-rgb), 0.05); 
  clip-path: polygon(25% 0%, 75% 0%, 100% 50%, 75% 100%, 25% 100%, 0% 50%);
  opacity: 0.5;
  pointer-events: none;
}

.hexagon-decoration.top-left { top: -10px; left: -15px; transform: rotate(30deg); }
.hexagon-decoration.top-right { top: -10px; right: -15px; transform: rotate(-30deg); }
.hexagon-decoration.bottom-left { bottom: -10px; left: -15px; transform: rotate(-30deg); }
.hexagon-decoration.bottom-right { bottom: -10px; right: -15px; transform: rotate(30deg); }


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

.futuristic-btn[variant="tonal"] {
    background-color: rgba(var(--v-theme-primary-rgb), 0.15) !important;
    color: var(--v-theme-primary) !important;
}
.futuristic-btn[variant="tonal"]:hover {
    background-color: rgba(var(--v-theme-primary-rgb), 0.25) !important;
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

.futuristic-app-bar {
  border-bottom: 1px solid rgba(var(--v-theme-primary-rgb), 0.3) !important;
}

.futuristic-drawer {
   border-right: 1px solid rgba(var(--v-theme-primary-rgb), 0.2) !important;
}

/* Navigation drawer menu item styling */
.menu-item .v-list-item-title {
  font-family: 'Exo 2', sans-serif;
  transition: color 0.2s ease-in-out;
}

.menu-item {
  border-left: 3px solid transparent;
  transition: all 0.2s ease-in-out;
}

.menu-item:not(.v-list-item--active) .v-list-item-title {
  color: var(--v-theme-text-primary) !important;
}
.menu-item:not(.v-list-item--active) .v-icon {
  color: var(--v-theme-text-primary) !important;
}

.menu-item:not(.v-list-item--active):hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.08);
  border-left-color: var(--v-theme-primary);
}
.menu-item:not(.v-list-item--active):hover .v-list-item-title,
.menu-item:not(.v-list-item--active):hover .v-icon {
  color: var(--v-theme-primary) !important;
}

.menu-item.v-list-item--active {
  border-left-color: var(--v-theme-primary) !important;
  background-color: rgba(var(--v-theme-primary-rgb), 0.12) !important;
}
.menu-item.v-list-item--active .v-list-item-title,
.menu-item.v-list-item--active .v-icon {
  color: var(--v-theme-primary) !important;
}


.futuristic-footer {
  border-top: 1px solid rgba(var(--v-theme-primary-rgb), 0.2) !important;
  font-size: 0.875rem;
}

.futuristic-menu .v-list-item-title {
  font-size: 0.95rem !important;
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

.fade-transform-leave-active,
.fade-transform-enter-active {
  transition: all .3s ease;
}
.fade-transform-enter-from {
  opacity: 0;
  transform: translateX(-20px);
}
.fade-transform-leave-to {
  opacity: 0;
  transform: translateX(20px);
}
</style>
