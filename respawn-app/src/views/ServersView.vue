<template>
  <v-container class="futuristic-page servers-view-container">
    <v-row justify="space-between" align="center" class="mb-6">
      <v-col>
        <h1 class="text-h3 font-oxanium page-title">Herní Servery</h1>
      </v-col>
      <v-col cols="auto" v-if="canManageServers">
        <v-btn
          color="primary"
          @click="handleOpenCreateServerModal"
          class="futuristic-btn"
          :prepend-icon="mdiPlusBox"
          :loading="isCreatingServer"
        >
          Vytvořit server
        </v-btn>
      </v-col>
    </v-row>

    <v-alert
      v-if="signalRError"
      type="warning"
      density="compact"
      class="mb-4 font-inter"
      closable
      @click:close="signalRError = null"
    >
      Chyba real-time spojení pro servery: {{ signalRError }}.
      <v-btn
        variant="text"
        size="small"
        @click="attemptServersReconnect"
        :loading="reconnectingSignalR"
        >Zkusit znovu</v-btn
      >
    </v-alert>

    <v-progress-linear
      v-if="loading && servers.length === 0"
      indeterminate
      color="primary"
      class="mb-4"
    ></v-progress-linear>

    <v-alert
      v-if="apiError"
      type="error"
      prominent
      class="mb-4 font-inter"
      closable
      @click:close="apiError = null"
    >
      <div class="font-weight-bold mb-1">Chyba při komunikaci se serverem:</div>
      <pre style="white-space: pre-wrap">{{ apiError }}</pre>
    </v-alert>

    <div v-if="!loading && servers.length === 0 && !apiError" class="text-center pa-8">
      <v-icon :icon="mdiServerOff" size="64" color="grey-darken-1"></v-icon>
      <p class="text-h6 font-inter mt-4 text-grey-darken-1">
        Zatím nebyly vytvořeny žádné herní servery.
      </p>
    </div>

    <v-row justify="center" v-if="servers.length > 0">
      <v-col v-for="server in servers" :key="server.gameServerId" cols="12" md="10" lg="10">
        <ServerCard
          :server="server"
          :can-manage="canManageServers"
          :action-loading-states="actionLoadingStates[server.gameServerId] || {}"
          @toggle-state="handleToggleServerState"
          @delete-server="handleConfirmDeleteServer"
          @navigate-to-detail="handleNavigateToDetail"
        />
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed, reactive } from 'vue'
import { useRouter } from 'vue-router'
import Swal, { type SweetAlertOptions } from 'sweetalert2'
import { useAuthStore } from '@/stores/authStore'
import { UserRoles, ServerStatus } from '@/types/enums'
import { mdiPlusBox, mdiServerOff, mdiAlertOctagon, mdiCheckCircle, mdiRefresh } from '@mdi/js'

import ServerCard, { type GameServerDto } from '@/components/ServerCard.vue'
import { useGameDisplayHelpers } from '@/composables/useGameDisplayHelpers'
import { useCreateServerModal, type CreateServerFormData } from '@/composables/useCreateServerModal'

import { signalRService } from '@/services/signalrService'
// --- Constants ---
const GAME_SERVER_HUB_PATH = '/gameServerHub'
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL + '/api/gameservers'

// --- Reactive State ---
const authStore = useAuthStore()
const router = useRouter()
const servers = ref<GameServerDto[]>([])
const loading = ref(true)
const apiError = ref<string | null>(null)
const signalRError = ref<string | null>(null)
const reconnectingSignalR = ref(false)
const isCreatingServer = ref(false)

// Tracking loading states for individual server actions
const actionLoadingStates = reactive<
  Record<string, { toggle?: boolean; logs?: boolean; delete?: boolean }>
>({})

// --- Composables ---
const {} = useGameDisplayHelpers()

// Function to call the API to create a new game server
const callCreateServerApi = async (data: CreateServerFormData): Promise<GameServerDto | null> => {
  isCreatingServer.value = true
  apiError.value = null
  try {
    const response = await fetch(API_BASE_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${authStore.token}` },
      body: JSON.stringify(data),
    })
    if (!response.ok) {
      const errData = await response
        .json()
        .catch(() => ({ message: 'Unknown error when creating server.' }))
      throw new Error(errData.message || `Error ${response.status} when creating server.`)
    }
    const newServer = (await response.json()) as GameServerDto
    return newServer
  } catch (err: any) {
    apiError.value = err.message
    throw err
  } finally {
    isCreatingServer.value = false
  }
}
const { openCreateServerModal } = useCreateServerModal(callCreateServerApi)

// --- Computed Properties ---
// Check if the current user has permissions to manage servers
const canManageServers = computed<boolean>(
  () =>
    !!authStore.isLoggedIn &&
    !!(
      authStore.user?.roles.includes(UserRoles.Administrator) ||
      authStore.user?.roles.includes(UserRoles.Spravce)
    ),
)

// --- Methods ---
// Fetch all game servers from the API
const fetchServersFromApi = async () => {
  loading.value = true
  apiError.value = null
  try {
    const response = await fetch(API_BASE_URL, {
      headers: { Authorization: `Bearer ${authStore.token}` },
    })
    if (!response.ok) {
      const errorData = await response
        .json()
        .catch(() => ({ message: `Server error: ${response.statusText}` }))
      throw new Error(errorData.message || 'Failed to load servers.')
    }
    const data = await response.json()
    // Sort servers by creation date, newest first
    servers.value = data.sort(
      (a: GameServerDto, b: GameServerDto) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    )
  } catch (err: any) {
    apiError.value = err.message
    servers.value = []
  } finally {
    loading.value = false
  }
}

// Helper function to create SVG icons for SweetAlert
const createIconHtml = (
  pathData: string,
  size: number = 20,
  color: string = '#FFFFFF',
  extraStyle: string = '',
): string => {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-hidden="true" width="${size}" height="${size}" fill="${color}" style="vertical-align: middle; margin-right: 10px; ${extraStyle}"><path d="${pathData}"></path></svg>`
}

// Base configuration for SweetAlert popups with futuristic styling
const getFuturisticSwalBaseOptionsForView = (
  title: string,
  iconHtml?: string,
): SweetAlertOptions => ({
  titleText: title,
  iconHtml: iconHtml,
  background: 'rgba(10, 20, 40, 0.9)',
  color: '#E0E0E0',
  confirmButtonColor: '#00E0FF',
  cancelButtonColor: '#FF5252',
  customClass: {
    popup: 'futuristic-swal-popup swal2-backdrop-show animated-border',
    title: 'futuristic-swal-title font-oxanium',
    htmlContainer: 'futuristic-swal-html-container font-inter',
    confirmButton: 'futuristic-swal-confirm-button futuristic-btn futuristic-glow-cyan',
    cancelButton: 'futuristic-swal-cancel-button futuristic-btn futuristic-glow-red',
  },
  buttonsStyling: false,
  heightAuto: false,
})

// --- Event Handlers for ServerCard component ---
// Handle starting or stopping a server
const handleToggleServerState = async (server: GameServerDto) => {
  if (!server.containerId) {
    Swal.fire({
      ...getFuturisticSwalBaseOptionsForView('Error', createIconHtml(mdiAlertOctagon)),
      html: '<div class="font-inter">Server does not have a container ID assigned.</div>',
      icon: 'error',
    })
    return
  }
  const actionKey = server.gameServerId
  actionLoadingStates[actionKey] = { ...actionLoadingStates[actionKey], toggle: true }

  const action = server.status === ServerStatus.Online ? 'stop' : 'start'
  apiError.value = null
  try {
    const response = await fetch(`${API_BASE_URL}/${server.gameServerId}/${action}`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${authStore.token}` },
    })
    if (!response.ok) {
      const errData = await response.json().catch(() => ({ message: 'Unknown error.' }))
      throw new Error(errData.message || `Error when ${action} server.`)
    }
    // Status updates via SignalR, this is just confirmation of command sent
    Swal.fire({
      ...getFuturisticSwalBaseOptionsForView(
        'Command sent',
        createIconHtml(mdiRefresh, 24, '#00E0FF'),
      ),
      html: `<div class="font-inter">Request to ${action} server <strong>${server.name}</strong> has been sent.<br>Status will update soon.</div>`,
      icon: 'info',
      timer: 2500,
      showConfirmButton: false,
    })
  } catch (err: any) {
    apiError.value = err.message // Displayed in global alert
    Swal.fire({
      ...getFuturisticSwalBaseOptionsForView('Error!', createIconHtml(mdiAlertOctagon)),
      html: `<div class="font-inter">${err.message}</div>`,
      icon: 'error',
    })
  } finally {
    actionLoadingStates[actionKey] = { ...actionLoadingStates[actionKey], toggle: false }
  }
}

// Handle server deletion with confirmation
const handleConfirmDeleteServer = (server: GameServerDto) => {
  const actionKey = server.gameServerId
  Swal.fire({
    ...getFuturisticSwalBaseOptionsForView(
      `Delete server ${server.name}?`,
      createIconHtml(mdiAlertOctagon, 24, '#FF5252'),
    ),
    html: `<div class="font-inter">Are you sure you want to delete server <strong>${server.name}</strong>?<br>This action will also delete its Docker container and data!</div>`,
    iconHtml: createIconHtml(mdiAlertOctagon, 48, '#FF5252'),
    showCancelButton: true,
    confirmButtonText: 'Yes, delete it',
    cancelButtonText: 'Cancel',
    showLoaderOnConfirm: true,
    preConfirm: async () => {
      actionLoadingStates[actionKey] = { ...actionLoadingStates[actionKey], delete: true }
      apiError.value = null
      try {
        const response = await fetch(`${API_BASE_URL}/${server.gameServerId}`, {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${authStore.token}` },
        })
        if (!response.ok) {
          const errData = await response
            .json()
            .catch(() => ({ message: 'Unknown error when deleting.' }))
          throw new Error(errData.message || `Error ${response.status} when deleting server.`)
        }
        return true // Success
      } catch (err: any) {
        apiError.value = err.message
        Swal.showValidationMessage(err.message)
        return false
      } finally {
        actionLoadingStates[actionKey] = { ...actionLoadingStates[actionKey], delete: false }
      }
    },
  }).then((result) => {
    if (result.isConfirmed && result.value) {
      // No need to manually remove from `servers.value`, SignalR will handle it
      Swal.fire({
        ...getFuturisticSwalBaseOptionsForView(
          'Deleted!',
          createIconHtml(mdiCheckCircle, 24, '#00E0FF'),
        ),
        html: `<div class="font-inter">Server <strong>${server.name}</strong> has been successfully deleted.</div>`,
        icon: 'success',
        timer: 2500,
        showConfirmButton: false,
      })
    }
  })
}

// Navigate to server detail page
const handleNavigateToDetail = (server: GameServerDto) => {
  if (
    server.status !== ServerStatus.PendingCreation &&
    server.status !== ServerStatus.Stopping &&
    server.containerId
  ) {
    router.push({ name: 'server-detail', params: { id: server.gameServerId } })
  } else if (!server.containerId) {
    Swal.fire({
      ...getFuturisticSwalBaseOptionsForView('Information'),
      html: '<div class="font-inter">Server details are not available until its container is created.</div>',
      icon: 'info',
    })
  }
}

// Open the create server modal dialog
const handleOpenCreateServerModal = () => {
  openCreateServerModal() // Call function from composable
}

// --- SignalR Handlers ---
// Handle updates to game server information
const handleReceiveGameServerUpdate = (updatedServer: GameServerDto) => {
  const index = servers.value.findIndex((s) => s.gameServerId === updatedServer.gameServerId)
  if (index !== -1) {
    servers.value[index] = { ...servers.value[index], ...updatedServer }
  } else {
    servers.value.unshift(updatedServer)
  }
  servers.value.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
}

// Handle server status updates from SignalR
const handleReceiveGameServerStatusUpdate = (statusUpdate: {
  gameServerId: string
  newOverallStatus: ServerStatus
  statusDetails?: string
  errorMessage?: string
}) => {
  const server = servers.value.find((s) => s.gameServerId === statusUpdate.gameServerId)
  if (server) {
    server.status = statusUpdate.newOverallStatus
    server.statusDetails = statusUpdate.statusDetails || server.statusDetails
    if (statusUpdate.errorMessage && !server.statusDetails?.includes(statusUpdate.errorMessage)) {
      server.statusDetails = `${server.statusDetails ? server.statusDetails + '; ' : ''}Error: ${statusUpdate.errorMessage}`
      server.status = ServerStatus.Error
    }
  }
}

// Handle server removal notifications
const handleReceiveGameServerRemoval = (removedServerId: string) => {
  servers.value = servers.value.filter((s) => s.gameServerId !== removedServerId)
}

// Setup SignalR event listeners
const setupSignalRListeners = () => {
  signalRService.on(GAME_SERVER_HUB_PATH, 'ReceiveGameServerUpdate', handleReceiveGameServerUpdate)
  signalRService.on(
    GAME_SERVER_HUB_PATH,
    'ReceiveGameServerStatusUpdate',
    handleReceiveGameServerStatusUpdate,
  )
  signalRService.on(
    GAME_SERVER_HUB_PATH,
    'ReceiveGameServerRemoval',
    handleReceiveGameServerRemoval,
  )
}

// Remove SignalR event listeners
const removeSignalRListeners = () => {
  signalRService.off(GAME_SERVER_HUB_PATH, 'ReceiveGameServerUpdate', handleReceiveGameServerUpdate)
  signalRService.off(
    GAME_SERVER_HUB_PATH,
    'ReceiveGameServerStatusUpdate',
    handleReceiveGameServerStatusUpdate,
  )
  signalRService.off(
    GAME_SERVER_HUB_PATH,
    'ReceiveGameServerRemoval',
    handleReceiveGameServerRemoval,
  )
}

// Attempt to reconnect to SignalR if disconnected
const attemptServersReconnect = async () => {
  if (
    signalRService.getConnectionState(GAME_SERVER_HUB_PATH) === 'Disconnected' ||
    signalRService.getConnectionState(GAME_SERVER_HUB_PATH) === null
  ) {
    reconnectingSignalR.value = true
    signalRError.value = null
    try {
      await signalRService.startConnection(GAME_SERVER_HUB_PATH)
    } catch (err: any) {
      signalRError.value = err.message || 'Failed to reconnect to real-time service for servers.'
    } finally {
      reconnectingSignalR.value = false
    }
  }
}

// --- Lifecycle Hooks ---
onMounted(async () => {
  await fetchServersFromApi()
  if (authStore.isLoggedIn) {
    try {
      await signalRService.startConnection(GAME_SERVER_HUB_PATH)
      setupSignalRListeners()
    } catch (err: any) {
      signalRError.value = `Real-time connection error for servers: ${err.message || 'Unknown error'}`
    }
  }
})
onBeforeUnmount(() => {
  removeSignalRListeners()
})
</script>

<style scoped>
.servers-view-container {
  max-width: 1000px;
}
.page-title {
  color: var(--v-theme-primary);
}

:deep(.futuristic-swal-popup) {
  border-radius: 15px !important;
  border: 1px solid var(--v-theme-primary-lighten-1) !important;
  box-shadow:
    0 0 25px rgba(var(--v-theme-primary-rgb), 0.3),
    0 0 10px rgba(var(--v-theme-secondary-rgb), 0.2) inset !important;
  overflow: hidden !important;
}
:deep(.futuristic-swal-popup.animated-border::before) {
  content: '';
  position: absolute;
  top: -2px;
  left: -2px;
  right: -2px;
  bottom: -2px;
  background: linear-gradient(
    45deg,
    var(--v-theme-primary),
    var(--v-theme-secondary),
    var(--v-theme-primary)
  );
  background-size: 400% 400%;
  z-index: -1;
  filter: blur(5px);
  animation: glowingBorder 10s linear infinite;
  border-radius: 17px;
  opacity: 0.7;
}
@keyframes glowingBorder {
  0% {
    background-position: 0% 50%;
  }
  50% {
    background-position: 100% 50%;
  }
  100% {
    background-position: 0% 50%;
  }
}

:deep(.futuristic-swal-title) {
  color: var(--v-theme-primary) !important;
  font-size: 1.8em !important;
  text-shadow: 0 0 5px rgba(var(--v-theme-primary-rgb), 0.7);
  padding-bottom: 15px !important;
  border-bottom: 1px solid rgba(var(--v-theme-primary-rgb), 0.2);
  margin-bottom: 20px !important;
  display: flex;
  align-items: center;
  justify-content: center;
}
:deep(.futuristic-swal-title .swal2-icon-content) {
  margin-right: 15px !important;
}
:deep(.futuristic-swal-html-container) {
  color: #b0c4de !important;
  line-height: 1.6 !important;
}
:deep(.swal-form-container-custom-padding) {
  padding: 0 1em 1em 1em !important;
}

:deep(.futuristic-form .futuristic-input-group) {
  margin-bottom: 20px;
  text-align: left;
}
:deep(.futuristic-form .futuristic-label) {
  display: flex;
  align-items: center;
  color: var(--v-theme-secondary);
  margin-bottom: 8px;
  font-size: 0.95rem;
  font-weight: 500;
}
:deep(.futuristic-input),
:deep(.futuristic-select) {
  background-color: rgba(var(--v-theme-surface-rgb), 0.1) !important;
  border: 1px solid rgba(var(--v-theme-primary-rgb), 0.3) !important;
  color: #e0e0e0 !important;
  border-radius: 8px !important;
  padding: 12px 15px !important;
  width: 100% !important;
  box-sizing: border-box !important;
  transition:
    border-color 0.3s ease,
    box-shadow 0.3s ease !important;
  font-family: 'Inter', sans-serif;
}
:deep(.futuristic-input::placeholder) {
  color: rgba(var(--v-theme-text-secondary-rgb), 0.7) !important;
}
:deep(.futuristic-input:focus),
:deep(.futuristic-select:focus) {
  border-color: var(--v-theme-primary) !important;
  box-shadow: 0 0 10px rgba(var(--v-theme-primary-rgb), 0.5) !important;
  outline: none !important;
}
:deep(.futuristic-select-option) {
  background-color: #0a1428;
  color: #e0e0e0;
}
:deep(.futuristic-select-option:hover) {
  background-color: var(--v-theme-primary-darken-1);
}

:deep(.futuristic-swal-confirm-button),
:deep(.futuristic-swal-cancel-button) {
  padding: 10px 25px !important;
  font-size: 1rem !important;
  border-radius: 8px !important;
  transition: all 0.3s ease !important;
  text-transform: uppercase !important;
  letter-spacing: 0.5px;
  margin: 5px !important;
}
:deep(.futuristic-glow-cyan:hover) {
  box-shadow:
    0 0 15px 3px var(--v-theme-primary-rgb),
    0 0 5px 1px var(--v-theme-primary-rgb) inset !important;
  transform: translateY(-2px);
}
:deep(.futuristic-glow-red:hover) {
  box-shadow:
    0 0 15px 3px var(--v-theme-error-rgb),
    0 0 5px 1px var(--v-theme-error-rgb) inset !important;
  transform: translateY(-2px);
}
:deep(.swal2-validation-message) {
  background-color: rgba(var(--v-theme-error-rgb), 0.1) !important;
  color: var(--v-theme-error) !important;
  border: 1px solid var(--v-theme-error) !important;
  border-radius: 6px;
  padding: 8px 12px !important;
  margin-top: 10px !important;
}
:deep(.large-swal) {
  width: 650px !important;
  max-width: 90vw;
}
:deep(.logs-swal .swal2-html-container) {
  max-width: 100%;
}
:deep(.server-logs-pre) {
  text-align: left;
  max-height: 70vh;
  overflow-y: auto;
  background-color: #010409;
  color: #c9d1d9;
  padding: 15px;
  border-radius: 6px;
  font-size: 0.8rem;
  white-space: pre-wrap;
  word-break: break-all;
  border: 1px solid rgba(var(--v-theme-primary-rgb), 0.3);
  font-family: 'Roboto Mono', monospace;
}
</style>
