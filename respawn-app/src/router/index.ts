// src/router/index.ts
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import UserProfileView from '../views/UserProfileView.vue'
import ServersView from '../views/ServersView.vue'
import StatisticsView from '@/views/StatisticsView.vue'
import AboutView from '@/views/AboutView.vue'
import AdminView from '@/views/AdminView.vue'
import UsersView from '@/views/UsersView.vue'
import PollsView from '@/views/PollsView.vue';
import { useAuthStore } from '@/stores/authStore'
import { displayLoginModal } from '@/services/authService'
import { UserRoles } from '@/types/enums'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'home',
    component: HomeView,
    meta: { requiresAuth: false },
  },
  {
    path: '/profile',
    name: 'profile',
    component: UserProfileView,
    meta: { requiresAuth: true },
  },
  {
    path : '/servers',
    name : 'servers',
    component : ServersView,
    meta : { requiresAuth: true },
  },
  {
    path: '/statistics',
    name: 'statistics',
    component: StatisticsView,
    meta: { requiresAuth: false },
  },
  {
    path: '/about',
    name: 'about',
    component: AboutView,
    meta: { requiresAuth: false },
  },
  {
    path: '/admin',
    name: 'admin',
    component: AdminView,
    meta: {
      requiresAuth: true,
      roles: [UserRoles.Administrator]
    },
  },
  {
    path: '/users',
    name: 'users',
    component: UsersView,
    meta: {
      requiresAuth: true,
      roles: [UserRoles.Spravce]
    },
  },
  {
    path: '/polls',
    name: 'polls',
    component: PollsView,
    meta: {
      requiresAuth: true // Ankety jsou pro přihlášené uživatele
    },
  }
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

// Navigation Guard
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  if (!authStore.token && localStorage.getItem('authToken')) {
    authStore.token = localStorage.getItem('authToken')
    authStore.user = JSON.parse(localStorage.getItem('authUser') || 'null')
    authStore.isAuthenticated = true
    authStore.expiresAt = localStorage.getItem('authExpiresAt')
      ? new Date(localStorage.getItem('authExpiresAt')!)
      : null
    authStore.checkTokenExpiration()
  }

  const userIsLoggedIn = authStore.isLoggedIn;
  const userRoles = authStore.user?.roles || [];

  if (to.meta.requiresAuth && !userIsLoggedIn) {
    displayLoginModal().then((result) => {
      if (result && result.success) {
        const updatedUserRoles = authStore.user?.roles || [];
        if (to.meta.roles && Array.isArray(to.meta.roles) && !(to.meta.roles as string[]).some(role => updatedUserRoles.includes(role))) {
          next({ name: 'home' });
        } else {
          next();
        }
      } else {
        next({ name: 'home' });
      }
    });
  } else if (to.meta.requiresAuth && to.meta.roles && Array.isArray(to.meta.roles) && !(to.meta.roles as string[]).some(role => userRoles.includes(role))) {
    console.warn(`Uživatel nemá oprávnění pro ${to.path}. Požadované role: ${(to.meta.roles as string[]).join(', ')}. Role uživatele: ${userRoles.join(', ')}`);
    next({ name: 'home' });
  }
  else {
    next();
  }
})

export default router
