<template>
  <v-app class="futuristic-app">
    <TheAppBar
      @toggle-drawer="drawer = !drawer"
      @login="handleLogin"
      @register="handleRegister"
      @logout="handleLogout"
      @navigate="navigateTo"
      @navigate-home="navigateTo('/')"
    />

    <TheNavigationDrawer
      :drawer-visible="drawer"
      @update:drawerVisible="drawer = $event"
      :menu-items="filteredMenuItems"
      @navigate="navigateTo"
    />

    <v-main>
      <v-container fluid class="pa-md-8 pa-4">
        <router-view v-slot="{ Component }">
          <transition name="fade-transform" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>

        <WelcomeMessage v-if="showWelcomeMessage" @login="handleLogin" @register="handleRegister" />
        <DashboardFallback v-if="showDashboardContent" />
      </v-container>
    </v-main>

    <TheAppFooter />
  </v-app>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { displayLoginModal, displayRegisterModal } from '@/services/authService'
import { useAuthStore } from '@/stores/authStore'

import TheAppBar from '@/components/TheAppBar.vue'
import TheNavigationDrawer from '@/components/TheNavigationDrawer.vue'
import TheAppFooter from '@/components/TheAppFooter.vue'
import WelcomeMessage from '@/components/WelcomeMessage.vue'
import DashboardFallback from '@/components/DashboardFallback.vue'

import {
  mdiViewDashboard,
  mdiInformationOutline,
  mdiAccountCog,
  mdiServer,
  mdiPoll,
  mdiChartBar,
} from '@mdi/js'

const drawer = ref(false)
const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

onMounted(() => {
  authStore.checkTokenExpiration()
})

const allMenuItems = ref([
  { title: 'Hlavní Panel', icon: mdiViewDashboard, path: '/', requiresAuth: false },
  { title: 'O Aplikaci', icon: mdiInformationOutline, path: '/about', requiresAuth: false },
  { title: 'Servery', icon: mdiServer, path: '/servers', requiresAuth: true },
  { title: 'Ankety', icon: mdiPoll, path: '/polls', requiresAuth: true },
  { title: 'Statistiky', icon: mdiChartBar, path: '/stats', requiresAuth: true },
])

const filteredMenuItems = computed(() => {
  if (authStore.isLoggedIn) {
    return allMenuItems.value
  } else {
    return allMenuItems.value.filter((item) => !item.requiresAuth)
  }
})

const navigateTo = async (path: string) => {
  const menuItem = allMenuItems.value.find((item) => item.path === path)

  if (path === '/') {
    router.push(path)
    if (drawer.value) drawer.value = false
    return
  }

  if (menuItem?.requiresAuth && !authStore.isLoggedIn) {
    if (drawer.value) drawer.value = false
    const loginResult = await displayLoginModal()
    if (loginResult && loginResult.success) {
      router.push(path)
    } else {
      if (route.path !== '/') router.push('/')
    }
    return
  }
  router.push(path)
  if (drawer.value) drawer.value = false
}

const handleLogin = async () => {
  await displayLoginModal()
}

const handleRegister = async () => {
  await displayRegisterModal()
}

const handleLogout = async () => {
  await authStore.logout()
  router.push('/')
}
console.log(route.matched.length)
// Podmínky pro zobrazení fallback obsahu
const showWelcomeMessage = computed(() => {
  // Zobrazit, pokud je na '/' a není žádná shoda v routeru A uživatel není přihlášen
  return (
    route.path === '/' &&
    (route.matched.length === 0 || route.matched.length === 1) &&
    !authStore.isLoggedIn
  )
})
const showDashboardContent = computed(() => {
  // Zobrazit, pokud je na '/' A není žádná shoda v routeru A uživatel JE přihlášen
  return route.path === '/' && route.matched.length === 0 && authStore.isLoggedIn
})
</script>

<style scoped>
.fade-transform-leave-active,
.fade-transform-enter-active {
  transition: all 0.3s ease;
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
