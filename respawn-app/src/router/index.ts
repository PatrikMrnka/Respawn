import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import HomeView from '../views/HomeView.vue'
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

// Define all application routes
const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'home',
    component: HomeView,
    meta: { requiresAuth: false }, // Public route - authentication not required
  },
  {
    path: '/profile',
    name: 'profile',
    component: UserProfileView,
    meta: { requiresAuth: true }, // Protected route - requires authentication
  },
  {
    path: '/servers',
    name: 'servers',
    component: ServersView,
    meta: { requiresAuth: true }, // Protected route - requires authentication
  },
  {
    path: '/servers/:id',
    name: 'server-detail',
    component: GameServerDetailView,
    props: true, // Route params are passed as props to the component
    meta: { requiresAuth: true }, // Protected route - requires authentication
  },
  {
    path: '/statistics',
    name: 'statistics',
    component: StatisticsView,
    meta: { requiresAuth: false }, // Public route - authentication not required
  },
  {
    path: '/about',
    name: 'about',
    component: AboutView,
    meta: { requiresAuth: false }, // Public route - authentication not required
  },
  {
    path: '/admin',
    name: 'admin',
    component: AdminView,
    meta: {
      requiresAuth: true, // Protected route - requires authentication
      roles: [UserRoles.Administrator, UserRoles.Spravce], // Restricted to specific user roles
    },
  },
  {
    path: '/users',
    name: 'users',
    component: UsersView,
    meta: {
      requiresAuth: true, // Protected route - requires authentication
      roles: [UserRoles.Administrator, UserRoles.Spravce], // Restricted to specific user roles
    },
  },
  {
    path: '/polls',
    name: 'polls',
    component: PollsView,
    meta: {
      requiresAuth: true, // Protected route - requires authentication
    },
  },
]

// Create router instance
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

// Navigation guard to protect routes and handle authentication
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  // Restore authentication state from localStorage if token exists
  if (!authStore.token && localStorage.getItem('authToken')) {
    authStore.token = localStorage.getItem('authToken')
    authStore.user = JSON.parse(localStorage.getItem('authUser') || 'null')
    authStore.isAuthenticated = true
    authStore.expiresAt = localStorage.getItem('authExpiresAt')
      ? new Date(localStorage.getItem('authExpiresAt')!)
      : null
    authStore.checkTokenExpiration()
  }

  const userIsLoggedIn = authStore.isLoggedIn
  const userRoles = authStore.user?.roles || []

  // Handle protected routes when user is not authenticated
  if (to.meta.requiresAuth && !userIsLoggedIn) {
    // Display login modal and handle the authentication flow
    displayLoginModal()
      .then((result) => {
        if (result && result.success) {
          const updatedUserRoles = authStore.user?.roles || [] // Re-check roles after login
          // Check if user has required role after logging in
          if (
            to.meta.roles &&
            Array.isArray(to.meta.roles) &&
            !(to.meta.roles as string[]).some((role) => updatedUserRoles.includes(role))
          ) {
            next({ name: 'home' }) // Redirect if role still not sufficient
          } else {
            next() // Proceed to the originally intended route
          }
        } else {
          next({ name: 'home' }) // Redirect to home if login fails or is cancelled
        }
      })
      .catch(() => {
        next({ name: 'home' }) // Handle modal promise rejection
      })
  }
  // Handle case when user is logged in but doesn't have required role
  else if (
    to.meta.requiresAuth &&
    to.meta.roles &&
    Array.isArray(to.meta.roles) &&
    !(to.meta.roles as string[]).some((role) => userRoles.includes(role))
  ) {
    console.warn(
      `User doesn't have permission for ${to.path}. Required roles: ${(to.meta.roles as string[]).join(', ')}. User roles: ${userRoles.join(', ')}`,
    )
    next({ name: 'home' }) // Redirect to home page
  } else {
    next() // Proceed if no auth required, or auth requirements are satisfied
  }
})

export default router
