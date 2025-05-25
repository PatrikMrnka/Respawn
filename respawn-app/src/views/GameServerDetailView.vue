<template>
  <v-container class="futuristic-page game-server-detail-view">
    <v-btn :to="{ name: 'servers' }" color="primary" variant="outlined" class="mb-6 futuristic-btn" :prepend-icon="mdiArrowLeft">
      Zpět na seznam serverů
    </v-btn>

    <v-card v-if="loading" class="pa-md-8 pa-4 futuristic-card text-center">
      <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
      <p class="mt-4 font-inter text-h6">Načítání detailů serveru...</p>
    </v-card>

    <v-alert v-if="apiError && !loading" type="error" prominent class="mb-6 font-inter futuristic-alert">
      <div class="font-weight-bold mb-1">Chyba při načítání informací o serveru:</div>
      <p>{{ apiError }}</p>
    </v-alert>

    <v-card v-if="serverDetails && !loading && !apiError" class="pa-md-6 pa-4 futuristic-card server-detail-card">
      <v-row align="center">
        <v-col cols="12" md="auto">
           <v-icon :icon="getGameIcon(serverDetails?.gameType)" size="64" :color="getOverallStatusColor(serverDetails.status)" class="mr-4 server-icon"></v-icon>
        </v-col>
        <v-col>
          <v-card-title class="text-h3 font-oxanium page-title mb-0 pb-0">
            {{ serverDetails.name }}
          </v-card-title>
          <v-card-subtitle class="font-inter text-subtitle-1 mt-1">
            <span :class="`status-text-${serverDetails.status.toString().toLowerCase()}`">
              {{ getServerStatusText(serverDetails.status) }}
            </span>
            <span v-if="serverDetails.ipAddress && serverDetails.port"> | {{ serverDetails.ipAddress }}:{{ serverDetails.port }} </span>
            <span v-else-if="serverDetails.ipAddress"> | {{ serverDetails.ipAddress }}</span>
          </v-card-subtitle>
        </v-col>
      </v-row>

      <v-divider class="my-6 futuristic-divider"></v-divider>

      <v-alert v-if="a2sDisplayMessage" type="warning" prominent class="mb-6 font-inter futuristic-alert" :icon="mdiAlertCircleOutline">
        <div class="font-weight-bold mb-1">Informace o serveru:</div>
        <p>{{ a2sDisplayMessage }}</p>
        <p v-if="serverDetails.status === ServerStatus.Online && serverDetails.statusDetails && serverDetails.statusDetails.toLowerCase().includes('a2s')" class="text-caption mt-2">
            Detail z backendu: {{ serverDetails.statusDetails }}
        </p>
      </v-alert>

      <v-row v-else>
        <v-col cols="12" md="6">
          <h3 class="text-h5 font-exo2 mb-3 section-title">Informace o serveru (Live)</h3>
          <v-list class="futuristic-list" density="compact">
            <v-list-item :prepend-icon="mdiTag" class="info-item">
              <v-list-item-title class="font-weight-bold">Název serveru (A2S):</v-list-item-title>
              <v-list-item-subtitle>{{ serverDetails.gameName || serverDetails.name }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item :prepend-icon="mdiMapMarker" class="info-item">
              <v-list-item-title class="font-weight-bold">Mapa:</v-list-item-title>
              <v-list-item-subtitle>{{ serverDetails.mapName || 'N/A' }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item :prepend-icon="mdiAccountGroup" class="info-item">
              <v-list-item-title class="font-weight-bold">Hráči:</v-list-item-title>
              <v-list-item-subtitle>{{ serverDetails.currentPlayers ?? 'N/A' }} / {{ serverDetails.maxPlayers ?? 'N/A' }}</v-list-item-subtitle>
            </v-list-item>
             <v-list-item :prepend-icon="mdiShieldCheck" class="info-item">
              <v-list-item-title class="font-weight-bold">VAC Zabezpečení:</v-list-item-title>
              <v-list-item-subtitle>{{ serverDetails.isVacSecured ? 'Ano' : 'Ne' }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item :prepend-icon="mdiInformationOutline" class="info-item">
              <v-list-item-title class="font-weight-bold">Typ hry (DB):</v-list-item-title>
              <v-list-item-subtitle>{{ getGameTypeText(serverDetails.gameType) }}</v-list-item-subtitle>
            </v-list-item>
             <v-list-item :prepend-icon="mdiServerNetwork" class="info-item" v-if="serverDetails.containerId">
              <v-list-item-title class="font-weight-bold">ID Kontejneru:</v-list-item-title>
              <v-list-item-subtitle class="font-roboto-mono" style="font-size: 0.8rem;">{{ serverDetails.containerId.substring(0,12) }}...</v-list-item-subtitle>
            </v-list-item>
            <v-list-item :prepend-icon="mdiClockTimeFourOutline" class="info-item">
              <v-list-item-title class="font-weight-bold">Vytvořeno (DB):</v-list-item-title>
              <v-list-item-subtitle>{{ formatFullDateTime(serverDetails.createdAt) }}</v-list-item-subtitle>
            </v-list-item>
             <v-list-item :prepend-icon="mdiTextBoxOutline" class="info-item" v-if="serverDetails.statusDetails && !a2sDisplayMessage">
              <v-list-item-title class="font-weight-bold">Detail stavu (DB):</v-list-item-title>
              <v-list-item-subtitle style="white-space: pre-wrap;">{{ serverDetails.statusDetails }}</v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-col>

        <v-col cols="12" md="6">
          <h3 class="text-h5 font-exo2 mb-3 section-title">Seznam hráčů</h3>
          <div v-if="!serverDetails.players || serverDetails.players.length === 0" class="text-center pa-4 text-grey-darken-1 font-inter">
            <v-icon :icon="mdiAccountOff" size="48" class="mb-2"></v-icon>
            <p>Na serveru nejsou žádní hráči nebo se nepodařilo načíst seznam.</p>
          </div>
          <v-table v-else class="futuristic-table player-table" density="compact" fixed-header height="300px">
            <thead>
              <tr>
                <th class="text-left font-exo2">Jméno</th>
                <th class="text-right font-exo2">Skóre</th>
                <th class="text-right font-exo2">Čas na serveru</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="player in serverDetails.players" :key="player.name">
                <td class="font-inter">{{ player.name }}</td>
                <td class="text-right font-roboto-mono">{{ player.score }}</td>
                <td class="text-right font-roboto-mono">{{ formatDuration(player.duration) }}</td>
              </tr>
            </tbody>
          </v-table>
        </v-col>
      </v-row>

      <v-row v-if="canManageServer && serverDetails.containerId" class="mt-6">
        <v-col cols="12">
          <div class="d-flex justify-space-between align-center mb-3">
            <h3 class="text-h5 font-exo2 section-title mb-0">Logy Kontejneru</h3>
            <v-btn 
              @click="toggleLiveLogs" 
              :color="isLiveLogging ? 'error' : 'secondary'"
              class="futuristic-btn-secondary"
              :loading="logLoadingState"
              :prepend-icon="isLiveLogging ? mdiStopCircleOutline : mdiPlayCircleOutline"
            >
              {{ isLiveLogging ? 'Zastavit živé logy' : 'Sledovat živé logy' }}
            </v-btn>
          </div>
          <div ref="logContainer" class="realtime-logs-container pa-3">
            <div v-if="realtimeLogs.length === 0 && !isLiveLogging" class="text-center text-grey-darken-1 font-inter py-5">
              Pro zobrazení živých logů klikněte na tlačítko "Sledovat živé logy".
            </div>
             <div v-if="realtimeLogs.length === 0 && isLiveLogging && !logLoadingState" class="text-center text-grey-darken-1 font-inter py-5">
              Čekání na logy...
            </div>
            <div v-for="(line, index) in realtimeLogs" :key="index" class="log-line" v-html="formatLogLine(line)"></div>
          </div>
        </v-col>
      </v-row>


      <v-card-actions class="mt-6" v-if="canManageServer">
          <v-spacer></v-spacer>
          <v-btn
            small
            :color="serverDetails.status === ServerStatus.Online ? 'warning' : 'success'"
            @click="toggleServerState(serverDetails)"
            :loading="actionLoading"
            :disabled="isActionDisabled(serverDetails.status)"
            class="futuristic-btn-secondary mx-1"
          >
            <v-icon left>{{ serverDetails.status === ServerStatus.Online ? mdiStopCircleOutline : mdiPlayCircleOutline }}</v-icon>
            {{ serverDetails.status === ServerStatus.Online ? 'Stop' : 'Start' }}
          </v-btn>
          <v-btn :icon="mdiDelete" color="error" variant="text" @click="confirmDeleteServer(serverDetails)" :loading="deleteLoading" title="Smazat server" class="mx-1" :disabled="isActionDisabled(serverDetails.status) && serverDetails.status !== ServerStatus.Error && serverDetails.status !== ServerStatus.Offline"></v-btn>
      </v-card-actions>

    </v-card>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, onBeforeUnmount, watch, nextTick } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { getGameServerDetails, type GameServerDetailDtoFE, type PlayerDetailDtoFE } from '@/services/gameServerService';
import { useAuthStore } from '@/stores/authStore';
import Swal, { type SweetAlertOptions } from 'sweetalert2';
import { signalRService } from '@/services/signalrService';
import { serverLogSignalr } from '@/services/serverLogSignalrService';
import Convert from 'ansi-to-html';

import {
  mdiArrowLeft, mdiTag, mdiMapMarker, mdiAccountGroup, mdiShieldCheck, mdiInformationOutline, mdiServerNetwork, mdiClockTimeFourOutline, mdiTextBoxOutline, mdiAccountOff,
  mdiServerOff, mdiAlphaTBoxOutline, mdiPuzzleOutline, mdiServer,
  mdiPlayCircleOutline, mdiStopCircleOutline, mdiDelete, mdiAlertCircleOutline
} from '@mdi/js';
import { GameType, ServerStatus, UserRoles } from '@/types/enums';
import type { GameServerDto, GameServerStatusUpdateDtoFE as BasicStatusUpdateDto } from '@/components/ServerCard.vue';


const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

// Server identification and state
const serverId = ref<string>(route.params.id as string);
const serverDetails = ref<GameServerDetailDtoFE | null>(null);
const loading = ref(true);
const apiError = ref<string | null>(null);
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// UI loading states
const actionLoading = ref(false);
const deleteLoading = ref(false);

// SignalR hub path
const GAME_SERVER_HUB_PATH = "/gameServerHub";

// Real-time logs state management
const realtimeLogs = ref<string[]>([]);
const isLiveLogging = ref(false);
const logLoadingState = ref(false); // For "Watch/Stop" button loading state
const logContainer = ref<HTMLElement | null>(null); // Ref for the log container div
const ansiConverter = new Convert({ newline: true, escapeXML: true, fg: '#FFF', bg: '#1A2033' });


/**
 * Computed property to determine what message to display regarding A2S query status
 * Returns appropriate message based on server status and A2S data availability
 */
const a2sDisplayMessage = computed<string | null>(() => {
  if (!serverDetails.value) return null;
  if (serverDetails.value.status !== ServerStatus.Online) {
    return "Herní server je aktuálně offline.";
  }
  const hasEssentialA2sData = serverDetails.value.mapName || serverDetails.value.gameName;
  if (!hasEssentialA2sData) {
    if (serverDetails.value.statusDetails && 
        (serverDetails.value.statusDetails.toLowerCase().includes("a2s dotaz selhal") ||
         serverDetails.value.statusDetails.toLowerCase().includes("nepodařilo se načíst detailní informace") ||
         serverDetails.value.statusDetails.toLowerCase().includes("server neodpovídá na a2s dotazy")
        )) {
      return serverDetails.value.statusDetails;
    }
    return "Herní server je online, ale nepodařilo se načíst detailní informace (server neodpovídá na A2S dotazy).";
  }
  return null;
});

/**
 * Fetches server details from the API
 * Sets loading state and handles errors
 */
const fetchDetails = async () => {
  loading.value = true;
  apiError.value = null;
  try {
    const details = await getGameServerDetails(serverId.value);
    if (details) {
      serverDetails.value = details;
    } else {
      apiError.value = 'Nepodařilo se načíst informace o serveru nebo server neexistuje.';
      serverDetails.value = null;
    }
  } catch (err: any) {
    apiError.value = err.message || 'Došlo k neočekávané chybě.';
    serverDetails.value = null;
  } finally {
    loading.value = false;
  }
};

/**
 * Maps game type enum to appropriate icon
 */
const getGameIcon = (gameType: GameType) => ({
  [GameType.CounterStrike]: mdiServer,
  [GameType.TeamFortress2]: mdiAlphaTBoxOutline,
  [GameType.GarrysMod]: mdiPuzzleOutline,
}[gameType] || mdiServerOff);

/**
 * Maps game type enum to human-readable text
 */
const getGameTypeText = (gameType: GameType) => ({
  [GameType.CounterStrike]: "Counter-Strike 1.6",
  [GameType.TeamFortress2]: "Team Fortress 2",
  [GameType.GarrysMod]: "Garry's Mod",
}[gameType] || "Neznámá hra");

/**
 * Maps server status to appropriate color for UI
 */
const getOverallStatusColor = (status: ServerStatus) => ({
  [ServerStatus.Online]: 'success', [ServerStatus.Offline]: 'error',
  [ServerStatus.Starting]: 'info', [ServerStatus.Stopping]: 'warning',
  [ServerStatus.Error]: 'deep-orange-accent-4',
  [ServerStatus.PendingCreation]: 'grey-lighten-1', [ServerStatus.Unknown]: 'grey-darken-1',
  [ServerStatus.Restarting]: 'cyan',
}[status] || 'grey');

/**
 * Maps server status enum to human-readable text
 */
const getServerStatusText = (status: ServerStatus) => ({
  [ServerStatus.Online]: "Online", [ServerStatus.Offline]: "Offline",
  [ServerStatus.Starting]: "Spouští se", [ServerStatus.Stopping]: "Zastavuje se",
  [ServerStatus.Error]: "Chyba",
  [ServerStatus.PendingCreation]: "Čeká na vytvoření", [ServerStatus.Unknown]: "Neznámý",
  [ServerStatus.Restarting]: "Restartuje se",
}[status] || "Neznámý stav");

/**
 * Formats date string to localized format
 */
const formatFullDateTime = (dateString?: string) => {
  if (!dateString) return 'N/A';
  return new Date(dateString).toLocaleString('cs-CZ', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
};

/**
 * Converts seconds to human-readable time format (Xh Xm Xs)
 */
const formatDuration = (seconds: number): string => {
  if (isNaN(seconds) || seconds < 0) return 'N/A';
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  return `${h > 0 ? h + 'h ' : ''}${m > 0 ? m + 'm ' : ''}${s}s`;
};

/**
 * SignalR handler for full server update events
 */
const handleReceiveGameServerUpdate = (updatedServer: GameServerDetailDtoFE | GameServerDto) => {
  if (updatedServer.gameServerId === serverId.value) {
    serverDetails.value = { 
        ...(serverDetails.value || {} as GameServerDetailDtoFE), 
        ...updatedServer, 
        players: (updatedServer as GameServerDetailDtoFE).players || serverDetails.value?.players || [] 
    };
    console.log("Detail serveru {ServerId} aktualizován přes SignalR (plný update).", serverId.value);
  }
};

/**
 * SignalR handler for server status update events
 */
const handleReceiveGameServerStatusUpdate = (statusUpdate: BasicStatusUpdateDto) => {
  if (statusUpdate.gameServerId === serverId.value && serverDetails.value) {
    serverDetails.value.status = statusUpdate.newOverallStatus;
    serverDetails.value.statusDetails = statusUpdate.statusDetails || serverDetails.value.statusDetails;
    if(statusUpdate.errorMessage && !serverDetails.value.statusDetails?.includes(statusUpdate.errorMessage)) {
        serverDetails.value.statusDetails = `${serverDetails.value.statusDetails ? serverDetails.value.statusDetails + '; ' : ''}Chyba: ${statusUpdate.errorMessage}`;
        serverDetails.value.status = ServerStatus.Error;
    }
    console.log("Stav serveru {ServerId} aktualizován přes SignalR. Nový stav: {Status}, Detail: {Details}", serverId.value, serverDetails.value.status, serverDetails.value.statusDetails);
    if (statusUpdate.newOverallStatus === ServerStatus.Online && a2sDisplayMessage.value) {
        console.log("Server {ServerId} is now Online, attempting to refresh details for A2S data.", serverId.value);
        fetchDetails();
    }
  }
};

/**
 * Handler for new log lines received from SignalR
 * Adds line to log buffer and maintains max size
 */
const handleNewLogLine = (line: string) => {
  realtimeLogs.value.push(line);
  if (realtimeLogs.value.length > 200) { // Keep only the last 200 lines
    realtimeLogs.value.shift();
  }
  // Scroll to bottom
  nextTick(() => {
    if (logContainer.value) {
      logContainer.value.scrollTop = logContainer.value.scrollHeight;
    }
  });
};

/**
 * Converts ANSI escape codes in log lines to HTML for colored output
 */
const formatLogLine = (line: string) => {
    return ansiConverter.toHtml(line);
};

/**
 * Toggles real-time log monitoring
 * Connects to SignalR log hub when starting, disconnects when stopping
 */
const toggleLiveLogs = async () => {
  if (!serverDetails.value?.gameServerId) return;
  logLoadingState.value = true;

  if (isLiveLogging.value) {
    await serverLogSignalr.unwatchLogs(serverDetails.value.gameServerId);
    isLiveLogging.value = false;
  } else {
    realtimeLogs.value = []; // Clear previous logs
    const connected = await serverLogSignalr.startConnection();
    if (connected) {
      await serverLogSignalr.watchLogs(serverDetails.value.gameServerId, handleNewLogLine);
      isLiveLogging.value = true;
    } else {
      realtimeLogs.value.push("[SYSTEM ERROR] Nepodařilo se připojit k službě logů.");
    }
  }
  logLoadingState.value = false;
};

/**
 * Component lifecycle hook
 * Fetches server details and sets up SignalR connections
 */
onMounted(async () => {
  await fetchDetails();
  if (authStore.isLoggedIn) {
    try {
      await signalRService.startConnection(GAME_SERVER_HUB_PATH); // For server status/details updates
      signalRService.on(GAME_SERVER_HUB_PATH, "ReceiveGameServerUpdate", handleReceiveGameServerUpdate as (updatedServer: GameServerDto) => void);
      signalRService.on(GAME_SERVER_HUB_PATH, "ReceiveGameServerStatusUpdate", handleReceiveGameServerStatusUpdate as (statusUpdate: BasicStatusUpdateDto) => void);
    } catch (err) {
      console.error("SignalR (GameServerHub) connection error on GameServerDetailView:", err);
    }
  }
});

/**
 * Component cleanup hook
 * Removes SignalR event handlers and closes connections
 */
onBeforeUnmount(async () => {
  if (authStore.isLoggedIn) {
    signalRService.off(GAME_SERVER_HUB_PATH, "ReceiveGameServerUpdate", handleReceiveGameServerUpdate as (updatedServer: GameServerDto) => void);
    signalRService.off(GAME_SERVER_HUB_PATH, "ReceiveGameServerStatusUpdate", handleReceiveGameServerStatusUpdate as (statusUpdate: BasicStatusUpdateDto) => void);
  }
  // Stop live logging if active when component is unmounted
  if (isLiveLogging.value && serverDetails.value?.gameServerId) {
    await serverLogSignalr.unwatchLogs(serverDetails.value.gameServerId);
  }
  await serverLogSignalr.stopConnection(); // Ensure log hub connection is closed
});

/**
 * Determines if current user has permissions to manage servers
 */
const canManageServer = computed(() => {
  return authStore.isLoggedIn && (authStore.user?.roles.includes(UserRoles.Administrator) || authStore.user?.roles.includes(UserRoles.Spravce));
});

/**
 * Checks if server status is a transitional state
 */
const isLoadingStatus = (status: ServerStatus): boolean => {
    return [
        ServerStatus.Starting, ServerStatus.Stopping,
        ServerStatus.PendingCreation, ServerStatus.Restarting
    ].includes(status);
};

/**
 * Determines if server action buttons should be disabled
 */
const isActionDisabled = (status: ServerStatus): boolean => {
    return isLoadingStatus(status) || status === ServerStatus.Unknown;
};

/**
 * Creates base SweetAlert2 options with futuristic styling
 */
const getFuturisticSwalBaseOptions = (title: string): SweetAlertOptions => ({
  titleText: title, background: '#1A2033', color: '#E0E0E0',
  confirmButtonColor: '#00E0FF', cancelButtonColor: '#FF5252',
  customClass: {
    popup: 'futuristic-swal-popup', title: 'futuristic-swal-title font-oxanium',
    htmlContainer: 'futuristic-swal-html-container font-inter', input: 'futuristic-swal-input',
    confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
    cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
    actions: 'futuristic-swal-actions', validationMessage: 'futuristic-swal-validation-message font-inter',
  },
  buttonsStyling: false, heightAuto: false, allowEnterKey: true,
});

/**
 * Handles start/stop server operations
 * Sends API request and shows appropriate messages
 */
const toggleServerState = async (server: GameServerDetailDtoFE) => {
    if (!server.containerId) {
        Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: 'Server nemá přiřazené ID kontejneru.', icon: 'error'});
        return;
    }
    actionLoading.value = true;
    const action = server.status === ServerStatus.Online ? 'stop' : 'start';
    
    try {
        const response = await fetch(API_BASE_URL+`/api/gameservers/${server.gameServerId}/${action}`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${authStore.token}` }
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({message: "Neznámá chyba."}));
            throw new Error(errData.message || `Chyba při ${action} serveru.`);
        }
        Swal.fire({...getFuturisticSwalBaseOptions('Příkaz odeslán'), text: `Požadavek na ${action} serveru byl odeslán. Stav se brzy aktualizuje.`, icon: 'info', timer: 2000, showConfirmButton: false});
    } catch (err: any) {
        Swal.fire({...getFuturisticSwalBaseOptions('Chyba!'), text: err.message, icon: 'error'});
    } finally {
        actionLoading.value = false;
    }
};

/**
 * Confirmation dialog for server deletion
 * Handles the delete API call if confirmed
 */
const confirmDeleteServer = (server: GameServerDetailDtoFE) => {
    deleteLoading.value = true;
    Swal.fire({
        ...getFuturisticSwalBaseOptions(`Smazat server ${server.name}?`),
        html: `<div class="font-inter">Opravdu si přejete smazat server <strong>${server.name}</strong>?<br>Tato akce smaže i jeho Docker kontejner a data!</div>`,
        icon: 'warning', showCancelButton: true, confirmButtonText: 'Ano, smazat', cancelButtonText: 'Zrušit',
        showLoaderOnConfirm: true,
        preConfirm: async () => {
            try {
                 const response = await fetch(API_BASE_URL+`/api/gameservers/${server.gameServerId}`, {
                    method: 'DELETE',
                    headers: { 'Authorization': `Bearer ${authStore.token}` }
                });
                if (!response.ok) {
                     const errData = await response.json().catch(() => ({message: "Neznámá chyba při mazání."}));
                    throw new Error(errData.message || `Chyba ${response.status} při mazání serveru.`);
                }
                return true;
            } catch (err: any) {
                Swal.showValidationMessage(err.message);
                return false;
            }
        }
    }).then((result) => {
        if (result.isConfirmed && result.value) {
            Swal.fire({...getFuturisticSwalBaseOptions('Smazáno!'), text: `Server ${server.name} byl úspěšně smazán.`, icon: 'success'});
            router.push({ name: 'servers' });
        }
    }).finally(() => {
        deleteLoading.value = false;
    });
};

</script>

<style scoped>
.game-server-detail-view {
  padding-top: 20px;
  max-width: 1000px;
}
.page-title {
  color: var(--v-theme-primary);
  word-break: break-word;
}
.server-icon {
  filter: drop-shadow(0 0 8px rgba(var(--v-theme-primary-rgb), 0.7));
}
.server-detail-card {
  background-color: var(--v-theme-surface);
  border: 1px solid rgba(var(--v-theme-primary-rgb), 0.25);
  box-shadow: 0 8px 25px rgba(var(--v-theme-primary-rgb), 0.1);
}
.futuristic-divider {
  border-color: rgba(var(--v-theme-primary-rgb), 0.2) !important;
}
.section-title {
  color: var(--v-theme-secondary);
  border-bottom: 2px solid rgba(var(--v-theme-secondary-rgb), 0.3);
  padding-bottom: 8px;
  margin-bottom: 16px !important;
}
.futuristic-list {
  background-color: transparent !important;
}
.info-item {
  padding-left: 0 !important;
  padding-right: 0 !important;
  border-bottom: 1px dashed rgba(var(--v-theme-text-primary-rgb), 0.1);
}
.info-item:last-child {
  border-bottom: none;
}
.info-item .v-list-item-title {
  color: var(--v-theme-text-secondary);
}
.info-item .v-list-item-subtitle {
  color: var(--v-theme-text-primary);
  font-size: 1rem;
}
.player-table th {
  background-color: rgba(var(--v-theme-primary-rgb), 0.05) !important;
  color: var(--v-theme-primary) !important;
  font-weight: 500;
}
.player-table tbody tr:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.03) !important;
}

.status-text-online { color: var(--v-theme-success); }
.status-text-offline { color: var(--v-theme-error); }
.status-text-starting, .status-text-restarting { color: var(--v-theme-info); }
.status-text-stopping { color: var(--v-theme-warning); }
.status-text-error { color: var(--v-theme-error); }
.status-text-pendingcreation, .status-text-unknown { color: var(--v-theme-text-secondary); }

.futuristic-alert {
  border-left: 4px solid;
}
.futuristic-alert.v-alert--type-error {
  border-left-color: var(--v-theme-error);
  background-color: rgba(var(--v-theme-error-rgb), 0.1);
}
.futuristic-alert.v-alert--type-warning {
  border-left-color: var(--v-theme-warning);
  background-color: rgba(var(--v-theme-warning-rgb), 0.1);
}

.realtime-logs-container {
  background-color: #010409;
  color: #c9d1d9;
  border: 1px solid rgba(var(--v-theme-primary-rgb), 0.3);
  border-radius: 6px;
  height: 400px;
  overflow-y: auto;
  font-family: 'Roboto Mono', monospace;
  font-size: 0.8rem;
  white-space: pre-wrap;
  word-break: break-all;
}
.log-line {
  padding: 2px 5px;
  border-bottom: 1px solid rgba(255,255,255,0.05);
}
.log-line:last-child {
  border-bottom: none;
}
:deep(.realtime-logs-container span) {
    display: inline !important;
}


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
.futuristic-btn-secondary {
  font-family: 'Exo 2', sans-serif;
  box-shadow: 0 0 8px 0px transparent;
  transition: all 0.2s ease-in-out;
}
.futuristic-btn-secondary:hover {
  box-shadow: 0 0 12px 2px var(--v-theme-glow-color);
  transform: translateY(-1px);
}
</style>
