import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import UserProfileView from '../views/UserProfileView.vue'
import { useAuthStore } from '@/stores/authStore'
import { displayLoginModal } from '@/services/authService'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'home',
    component: HomeView,
  },
  {
    path: '/profile',
    name: 'profile',
    component: UserProfileView,
    meta: { requiresAuth: true }, // Označení, že tato routa vyžaduje přihlášení
  },
  // TODO: další routy pro /servers, /polls, /stats, /settings až budou implementovány
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

// Navigation Guard
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  // Zkontrolujte, zda je token platný při každé navigaci (pokud ještě není načten stav)
  if (!authStore.token && localStorage.getItem('authToken')) {
    authStore.token = localStorage.getItem('authToken')
    authStore.user = JSON.parse(localStorage.getItem('authUser') || 'null')
    authStore.isAuthenticated = true
    authStore.expiresAt = localStorage.getItem('authExpiresAt')
      ? new Date(localStorage.getItem('authExpiresAt')!)
      : null
    authStore.checkTokenExpiration() // Zkontroluje, zda token nevypršel
  }

  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    displayLoginModal().then((result) => {
      if (result && result.success) {
        next() // Pokračovat na původní cíl po úspěšném přihlášení
      } else {
        next({ name: 'home' }) // Zůstat na home, pokud se nepřihlásí
      }
    })
  } else {
    next() // Pokračovat v navigaci
  }
})

export default router
