<template>
  <v-app class="futuristic-app">
    <v-app-bar app color="surface" elevation="4" class="futuristic-app-bar">
      <v-app-bar-nav-icon @click.stop="drawer = !drawer" class="text-primary"></v-app-bar-nav-icon>
      <v-toolbar-title class="font-oxanium text-primary">Respawn LAN Organizer</v-toolbar-title>
      <v-spacer></v-spacer>

      <v-btn class="futuristic-btn mr-2" variant="outlined" @click="handleLogin">
        <v-icon left class="mr-1">{{ mdiLogin }}</v-icon>
        Přihlásit se
      </v-btn>
      <v-btn class="futuristic-btn-filled" color="primary" @click="handleRegister">
         <v-icon left class="mr-1">{{ mdiAccountPlus }}</v-icon>
        Registrovat
      </v-btn>
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
          v-for="(item, i) in menuItems"
          :key="i"
          :prepend-icon="item.icon"
          :title="item.title"
          @click="navigateTo(item.path)"
          class="menu-item"
          :active="isActiveRoute(item.path)"
        ></v-list-item>
      </v-list>
    </v-navigation-drawer>

    <v-main>
      <v-container fluid class="pa-md-8 pa-4">
        <router-view v-slot="{ Component }">
          <transition name="fade-transform" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>

        <v-row v-if="!$route.matched.length">
          <v-col cols="12">
            <h1 class="font-exo2 page-title text-primary mb-6">Vítejte!</h1>
            <p class="font-inter text-text-secondary">
              Vyberte si jednu z možností v navigačním menu, nebo se přihlaste/zaregistrujte.
            </p>
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
import { ref, computed } from 'vue'; // Added computed for userInitials example
import { useRouter, useRoute } from 'vue-router';
import { displayLoginModal, displayRegisterModal } from '@/services/authService'; // Import auth service
// TODO: Import Pinia store for auth state if you create one
// import { useAuthStore } from '@/stores/authStore';

// MDI SVG Icons import
import { 
  mdiAccountCircle, 
  mdiServer, 
  mdiPoll, 
  mdiChartBar, 
  mdiCog, 
  mdiViewDashboard, 
  mdiCogs, 
  mdiLogout,
  mdiInformationOutline,
  mdiLogin, // Icon for Login
  mdiAccountPlus, // Icon for Register
  mdiAccountCog // Icon for Profile in user menu
} from '@mdi/js';

const drawer = ref(false);
const router = useRouter();
const route = useRoute();
// const authStore = useAuthStore(); // Example if using Pinia

// Example: Replace with actual logic from authStore
const isUserLoggedIn = ref(false); // Reactive ref for login state
const userNickname = ref(''); // Reactive ref for user nickname

// Example for user initials, replace with actual data
const userInitials = computed(() => {
  return userNickname.value ? userNickname.value.substring(0, 2).toUpperCase() : '??';
});


const menuItems = ref([
  { title: 'Hlavní Panel', icon: mdiViewDashboard, path: '/' },
  { title: 'O Aplikaci', icon: mdiInformationOutline, path: '/about' },
  { title: 'Servery', icon: mdiServer, path: '/servers' },
  { title: 'Ankety', icon: mdiPoll, path: '/polls' },
  { title: 'Statistiky', icon: mdiChartBar, path: '/stats' },
  { title: 'Nastavení', icon: mdiCogs, path: '/settings' },
  // Conditional menu item for logout - will be shown/hidden based on login state
  // { title: 'Odhlásit se', icon: mdiLogout, path: '/logout', requiresAuth: true },
]);

const navigateTo = (path: string) => {
  router.push(path);
  drawer.value = false;
};

const isActiveRoute = (path: string) => {
  return route.path === path;
};

const handleLogin = async () => {
  const result = await displayLoginModal();
  if (result && result.success) {
    // TODO: Update authStore, set isUserLoggedIn = true, userNickname.value = result.nickname
    // For now, simulate login
    isUserLoggedIn.value = true;
    userNickname.value = result.nickname || 'Hráč';
    console.log("Přihlášen jako:", userNickname.value);
    // Potentially add a logout item to menuItems or handle it differently
  }
};

const handleRegister = async () => {
  const result = await displayRegisterModal();
   if (result && result.success) {
    // TODO: Update authStore, potentially auto-login or show success message
    console.log("Registrován:", result.nickname);
  }
};

const handleLogout = () => {
  // TODO: Implement logout logic (clear token, update authStore)
  isUserLoggedIn.value = false;
  userNickname.value = '';
  router.push('/'); // Redirect to home or login page
  console.log("Uživatel odhlášen");
};

</script>

<style scoped>
/* Scoped styles specific to App.vue, additional global styles are in futuristic-theme.css */

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
  font-size: 1.25rem; /* Adjusted for consistency */
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
  list-style-type: none; /* Remove default bullets */
  padding-left: 0;
}
.card-text li::before {
  content: "»"; /* Hexagonal or futuristic bullet */
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

/* Hexagon decorations for cards - subtle */
.hexagon-decoration {
  position: absolute;
  width: 50px; /* Adjusted size */
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

.menu-item {
  font-family: 'Exo 2', sans-serif;
  color: var(--v-theme-text-primary) !important; 
  border-left: 3px solid transparent;
  transition: all 0.2s ease-in-out;
}
.menu-item:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.08);
  color: var(--v-theme-primary) !important;
  border-left-color: var(--v-theme-primary);
}
.menu-item.v-list-item--active {
  color: var(--v-theme-primary) !important;
  border-left-color: var(--v-theme-primary) !important;
  background-color: rgba(var(--v-theme-primary-rgb), 0.12) !important;
}
.menu-item .v-icon {
  color: var(--v-theme-text-primary) !important; 
  transition: color 0.2s ease-in-out;
}
.menu-item:hover .v-icon, .menu-item.v-list-item--active .v-icon {
   color: var(--v-theme-primary) !important;
}

.futuristic-footer {
  border-top: 1px solid rgba(var(--v-theme-primary-rgb), 0.2) !important;
  font-size: 0.875rem;
}

/* User menu dropdown styles */
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


/* Transition for router-view */
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
