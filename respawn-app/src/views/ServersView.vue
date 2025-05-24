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

    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4"></v-progress-linear>
    <v-alert v-if="error" type="error" prominent class="mb-4 font-inter">{{ error }}</v-alert>

    <div v-if="!loading && servers.length === 0 && !error" class="text-center pa-8">
      <v-icon :icon="mdiServerOff" size="64" color="grey-darken-1"></v-icon>
      <p class="text-h6 font-inter mt-4 text-grey-darken-1">Zatím nebyly vytvořeny žádné herní servery.</p>
    </div>

    <v-row justify="center" v-if="!loading && servers.length > 0">
      <v-col v-for="server in servers" :key="server.gameServerId" cols="12" md="10" lg="10">
        <v-card class="futuristic-card server-card mb-6" :elevation="server.status === ServerStatus.Online ? 8 : 2" :class="`status-${server.status.toString().toLowerCase()}`">
          <v-card-title class="d-flex align-center">
            <v-icon :icon="getGameIcon(server.gameType)" class="mr-3" :color="getOverallStatusColor(server.status)"></v-icon>
            <span class="font-exo2 server-name">{{ server.name }}</span>
            <v-spacer></v-spacer>
             <v-chip :color="getOverallStatusColor(server.status)" label small class="font-exo2 status-chip mr-1">
                {{ getServerStatusText(server.status) }}
            </v-chip>
            <v-tooltip location="top" v-if="server.lgsmServerStatus">
                <template v-slot:activator="{ props }">
                     <v-chip :color="getLgsmStatusColor(server.lgsmServerStatus)" label small class="font-exo2 status-chip" v-bind="props">
                        LGSM: {{ server.lgsmServerStatus }}
                    </v-chip>
                </template>
                <span>Stav herního serveru (LinuxGSM)</span>
            </v-tooltip>
          </v-card-title>
          <v-card-subtitle class="font-inter">
            Typ: {{ getGameTypeText(server.gameType) }}
            <span v-if="server.ipAddress && server.port"> | {{ server.ipAddress }}:{{ server.port }}</span>
            <span v-else-if="server.ipAddress"> | {{ server.ipAddress }}</span>
          </v-card-subtitle>

          <v-card-text>
            <p class="text-caption text-grey-lighten-1 mb-1">Kontejner: {{ server.containerId?.substring(0,12) || 'N/A' }}</p>
            <p v-if="server.statusDetails" class="text-caption font-roboto-mono status-details-text">
              Detail: {{ server.statusDetails }}
            </p>
             <v-progress-linear
                v-if="server.status === ServerStatus.Installing || server.status === ServerStatus.Starting || server.status === ServerStatus.Stopping || server.status === ServerStatus.PendingCreation"
                indeterminate
                color="primary"
                height="4"
                class="mt-2"
            ></v-progress-linear>
          </v-card-text>

          <v-card-actions v-if="canManageServers" class="server-actions">
            <v-btn
              small
              :color="server.status === ServerStatus.Online ? 'warning' : 'success'"
              @click="toggleServerState(server)"
              :loading="actionLoading[server.gameServerId + '_toggle']"
              :disabled="server.status === ServerStatus.Installing || server.status === ServerStatus.PendingCreation || server.status === ServerStatus.Stopping || server.status === ServerStatus.Starting || server.status === ServerStatus.Deleting"
              class="futuristic-btn-secondary"
            >
              <v-icon left>{{ server.status === ServerStatus.Online ? mdiStopCircleOutline : mdiPlayCircleOutline }}</v-icon>
              {{ server.status === ServerStatus.Online ? 'Stop' : 'Start' }}
            </v-btn>
            <v-spacer></v-spacer>
            <v-btn :icon="mdiConsoleLine" color="info" variant="text" @click="viewServerLogs(server)" :loading="actionLoading[server.gameServerId + '_logs']" title="Zobrazit logy" :disabled="!server.containerId"></v-btn>
            <v-btn :icon="mdiDelete" color="error" variant="text" @click="confirmDeleteServer(server)" :loading="actionLoading[server.gameServerId + '_delete']" title="Smazat server"></v-btn>
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
  mdiServer, mdiTag, mdiCog // Ikony pro modal
} from '@mdi/js';
import { signalRService } from '@/services/signalrService';

interface GameServerDto {
  gameServerId: string;
  name: string;
  gameType: GameType;
  status: ServerStatus;
  lgsmServerStatus?: string;
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
    newLgsmServerStatus?: string;
    statusDetails?: string;
    errorMessage?: string;
}

const API_BASE_URL = 'http://localhost:5207/api/gameservers';
const authStore = useAuthStore();
const servers = ref<GameServerDto[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);
const actionLoading = reactive<Record<string, boolean>>({});

const canManageServers = computed(() => {
  return authStore.isLoggedIn && (authStore.user?.roles.includes(UserRoles.Administrator) || authStore.user?.roles.includes(UserRoles.Spravce));
});

const getGameIcon = (gameType: GameType) => ({
  [GameType.CounterStrike]: mdiServerOff,
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

const getLgsmStatusColor = (lgsmStatus?: string): string => {
    if (!lgsmStatus) return 'grey';
    const statusUpper = lgsmStatus.toUpperCase();
    if (statusUpper.includes("ONLINE")) return 'success';
    if (statusUpper.includes("STARTING") || statusUpper.includes("LOADING")) return 'info';
    if (statusUpper.includes("INSTALLING") || statusUpper.includes("UPDATING")) return 'blue-grey';
    if (statusUpper.includes("STOPPED") || statusUpper.includes("OFFLINE")) return 'error';
    if (statusUpper.includes("ERROR")) return 'deep-orange-accent-4';
    return 'grey-darken-1';
};

const fetchServers = async () => {
  loading.value = true; error.value = null;
  try {
    const response = await fetch(API_BASE_URL, { headers: { 'Authorization': `Bearer ${authStore.token}` } });
    if (!response.ok) throw new Error('Nepodařilo se načíst servery.');
    servers.value = await response.json();
  } catch (err: any) { error.value = err.message; } finally { loading.value = false; }
};

const handleReceiveGameServerUpdate = (updatedServer: GameServerDto) => {
  const index = servers.value.findIndex(s => s.gameServerId === updatedServer.gameServerId);
  if (index !== -1) servers.value[index] = { ...servers.value[index], ...updatedServer };
  else servers.value.unshift(updatedServer);
};
const handleReceiveGameServerStatusUpdate = (statusUpdate: GameServerStatusUpdateDtoFE) => {
  const server = servers.value.find(s => s.gameServerId === statusUpdate.gameServerId);
  if (server) {
    server.status = statusUpdate.newOverallStatus;
    server.lgsmServerStatus = statusUpdate.newLgsmServerStatus || server.lgsmServerStatus;
    server.statusDetails = statusUpdate.statusDetails || server.statusDetails;
    if(statusUpdate.errorMessage) { server.statusDetails = `Chyba: ${statusUpdate.errorMessage}`; server.status = ServerStatus.Error; server.lgsmServerStatus = "ERROR"; }
  }
};
const handleReceiveGameServerRemoval = (removedServerId: string) => {
  servers.value = servers.value.filter(s => s.gameServerId !== removedServerId);
};

onMounted(async () => {
  await fetchServers();
  if (authStore.isLoggedIn) {
    try {
      // Předpokládáme, že signalRService je již nakonfigurována a připravena pro /gameServerHub
      // nebo že startConnection je voláno s příslušnou cestou v App.vue nebo zde.
      if (signalRService.getConnectionState() !== 'Connected' && signalRService.getConnectionState() !== 'Connecting') {
         console.warn("SignalR connection for GameServerHub not explicitly started in this example. Assuming it's handled elsewhere or pollHub connection is reused.");
         // await signalRService.startConnection("/gameServerHub"); // Příklad explicitního startu
      }
      signalRService.on("ReceiveGameServerUpdate", handleReceiveGameServerUpdate);
      signalRService.on("ReceiveGameServerStatusUpdate", handleReceiveGameServerStatusUpdate);
      signalRService.on("ReceiveGameServerRemoval", handleReceiveGameServerRemoval);
    } catch (err) { console.error("Chyba při startu SignalR pro GameServerHub:", err); error.value = "Chyba real-time spojení pro servery."; }
  }
});

onBeforeUnmount(() => {
    signalRService.off("ReceiveGameServerUpdate", handleReceiveGameServerUpdate);
    signalRService.off("ReceiveGameServerStatusUpdate", handleReceiveGameServerStatusUpdate);
    signalRService.off("ReceiveGameServerRemoval", handleReceiveGameServerRemoval);
});

// Použití base options pro konzistentní vzhled SweetAlert2 dialogů
const getFuturisticSwalBaseOptions = (title: string): SweetAlertOptions => ({
  titleText: title,
  background: '#1A2033',
  color: '#E0E0E0',
  confirmButtonColor: '#00E0FF',
  cancelButtonColor: '#FF5252',
  customClass: {
    popup: 'futuristic-swal-popup',
    title: 'futuristic-swal-title font-oxanium',
    htmlContainer: 'futuristic-swal-html-container font-inter',
    input: 'futuristic-swal-input', // Důležitá třída pro inputy
    confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
    cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
    actions: 'futuristic-swal-actions',
    validationMessage: 'futuristic-swal-validation-message font-inter',
  },
  buttonsStyling: false,
  heightAuto: false,
  allowEnterKey: true,
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
        ...getFuturisticSwalBaseOptions('Vytvořit nový herní server'), // Použití base options
        html: `
        <div class="swal-form-container"> <label for="swal-server-name" class="swal-label">
                ${createIconHtml(mdiServer, 18, iconColor, iconStyle)}Název serveru:
            </label>
            <input id="swal-server-name" class="swal2-input futuristic-swal-input" placeholder="Můj CS Server">

            <label for="swal-game-type" class="swal-label mt-3">
                ${createIconHtml(mdiTag, 18, iconColor, iconStyle)}Typ hry:
            </label>
            <select id="swal-game-type" class="swal2-input futuristic-swal-input">
                ${gameTypesHtml}
            </select>

            <label for="swal-gs-params" class="swal-label mt-3">
                 ${createIconHtml(mdiCog, 18, iconColor, iconStyle)}Extra GS_PARAMS (volitelné):
            </label>
            <input id="swal-gs-params" class="swal2-input futuristic-swal-input" placeholder="-port 27016 +map de_dust2">
        </div>
        `,
        customClass: { // Přidání třídy pro širší modal, pokud je potřeba
            popup: 'futuristic-swal-popup large-swal', // 'large-swal' může být definováno globálně nebo zde
            htmlContainer: 'futuristic-swal-html-container font-inter swal-form-container-custom-padding',
            // Ostatní třídy jsou již v getFuturisticSwalBaseOptions
        },
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
                Swal.fire('Vytváření zahájeno', 'Požadavek na vytvoření serveru byl odeslán. Stav se brzy aktualizuje.', 'info');
            } catch (err: any) { Swal.fire('Chyba!', err.message, 'error');
            } finally { actionLoading['new_server'] = false; }
        }
    });
};

const toggleServerState = async (server: GameServerDto) => {
    if (!server.containerId) { Swal.fire('Chyba', 'Server nemá přiřazené ID kontejneru.', 'error'); return; }
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
        Swal.fire('Příkaz odeslán', `Požadavek na ${action} serveru byl odeslán.`, 'info');
    } catch (err: any) { Swal.fire('Chyba!', err.message, 'error');
    } finally { actionLoading[actionKey] = false; }
};

const confirmDeleteServer = (server: GameServerDto) => {
    const actionKey = server.gameServerId + '_delete';
    Swal.fire({
        ...getFuturisticSwalBaseOptions('Opravdu smazat server?'), // Použití base options
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
            Swal.fire({...getFuturisticSwalBaseOptions('Smazáno!'), text: 'Server byl úspěšně smazán.', icon: 'success'});
        }
    });
};

const viewServerLogs = async (server: GameServerDto) => {
    if (!server.containerId) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: 'Server nemá přiřazené ID kontejneru.', icon: 'error'}); return; }
    const actionKey = server.gameServerId + '_logs';
    actionLoading[actionKey] = true;
    try {
        const response = await fetch(`${API_BASE_URL}/${server.gameServerId}/logs?tail=200`, { headers: { 'Authorization': `Bearer ${authStore.token}` } });
        if (!response.ok) {
            const errData = await response.text();
            throw new Error(errData || `Nepodařilo se načíst logy serveru: ${response.statusText}`);
        }
        const logs = await response.text();

        Swal.fire({
            ...getFuturisticSwalBaseOptions(`Logy serveru: ${server.name}`), // Použití base options
            html: `<pre class="server-logs-pre">${logs.replace(/</g, "&lt;").replace(/>/g, "&gt;")}</pre>`,
            customClass: { // Přidání třídy pro ještě širší modal pro logy
                popup: 'futuristic-swal-popup extra-large-swal logs-swal',
                htmlContainer: 'futuristic-swal-html-container font-inter', // htmlContainer je již v base
            },
            confirmButtonText: 'Zavřít'
        });
    } catch (err: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba!'), text: err.message, icon: 'error'});
    } finally { actionLoading[actionKey] = false; }
};

</script>

<style scoped>
.servers-view-container {
  max-width: 1000px; /* Mírně zvětšeno pro širší karty */
}
.page-title { color: var(--v-theme-primary); }
.server-card {
  transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
  border-left-width: 4px;
  border-left-style: solid;
  border-left-color: transparent;
}
.server-card.status-online { border-left-color: var(--v-theme-success); }
.server-card.status-offline { border-left-color: var(--v-theme-error); }
.server-card.status-starting, .server-card.status-installing { border-left-color: var(--v-theme-info); }
.server-card.status-stopping { border-left-color: var(--v-theme-warning); }
.server-card.status-error { border-left-color: var(--v-theme-deep-orange-accent-4); }

.server-card:hover { transform: translateY(-5px); box-shadow: 0 8px 25px rgba(var(--v-theme-primary-rgb), 0.2); }
.status-chip { font-size: 0.75rem !important; font-weight: 500; }
.server-name { font-size: 1.1rem !important; font-weight: 500; }
.status-details-text {
    white-space: pre-wrap; 
    max-height: 100px;
    overflow-y: auto;
    background-color: rgba(var(--v-theme-on-surface-rgb), 0.05); /* Změna pro lepší kontrast */
    padding: 6px 8px;
    border-radius: 4px;
    font-size: 0.75rem;
    border: 1px solid rgba(var(--v-theme-on-surface-rgb), 0.1);
}
.server-actions { border-top: 1px solid rgba(var(--v-theme-text-primary-rgb), 0.1); padding-top: 8px !important; }

/* Styly pro SweetAlert formulář, konzistentní s authService */
/* :deep() je zde nutné, protože SweetAlert generuje HTML mimo scope komponenty */
:deep(.swal-form-container) {
  text-align: left;
  max-height: 70vh;
  overflow-y: auto;
  padding-right: 15px; /* Prostor pro scrollbar */
  padding-left: 5px;
}
:deep(.swal-form-container .swal-label) {
    display: flex;
    align-items: center;
    color: var(--v-theme-text-secondary);
    margin-bottom: 0.35rem;
    margin-top: 1rem; /* Větší odsazení mezi poli */
    font-size: 0.9rem;
}
:deep(.swal-form-container .swal-label > svg) { /* Cílení na SVG ikony v labelu */
    margin-right: 8px;
    /* Barva je již nastavena v createIconHtml */
}
:deep(.swal-form-container .futuristic-swal-input) { /* Třída z getFuturisticSwalBaseOptions */
    width: 100%;
    box-sizing: border-box;
}

/* Šířka pro modální okna */
:deep(.large-swal) { /* Pro "Vytvořit server" a podobné */
    width: 600px !important;
}
:deep(.extra-large-swal) { /* Pro logy serveru */
    width: 85vw !important;
    max-width: 1000px !important;
}
:deep(.logs-swal .swal2-html-container) { /* Zajistí, že pre tag se roztáhne */
    max-width: 100%;
}
:deep(.server-logs-pre) {
    text-align: left;
    max-height: 65vh; /* Omezení výšky logů */
    overflow-y: auto;
    background-color: #010409; /* Tmavší pozadí pro logy */
    color: #c9d1d9;
    padding: 15px;
    border-radius: 6px;
    font-size: 0.8rem;
    white-space: pre-wrap;
    word-break: break-all;
    border: 1px solid rgba(var(--v-theme-primary-rgb), 0.3);
    width: 95%;
}
</style>
