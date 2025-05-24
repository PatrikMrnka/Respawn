// File: haha/respawn-app/src/router/index.ts
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import HomeView from '../views/HomeView.vue' // Assuming .vue, but user provided .txt
import UserProfileView from '../views/UserProfileView.vue'
import ServersView from '../views/ServersView.vue'
import StatisticsView from '@/views/StatisticsView.vue'
import AboutView from '@/views/AboutView.vue'
import AdminView from '@/views/AdminView.vue'
import UsersView from '@/views/UsersView.vue'
import PollsView from '@/views/PollsView.vue'
import GameServerDetailView from '@/views/GameServerDetailView.vue'
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
    meta : { requiresAuth: true }, // Assuming only logged-in users can see server list
  },
  { // <-- ADD THIS ROUTE
    path: '/servers/:id',
    name: 'server-detail',
    component: GameServerDetailView,
    props: true, // Pass route params as props to the component
    meta: { requiresAuth: true }, // Assuming only logged-in users can see details
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
      roles: [UserRoles.Administrator, UserRoles.Spravce] // Adjusted to include Spravce if they can access parts of admin
    },
  },
  {
    path: '/users',
    name: 'users',
    component: UsersView,
    meta: {
      requiresAuth: true,
      roles: [UserRoles.Administrator, UserRoles.Spravce] // Assuming Spravce can manage users
    },
  },
  {
    path: '/polls',
    name: 'polls',
    component: PollsView,
    meta: {
      requiresAuth: true
    },
  }
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

// Navigation Guard (remains the same, but ensure it handles roles correctly for new admin routes if needed)
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  // Initialize authStore from localStorage if not already done
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
        const updatedUserRoles = authStore.user?.roles || []; // Re-check roles after login
        if (to.meta.roles && Array.isArray(to.meta.roles) && !(to.meta.roles as string[]).some(role => updatedUserRoles.includes(role))) {
          next({ name: 'home' }); // Redirect if role still not sufficient
        } else {
          next(); // Proceed to the originally intended route
        }
      } else {
        next({ name: 'home' }); // Stay on home or redirect to a public page if login fails/cancelled
      }
    }).catch(() => {
      next({ name: 'home' }); // Handle modal promise rejection
    });
  } else if (to.meta.requiresAuth && to.meta.roles && Array.isArray(to.meta.roles) && !(to.meta.roles as string[]).some(role => userRoles.includes(role))) {
    // User is logged in but does not have the required role
    console.warn(`Uživatel nemá oprávnění pro ${to.path}. Požadované role: ${(to.meta.roles as string[]).join(', ')}. Role uživatele: ${userRoles.join(', ')}`);
    next({ name: 'home' }); // Redirect to home or an "access denied" page
  }
  else {
    next(); // Proceed if no auth required, or auth satisfied
  }
})

export default router
