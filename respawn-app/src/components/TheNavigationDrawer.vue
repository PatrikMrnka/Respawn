<template>
  <v-navigation-drawer
    app
    :permanent="!isMobile"
    :temporary="isMobile"
    v-model="drawer"
    class="futuristic-drawer"
  >
    <v-list nav dense>
      <v-list-item class="logo-item pa-2 mb-2">
        <template v-slot:prepend>
          <v-icon size="large" color="primary" :icon="mdiRocketLaunchOutline"></v-icon>
        </template>
        <v-list-item-title class="text-h5 font-oxanium ml-2">
          Respawn App
        </v-list-item-title>
      </v-list-item>

      <v-divider></v-divider>

      <v-list-item
        :prepend-icon="mdiHomeOutline"
        title="Domů"
        to="/"
        exact
        class="futuristic-list-item"
      ></v-list-item>

      <v-list-item
        v-if="authStore.isLoggedIn"
        :prepend-icon="mdiAccountCircleOutline"
        title="Profil"
        to="/profile"
        class="futuristic-list-item"
      ></v-list-item>

      <v-list-item
        v-if="authStore.isLoggedIn"
        :prepend-icon="mdiServerNetwork"
        title="Servery"
        to="/servers"
        class="futuristic-list-item"
      ></v-list-item>

      <v-list-item
        v-if="authStore.isLoggedIn"
        :prepend-icon="mdiPoll" title="Ankety"
        to="/polls"
        class="futuristic-list-item"
      ></v-list-item>

      <v-list-item
        :prepend-icon="mdiChartLine"
        title="Statistiky"
        to="/statistics"
        class="futuristic-list-item"
      ></v-list-item>

      <v-list-item
        :prepend-icon="mdiInformationOutline"
        title="O aplikaci"
        to="/about"
        class="futuristic-list-item"
      ></v-list-item>

      <v-divider v-if="isUserAdmin || isUserSpravce" class="my-2"></v-divider>

      <v-list-item
        v-if="isUserAdmin"
        :prepend-icon="mdiShieldCrownOutline"
        title="Administrace"
        to="/admin"
        class="futuristic-list-item admin-link"
      ></v-list-item>

      <v-list-item
        v-if="isUserSpravce"
        :prepend-icon="mdiAccountGroupOutline"
        title="Uživatelé"
        to="/users"
        class="futuristic-list-item spravce-link"
      ></v-list-item>

    </v-list>

    <template v-slot:append>
      <div class="pa-2">
        <v-btn
          v-if="authStore.isLoggedIn"
          block
          @click="handleLogout"
          color="error"
          variant="outlined"
          :prepend-icon="mdiLogout"
          class="futuristic-btn"
        >
          Odhlásit se
        </v-btn>
      </div>
    </template>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useAuthStore } from '@/stores/authStore';
import { UserRoles } from '@/types/enums';
// Explicitní import MDI ikon
import {
  mdiRocketLaunchOutline,
  mdiHomeOutline,
  mdiAccountCircleOutline,
  mdiServerNetwork,
  mdiChartLine,
  mdiInformationOutline,
  mdiShieldCrownOutline,
  mdiAccountGroupOutline,
  mdiLogout,
  mdiPoll // <-- Přidáno: Ikona pro ankety
} from '@mdi/js';

const authStore = useAuthStore();

const drawer = ref(!isMobileDevice());
const isMobile = ref(isMobileDevice());

function isMobileDevice() {
  if (typeof window !== 'undefined') {
    return window.innerWidth < 960;
  }
  return false;
}

const handleResize = () => {
  isMobile.value = isMobileDevice();
  if (!isMobile.value) {
    drawer.value = true;
  }
};

onMounted(() => {
  if (typeof window !== 'undefined') {
    window.addEventListener('resize', handleResize);
    handleResize();
  }
});

onBeforeUnmount(() => {
  if (typeof window !== 'undefined') {
    window.removeEventListener('resize', handleResize);
  }
});

const isUserAdmin = computed(() => {
  return authStore.isLoggedIn && authStore.user?.roles?.includes(UserRoles.Administrator);
});

const isUserSpravce = computed(() => {
  return authStore.isLoggedIn && authStore.user?.roles?.includes(UserRoles.Spravce);
});

const handleLogout = async () => {
  await authStore.logout();
};

</script>

<style scoped>
.futuristic-drawer {
  background-color: var(--v-theme-surface);
  border-right: 1px solid rgba(var(--v-theme-primary-rgb), 0.15);
}

.futuristic-list-item:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.1);
}
.futuristic-list-item.v-list-item--active {
  background-color: rgba(var(--v-theme-primary-rgb), 0.15);
  border-left: 3px solid var(--v-theme-primary);
}
.futuristic-list-item .v-list-item-title {
  font-family: var(--font-family-headings-exo2);
  font-size: 0.95rem;
}
.admin-link .v-list-item-title {
  /* color: var(--v-theme-primary); */
}
.spravce-link .v-list-item-title {
   /* color: var(--v-theme-secondary); */
}
.logo-item .v-list-item-title {
 color: var(--v-theme-primary);
}
</style>
