<template>
  <v-navigation-drawer
    :model-value="modelValue"
    @update:model-value="emitUpdateModelValue"
    app
    class="futuristic-drawer"
    width="280"
  >
    <v-list nav dense>
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
        :prepend-icon="mdiPoll"
        title="Ankety"
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
      <div class="pa-3">
        <v-btn
          v-if="authStore.isLoggedIn"
          block
          @click="handleLogoutAndCloseDrawer"
          color="error"
          variant="flat"
          :prepend-icon="mdiLogout"
          class="futuristic-btn error-btn"
        >
          Odhlásit se
        </v-btn>
      </div>
    </template>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { UserRoles } from '@/types/enums'
// import { useDisplay } from 'vuetify'; // Již není potřeba pro closeDrawerOnMobile
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
  mdiPoll,
} from '@mdi/js'

const props = defineProps({
  modelValue: Boolean,
})

const emit = defineEmits(['update:modelValue'])

const authStore = useAuthStore()
// const { mobile } = useDisplay(); // Již není explicitně potřeba pro zavírání

const emitUpdateModelValue = (value: boolean) => {
  emit('update:modelValue', value)
}

const isUserAdmin = computed(() => {
  return authStore.isLoggedIn && authStore.user?.roles?.includes(UserRoles.Administrator)
})

const isUserSpravce = computed(() => {
  return authStore.isLoggedIn && authStore.user?.roles?.includes(UserRoles.Spravce)
})

// Funkce closeDrawerOnMobile byla odstraněna, protože drawer se nyní zavírá pouze přes hamburger ikonu
// Pokud byste chtěli, aby se na mobilu stále zavíral po kliknutí na odkaz, mohli byste ji vrátit a
// podmíněně ji volat v @click handleru v-list-item.

const handleLogoutAndCloseDrawer = async () => {
  await authStore.logout()
  emitUpdateModelValue(false) // Zavřít drawer po odhlášení
}
</script>

<style scoped>
.futuristic-drawer {
  background-color: var(--v-theme-surface) !important;
  border-right: 1px solid rgba(var(--v-theme-primary-rgb), 0.2);
  display: flex;
  flex-direction: column;
}

.logo-item .v-list-item-title {
  color: var(--v-theme-primary);
  font-weight: bold;
}

.futuristic-list-item {
  margin: 4px 8px;
  border-radius: 6px;
  transition:
    background-color 0.2s ease-in-out,
    color 0.2s ease-in-out;
}

.futuristic-list-item:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.1);
}
.futuristic-list-item.v-list-item--active {
  background-color: rgba(var(--v-theme-primary-rgb), 0.15) !important;
  color: var(--v-theme-primary) !important;
  border-left: 4px solid var(--v-theme-primary);
}
.futuristic-list-item.v-list-item--active .v-icon {
  color: var(--v-theme-primary) !important;
}

.futuristic-list-item .v-list-item-title {
  font-family: var(--font-family-headings-exo2);
  font-size: 0.98rem;
  font-weight: 500;
}
.admin-link .v-list-item-title,
.admin-link.v-list-item--active .v-list-item-title {
  /* color: var(--v-theme-warning); */
}
.spravce-link .v-list-item-title,
.spravce-link.v-list-item--active .v-list-item-title {
  /* color: var(--v-theme-info); */
}

.v-list {
  flex-grow: 1;
  overflow-y: auto;
}

.futuristic-btn.error-btn {
  background-color: var(--v-theme-error) !important;
  color: white !important;
  border: none;
  box-shadow: 0 2px 8px rgba(var(--v-theme-error-rgb), 0.4);
}
.futuristic-btn.error-btn:hover {
  background-color: var(--v-theme-error-darken-1) !important;
  box-shadow: 0 4px 15px rgba(var(--v-theme-error-rgb), 0.6);
  transform: translateY(-2px);
}
</style>
