<template>
  <v-container class="futuristic-page servers-view-container">
    <v-row justify="space-between" align="center" class="mb-6">
      <v-col>
        <h1 class="text-h3 font-oxanium page-title">Herní Servery</h1>
      </v-col>
      <v-col cols="auto" v-if="canManageServers">
        <v-btn color="primary" @click="openCreateServerModal" class="futuristic-btn" :prepend-icon="mdiPlusBox">
          Vytvořit server
        </v-btn>
      </v-col>
    </v-row>

     <v-alert v-if="signalRError" type="warning" density="compact" class="mb-4 font-inter" closable @click:close="signalRError = null">
      Chyba real-time spojení pro servery: {{ signalRError }}.
      <v-btn variant="text" size="small" @click="attemptServersReconnect" :loading="reconnectingSignalR">Zkusit znovu</v-btn>
    </v-alert>

    <v-progress-linear v-if="loading && servers.length === 0" indeterminate color="primary" class="mb-4"></v-progress-linear>
    <v-alert v-if="error" type="error" prominent class="mb-4 font-inter" closable @click:close="error = null">
      <div class="font-weight-bold mb-1">Chyba při načítání serverů:</div>
      <pre style="white-space: pre-wrap;">{{ error }}</pre>
    </v-alert>

    <div v-if="!loading && servers.length === 0 && !error" class="text-center pa-8">
      <v-icon :icon="mdiServerOff" size="64" color="grey-darken-1"></v-icon>
      <p class="text-h6 font-inter mt-4 text-grey-darken-1">Zatím nebyly vytvořeny žádné herní servery.</p>
    </div>

    <v-row justify="center" v-if="servers.length > 0">
      <v-col v-for="server in servers" :key="server.gameServerId" cols="12" md="10" lg="10">
        <v-card class="futuristic-card server-card mb-6" :elevation="server.status === ServerStatus.Online ? 8 : 2" :class="`status-border-${server.status.toString().toLowerCase()}`">
          <v-card-title class="d-flex align-center">
            <v-icon :icon="getGameIcon(server.gameType)" class="mr-3" :color="getOverallStatusColor(server.status)"></v-icon>
            <span class="font-exo2 server-name">{{ server.name }}</span>
            <v-spacer></v-spacer>
             <v-chip :color="getOverallStatusColor(server.status)" label small class="font-exo2 status-chip mr-1" :title="`Stav: ${getServerStatusText(server.status)}`">
                {{ getServerStatusText(server.status) }}
            </v-chip>
            </v-card-title>
          <v-card-subtitle class="font-inter">
            Typ: {{ getGameTypeText(server.gameType) }}
            <span v-if="server.ipAddress && server.port"> | {{ server.ipAddress }}:{{ server.port }}</span>
            <span v-else-if="server.ipAddress"> | {{ server.ipAddress }}</span>
             | Vytvořeno: {{ formatFullDateTime(server.createdAt) }}
          </v-card-subtitle>

          <v-card-text>
            <p class="text-caption text-grey-lighten-1 mb-1">Kontejner: {{ server.containerId?.substring(0,12) || 'N/A' }}</p>
            <p v-if="server.statusDetails" class="text-caption font-roboto-mono status-details-text mt-1" :title="server.statusDetails">
              Detail: {{ filters.truncate(server.statusDetails, 100) }}
            </p>
             <v-progress-linear
                v-if="isLoadingStatus(server.status)"
                indeterminate
                :color="getOverallStatusColor(server.status)"
                height="5"
                class="mt-2"
            ></v-progress-linear>
          </v-card-text>

          <v-card-actions v-if="canManageServers" class="server-actions">
            <v-btn
              small
              :color="server.status === ServerStatus.Online ? 'warning' : 'success'"
              @click="toggleServerState(server)"
              :loading="actionLoading[server.gameServerId + '_toggle']"
              :disabled="isActionDisabled(server.status)"
              class="futuristic-btn-secondary"
            >
              <v-icon left>{{ server.status === ServerStatus.Online ? mdiStopCircleOutline : mdiPlayCircleOutline }}</v-icon>
              {{ server.status === ServerStatus.Online ? 'Stop' : 'Start' }}
            </v-btn>
            <v-spacer></v-spacer>
            <v-btn :icon="mdiConsoleLine" color="info" variant="text" @click="viewServerLogs(server)" :loading="actionLoading[server.gameServerId + '_logs']" title="Zobrazit logy" :disabled="!server.containerId || server.status === ServerStatus.PendingCreation"></v-btn>
            <v-btn :icon="mdiDelete" color="error" variant="text" @click="confirmDeleteServer(server)" :loading="actionLoading[server.gameServerId + '_delete']" title="Smazat server" :disabled="isActionDisabled(server.status) && server.status !== ServerStatus.Error && server.status !== ServerStatus.Offline"></v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed, reactive } from 'vue';
import Swal, {type SweetAlertOptions} from 'sweetalert2';
import { useAuthStore } from '@/stores/authStore';
import { UserRoles, GameType, ServerStatus } from '@/types/enums';
import {
  mdiPlusBox, mdiServerOff, mdiPencil, mdiDelete, mdiPlayCircleOutline, mdiStopCircleOutline,mdiAlphaTBoxOutline, mdiPuzzleOutline, mdiConsoleLine,
  mdiServer, mdiTag, mdiCog, mdiRefresh
} from '@mdi/js';
import { signalRService } from '@/services/signalrService';
import { getContainerLogs } from '@/services/dockerAdminService';

interface GameServerDto {
  gameServerId: string;
  name: string;
  gameType: GameType;
  status: ServerStatus;
  ipAddress?: string;
  port?: number;
  containerId?: string;
  createdAt: string;
  statusDetails?: string;
}
interface CreateGameServerDtoFE {
  name: string;
  gameType: GameType;
  additionalGsParams?: string;
}
interface GameServerStatusUpdateDtoFE {
    gameServerId: string;
    newOverallStatus: ServerStatus;
    // newLgsmServerStatus?: string; // Odstraněno
    statusDetails?: string;
    errorMessage?: string;
}

const GAME_SERVER_HUB_PATH = "/gameServerHub";

const API_BASE_URL = 'http://localhost:5207/api/gameservers';
const authStore = useAuthStore();
const servers = ref<GameServerDto[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);
const actionLoading = reactive<Record<string, boolean>>({});
const signalRError = ref<string | null>(null);
const reconnectingSignalR = ref(false);

const filters = {
  truncate(value: string | null | undefined, length: number = 50) {
    if (!value) return '';
    if (value.length <= length) return value;
    return value.substring(0, length) + '...';
  }
};

const canManageServers = computed(() => {
  return authStore.isLoggedIn && (authStore.user?.roles.includes(UserRoles.Administrator) || authStore.user?.roles.includes(UserRoles.Spravce));
});

const getGameIcon = (gameType: GameType) => ({
  [GameType.CounterStrike]: mdiServer,
  [GameType.TeamFortress2]: mdiAlphaTBoxOutline,
  [GameType.GarrysMod]: mdiPuzzleOutline,
}[gameType] || mdiServerOff);

const getGameTypeText = (gameType: GameType) => ({
  [GameType.CounterStrike]: "Counter-Strike 1.6",
  [GameType.TeamFortress2]: "Team Fortress 2",
  [GameType.GarrysMod]: "Garry's Mod",
}[gameType] || "Neznámá hra");

const getOverallStatusColor = (status: ServerStatus) => ({
  [ServerStatus.Online]: 'success', [ServerStatus.Offline]: 'error',
  [ServerStatus.Starting]: 'info', [ServerStatus.Stopping]: 'warning',
  [ServerStatus.Installing]: 'blue-grey', [ServerStatus.Error]: 'deep-orange-accent-4',
  [ServerStatus.PendingCreation]: 'grey-lighten-1', [ServerStatus.Unknown]: 'grey-darken-1',
  [ServerStatus.Updating]: 'teal', [ServerStatus.Restarting]: 'cyan', [ServerStatus.Deleting]: 'pink-darken-1'
}[status] || 'grey');

const getServerStatusText = (status: ServerStatus) => ({
  [ServerStatus.Online]: "Online", [ServerStatus.Offline]: "Offline",
  [ServerStatus.Starting]: "Spouští se", [ServerStatus.Stopping]: "Zastavuje se",
  [ServerStatus.Installing]: "Instaluje se", [ServerStatus.Error]: "Chyba",
  [ServerStatus.PendingCreation]: "Čeká", [ServerStatus.Unknown]: "Neznámý",
  [ServerStatus.Updating]: "Aktualizuje se", [ServerStatus.Restarting]: "Restartuje se", [ServerStatus.Deleting]: "Maže se"
}[status] || "Neznámý stav");

// Funkce getLgsmStatusColor již není potřeba
// const getLgsmStatusColor = (lgsmStatus?: string): string => { /* ... */ };

const isLoadingStatus = (status: ServerStatus): boolean => {
    return [
        ServerStatus.Installing, ServerStatus.Starting, ServerStatus.Stopping,
        ServerStatus.PendingCreation, ServerStatus.Updating, ServerStatus.Restarting, ServerStatus.Deleting
    ].includes(status);
};

const isActionDisabled = (status: ServerStatus): boolean => {
    return isLoadingStatus(status) || status === ServerStatus.Unknown;
};

const fetchServers = async () => {
  loading.value = true; error.value = null;
  console.log("ServersView: fetchServers - Zahájení načítání serverů...");
  try {
    const response = await fetch(API_BASE_URL, { headers: { 'Authorization': `Bearer ${authStore.token}` } });
    console.log("ServersView: fetchServers - Odpověď z API, status:", response.status, "OK:", response.ok);
    const responseText = await response.text();
    console.log("ServersView: fetchServers - Raw response text:", responseText);
    if (!response.ok) {
      let errorJsonMessage = null;
      try { if (responseText) { const errorData = JSON.parse(responseText); errorJsonMessage = errorData?.message || errorData?.title; } }
      catch (e) { console.warn("ServersView: fetchServers - Tělo chybové odpovědi není validní JSON.", e); }
      throw new Error(errorJsonMessage || responseText || `Nepodařilo se načíst servery (HTTP ${response.status})`);
    }
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.indexOf("application/json") !== -1) {
        if (responseText && responseText.trim() !== "") { const data = JSON.parse(responseText); console.log("ServersView: fetchServers - Data úspěšně načtena a parsována:", data); servers.value = data; }
        else { console.warn("ServersView: fetchServers - Odpověď je JSON, ale tělo je prázdné."); servers.value = []; }
    } else {
        if (response.ok && responseText.trim() === '') { servers.value = []; console.log("ServersView: fetchServers - Přijata prázdná odpověď (200 OK), interpretováno jako žádné servery."); }
        else { console.error("ServersView: fetchServers - Odpověď není JSON. Content-Type:", contentType, "Obsah:", responseText); throw new Error(`Odpověď serveru není ve formátu JSON. Content-Type: ${contentType || 'N/A'}`); }
    }
  } catch (err: any) { console.error("ServersView: fetchServers - Výjimka při fetch:", err); error.value = err.message || "Došlo k neočekávané chybě při načítání serverů."; servers.value = [];
  } finally { loading.value = false; console.log("ServersView: fetchServers - Načítání dokončeno."); }
};

const handleReceiveGameServerUpdate = (updatedServer: GameServerDto) => {
  console.log('SignalR ServersView: ReceiveGameServerUpdate', updatedServer);
  const index = servers.value.findIndex(s => s.gameServerId === updatedServer.gameServerId);
  if (index !== -1) {
    // Při aktualizaci se ujistíme, že LgsmServerStatus je odstraněn, pokud již není v DTO
    const { ...restOfUpdatedServer } = updatedServer;
    servers.value[index] = { ...servers.value[index], ...restOfUpdatedServer };
    if (Object.prototype.hasOwnProperty.call(updatedServer, 'lgsmServerStatus')) {
        // Pokud DTO explicitně obsahuje lgsmServerStatus (i když by nemělo být null/undefined)
        // V našem zjednodušeném případě DTO již LgsmServerStatus neobsahuje, takže se efektivně smaže
    }

    console.log("Aktualizován server (plný update)", updatedServer.gameServerId, "Nový stav:", updatedServer.status, "Detail:", updatedServer.statusDetails);
  } else {
    const { ...restOfNewServer } = updatedServer; // Odstraníme LgsmServerStatus i pro nové servery
    servers.value.unshift(restOfNewServer as GameServerDto); // Přetypování, protože jsme odstranili lgsmServerStatus
    console.log("Přidán nový server", updatedServer.gameServerId, "Stav:", updatedServer.status, "Detail:", updatedServer.statusDetails);
  }
};
const handleReceiveGameServerStatusUpdate = (statusUpdate: GameServerStatusUpdateDtoFE) => {
  console.log('SignalR ServersView: ReceiveGameServerStatusUpdate', statusUpdate);
  const server = servers.value.find(s => s.gameServerId === statusUpdate.gameServerId);
  if (server) {
    server.status = statusUpdate.newOverallStatus;
    server.statusDetails = statusUpdate.statusDetails || server.statusDetails;
    if(statusUpdate.errorMessage) {
        server.statusDetails = `Chyba: ${statusUpdate.errorMessage}`;
        server.status = ServerStatus.Error;
    }
    console.log("Aktualizován stav serveru", server.gameServerId, "Nový stav:", server.status, "Detail:", server.statusDetails);
  }
};
const handleReceiveGameServerRemoval = (removedServerId: string) => {
  console.log('SignalR ServersView: ReceiveGameServerRemoval', removedServerId);
  servers.value = servers.value.filter(s => s.gameServerId !== removedServerId);
};

const setupSignalRListeners = () => {
    console.log(`ServersView: Registruji SignalR listenery pro ${GAME_SERVER_HUB_PATH}`);
    signalRService.on(GAME_SERVER_HUB_PATH, "ReceiveGameServerUpdate", handleReceiveGameServerUpdate);
    signalRService.on(GAME_SERVER_HUB_PATH, "ReceiveGameServerStatusUpdate", handleReceiveGameServerStatusUpdate);
    signalRService.on(GAME_SERVER_HUB_PATH, "ReceiveGameServerRemoval", handleReceiveGameServerRemoval);
};
const removeSignalRListeners = () => {
    console.log(`ServersView: Odregistrovávám SignalR listenery pro ${GAME_SERVER_HUB_PATH}`);
    signalRService.off(GAME_SERVER_HUB_PATH, "ReceiveGameServerUpdate", handleReceiveGameServerUpdate);
    signalRService.off(GAME_SERVER_HUB_PATH, "ReceiveGameServerStatusUpdate", handleReceiveGameServerStatusUpdate);
    signalRService.off(GAME_SERVER_HUB_PATH, "ReceiveGameServerRemoval", handleReceiveGameServerRemoval);
};
const attemptServersReconnect = async () => {
    console.log(`ServersView: Pokus o znovupřipojení k ${GAME_SERVER_HUB_PATH}`);
    if (signalRService.getConnectionState(GAME_SERVER_HUB_PATH) === 'Disconnected' || signalRService.getConnectionState(GAME_SERVER_HUB_PATH) === null) {
        reconnectingSignalR.value = true; signalRError.value = null;
        try { await signalRService.startConnection(GAME_SERVER_HUB_PATH); }
        catch (err: any) { signalRError.value = err.message || "Nepodařilo se znovu připojit k real-time službě pro servery."; console.error(`SignalR reconnect error for ${GAME_SERVER_HUB_PATH}:`, err); }
        finally { reconnectingSignalR.value = false; }
    }
};

onMounted(async () => {
  console.log("ServersView: Komponenta připojena (mounted).");
  await fetchServers();
  if (authStore.isLoggedIn) {
    console.log(`ServersView: Uživatel přihlášen, pokus o start SignalR pro ${GAME_SERVER_HUB_PATH}`);
    try {
      await signalRService.startConnection(GAME_SERVER_HUB_PATH);
      setupSignalRListeners();
    } catch (err: any) { 
        signalRError.value = `Chyba real-time spojení pro servery: ${err.message || 'Neznámá chyba'}`;
        console.error(`SignalR connection error on mount for ${GAME_SERVER_HUB_PATH}:`, err);
    }
  } else {
    console.log("ServersView: Uživatel není přihlášen, SignalR se nespouští.");
  }
});
onBeforeUnmount(() => {
  console.log("ServersView: Komponenta odpojena (beforeUnmount). Čistím SignalR.");
  removeSignalRListeners();
  signalRService.stopConnection(GAME_SERVER_HUB_PATH);
});

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

const createIconHtml = (pathData: string, size: number = 18, color: string = 'currentColor', extraStyle: string = ''): string => {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" role="img" aria-hidden="true" width="${size}" height="${size}" fill="${color}" style="${extraStyle}"><path d="${pathData}"></path></svg>`;
};

const openCreateServerModal = () => {
    const gameTypesOptions = Object.values(GameType).filter(value => typeof value === 'number')
        .map(value => ({ text: getGameTypeText(value as GameType), value: value as GameType }));
    let gameTypesHtml = gameTypesOptions.map(opt => `<option value="${opt.value}">${opt.text}</option>`).join('');
    const iconColor = 'var(--v-theme-primary)';
    const iconStyle = 'vertical-align: middle; margin-right: 8px;';

    Swal.fire({
        ...getFuturisticSwalBaseOptions('Vytvořit nový herní server'),
        html: `
        <div class="swal-form-container">
            <label for="swal-server-name" class="swal-label"> ${createIconHtml(mdiServer, 18, iconColor, iconStyle)}Název serveru:</label>
            <input id="swal-server-name" class="swal2-input futuristic-swal-input" placeholder="Můj CS Server">
            <label for="swal-game-type" class="swal-label mt-3"> ${createIconHtml(mdiTag, 18, iconColor, iconStyle)}Typ hry:</label>
            <select id="swal-game-type" class="swal2-input futuristic-swal-input"> ${gameTypesHtml} </select>
            <label for="swal-gs-params" class="swal-label mt-3"> ${createIconHtml(mdiCog, 18, iconColor, iconStyle)}Extra GS_PARAMS (volitelné):</label>
            <input id="swal-gs-params" class="swal2-input futuristic-swal-input" placeholder="-port 27016 +map de_dust2">
        </div>`,
        customClass: { popup: 'futuristic-swal-popup large-swal', htmlContainer: 'futuristic-swal-html-container font-inter swal-form-container-custom-padding' },
        confirmButtonText: 'Vytvořit', showCancelButton: true, cancelButtonText: 'Zrušit',
        focusConfirm: false, showLoaderOnConfirm: true,
        preConfirm: () => {
            const name = (document.getElementById('swal-server-name') as HTMLInputElement).value;
            const gameType = parseInt((document.getElementById('swal-game-type') as HTMLSelectElement).value) as GameType;
            const additionalGsParams = (document.getElementById('swal-gs-params') as HTMLInputElement).value;
            if (!name || name.length < 3) { Swal.showValidationMessage('Název serveru musí mít alespoň 3 znaky.'); return false; }
            if (isNaN(gameType)) { Swal.showValidationMessage('Musíte vybrat typ hry.'); return false; }
            return { name, gameType, additionalGsParams };
        }
    }).then(async (result) => {
        if (result.isConfirmed && result.value) {
            actionLoading['new_server'] = true;
            try {
                const response = await fetch(API_BASE_URL, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${authStore.token}` },
                    body: JSON.stringify(result.value)
                });
                if (!response.ok) {
                    const errData = await response.json().catch(() => ({message: "Neznámá chyba."}));
                    throw new Error(errData.message || `Chyba ${response.status} při vytváření serveru.`);
                }
                Swal.fire({...getFuturisticSwalBaseOptions('Vytváření zahájeno'), text: 'Požadavek na vytvoření serveru byl odeslán. Stav se brzy aktualizuje.', icon: 'info'});
            } catch (err: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba!'), text: err.message, icon: 'error'});
            } finally { actionLoading['new_server'] = false; }
        }
    });
};

const toggleServerState = async (server: GameServerDto) => {
    if (!server.containerId) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: 'Server nemá přiřazené ID kontejneru.', icon: 'error'}); return; }
    const actionKey = server.gameServerId + '_toggle';
    actionLoading[actionKey] = true;
    const action = server.status === ServerStatus.Online ? 'stop' : 'start';
    try {
        const response = await fetch(`${API_BASE_URL}/${server.gameServerId}/${action}`, {
            method: 'POST', headers: { 'Authorization': `Bearer ${authStore.token}` }
        });
        if (!response.ok) {
            const errData = await response.json().catch(() => ({message: "Neznámá chyba."}));
            throw new Error(errData.message || `Chyba při ${action} serveru.`);
        }
        Swal.fire({...getFuturisticSwalBaseOptions('Příkaz odeslán'), text: `Požadavek na ${action} serveru byl odeslán.`, icon: 'info'});
    } catch (err: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba!'), text: err.message, icon: 'error'});
    } finally { actionLoading[actionKey] = false; }
};

const confirmDeleteServer = (server: GameServerDto) => {
    const actionKey = server.gameServerId + '_delete';
    Swal.fire({
        ...getFuturisticSwalBaseOptions(`Smazat server ${server.name}?`),
        html: `<div class="font-inter">Opravdu si přejete smazat server <strong>${server.name}</strong>?<br>Tato akce smaže i jeho Docker kontejner a data!</div>`,
        icon: 'warning', showCancelButton: true, confirmButtonText: 'Ano, smazat', cancelButtonText: 'Zrušit',
        showLoaderOnConfirm: true,
        preConfirm: async () => {
            actionLoading[actionKey] = true;
            try {
                const response = await fetch(`${API_BASE_URL}/${server.gameServerId}`, {
                    method: 'DELETE', headers: { 'Authorization': `Bearer ${authStore.token}` }
                });
                if (!response.ok) {
                     const errData = await response.json().catch(() => ({message: "Neznámá chyba."}));
                    throw new Error(errData.message || `Chyba ${response.status} při mazání serveru.`);
                }
                return true;
            } catch (err: any) { Swal.showValidationMessage(err.message); return false;
            } finally { actionLoading[actionKey] = false; }
        }
    }).then((result) => {
        if (result.isConfirmed && result.value) {
            Swal.fire({...getFuturisticSwalBaseOptions('Smazáno!'), text: `Server ${server.name} byl úspěšně smazán.`, icon: 'success'});
        }
    });
};

const viewServerLogs = async (server: GameServerDto) => {
    if (!server.containerId) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: 'Server nemá přiřazené ID kontejneru.', icon: 'error'}); return; }
    const actionKey = server.gameServerId + '_logs';
    actionLoading[actionKey] = true;
    try {
        const logs = await getContainerLogs(server.containerId, 500);
        Swal.fire({
            ...getFuturisticSwalBaseOptions(`Logy serveru: ${server.name}`),
            html: `<pre class="server-logs-pre">${logs.replace(/</g, "&lt;").replace(/>/g, "&gt;")}</pre>`,
            width: '90vw',
            customClass: { popup: 'futuristic-swal-popup logs-swal', htmlContainer: 'futuristic-swal-html-container font-inter' },
            confirmButtonText: 'Zavřít'
        });
    } catch (e: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba!'), text: e.message, icon: 'error'});
    } finally { actionLoading[actionKey] = false; }
};

const formatFullDateTime = (isoDateTime: string): string => {
    if (!isoDateTime) return 'N/A';
    return new Date(isoDateTime).toLocaleString('cs-CZ', { dateStyle: 'medium', timeStyle: 'short' });
};

</script>

<style scoped>
.servers-view-container { max-width: 1000px; }
.page-title { color: var(--v-theme-primary); }
.server-card {
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
  border-left-width: 5px;
  border-left-style: solid;
  border-left-color: transparent;
}
.server-card.status-border-online { border-left-color: var(--v-theme-success) !important; }
.server-card.status-border-offline { border-left-color: var(--v-theme-error) !important; }
.server-card.status-border-starting,
.server-card.status-border-installing,
.server-card.status-border-updating,
.server-card.status-border-restarting { border-left-color: var(--v-theme-info) !important; }
.server-card.status-border-stopping { border-left-color: var(--v-theme-warning) !important; }
.server-card.status-border-error { border-left-color: var(--v-theme-deep-orange-accent-4) !important; }
.server-card.status-border-pendingcreation,
.server-card.status-border-unknown { border-left-color: var(--v-theme-grey-darken-1) !important; }

.server-card:hover { transform: translateY(-5px); box-shadow: 0 8px 25px rgba(var(--v-theme-primary-rgb), 0.2); }
.status-chip { font-size: 0.75rem !important; font-weight: 500; }
.server-name { font-size: 1.1rem !important; font-weight: 500; }
.status-details-text {
    white-space: pre-wrap; 
    max-height: 60px;
    overflow-y: auto;
    background-color: rgba(var(--v-theme-on-surface-rgb), 0.05);
    padding: 4px 6px;
    border-radius: 4px;
    font-size: 0.75rem;
    border: 1px solid rgba(var(--v-theme-on-surface-rgb), 0.1);
    line-height: 1.4;
}
.server-actions { border-top: 1px solid rgba(var(--v-theme-text-primary-rgb), 0.1); padding-top: 8px !important; }

:deep(.swal-form-container) { text-align: left; max-height: 70vh; overflow-y: auto; padding: 0 1em 1em 1em; }
:deep(.swal-form-container .swal-label) { display: block; color: var(--v-theme-text-secondary); margin-bottom: .25em; margin-top: .75em; font-size: 0.9rem; }
:deep(.swal-form-container .futuristic-swal-input) { width: 100%; box-sizing: border-box; }
:deep(.large-swal) { width: 600px !important; }
:deep(.logs-swal .swal2-html-container) { max-width: 100%; }
:deep(.server-logs-pre) {
    text-align: left; max-height: 70vh; overflow-y: auto;
    background-color: #010409; color: #c9d1d9;
    padding: 15px; border-radius: 6px; font-size: 0.8rem;
    white-space: pre-wrap; word-break: break-all;
    border: 1px solid rgba(var(--v-theme-primary-rgb), 0.3);
}
</style>
