<template>
  <v-app class="futuristic-app">
    <v-app-bar app color="surface" elevation="4" class="futuristic-app-bar">
      <v-app-bar-nav-icon @click.stop="drawer = !drawer" class="text-primary"></v-app-bar-nav-icon>
      <v-toolbar-title class="font-oxanium text-primary">Respawn LAN Organizer</v-toolbar-title>
      <v-spacer></v-spacer>
      <v-btn icon class="text-primary">
        <v-icon>{{ mdiAccountCircle }}</v-icon>
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
              Vyberte si jednu z možností v navigačním menu.
            </p>
          </v-col>
        </v-row>
         <v-row class="mt-8" v-if="!$route.matched.length">
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
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router'; // Import Vue Router composables

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
  mdiInformationOutline // Added for About page
} from '@mdi/js';

const drawer = ref(false);
const router = useRouter();
const route = useRoute();

const menuItems = ref([
  { title: 'Hlavní Panel', icon: mdiViewDashboard, path: '/' },
  { title: 'O Aplikaci', icon: mdiInformationOutline, path: '/about' },
  { title: 'Servery', icon: mdiServer, path: '/servers' }, // Example path
  { title: 'Ankety', icon: mdiPoll, path: '/polls' }, // Example path
  { title: 'Statistiky', icon: mdiChartBar, path: '/stats' }, // Example path
  { title: 'Nastavení', icon: mdiCogs, path: '/settings' }, // Example path
  { title: 'Odhlásit se', icon: mdiLogout, path: '/logout' }, // Example path
]);

const navigateTo = (path: string) => {
  router.push(path);
  drawer.value = false; // Close drawer on navigation
};

const isActiveRoute = (path: string) => {
  return route.path === path;
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
  color: white;
  border-bottom: 1px solid rgba(var(--v-theme-border-color-rgb), var(--v-border-opacity));
  padding-bottom: 0.5em;
  font-size: 1.25rem; /* Adjusted for consistency */
  
  text-shadow: 0 0 8px var(--v-theme-glow-color);
}

.card-text p {
  margin-bottom: 0.5em;
  color: white;
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
  height: 28.87px; /* width * sqrt(3)/2 / 2 for half hexagon if needed, or full height */
  background-color: rgba(var(--v-theme-primary-rgb), 0.05); /* Very subtle */
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

.futuristic-btn[variant="tonal"] { /* Specific style for tonal buttons if needed */
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
  color: white !important; 
  border-left: 3px solid transparent;
  transition: all 0.2s ease-in-out;
}
.menu-item:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.08);
  color: var(--v-theme-primary) !important;
  border-left-color: var(--v-theme-primary);
}
.menu-item.v-list-item--active {
  color: white !important;
  border-left-color: var(--v-theme-primary) !important;
  background-color: rgba(var(--v-theme-primary-rgb), 0.12) !important;
}
.menu-item .v-icon {
  /* Changed icon color to --v-theme-text-primary for a whiter appearance */
  color: white !important; 
  transition: color 0.2s ease-in-out;
}
.menu-item:hover .v-icon, .menu-item.v-list-item--active .v-icon {
   color: white !important;
}

.futuristic-footer {
  border-top: 1px solid rgba(var(--v-theme-primary-rgb), 0.2) !important;
  font-size: 0.875rem;
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
