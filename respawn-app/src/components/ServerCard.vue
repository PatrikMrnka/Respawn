<template>
  <!-- Main server card component with dynamic status-based styling -->
  <v-card
    class="futuristic-card server-card mb-6"
    :elevation="displayHelpers.getOverallStatusColor(server.status) === 'success' ? 8 : 2"
    :class="`status-border-${server.status.toString().toLowerCase()}`"
    @click="onCardClick"
    :disabled="
      server.status === ServerStatus.PendingCreation || isActionDisabledForCard(server.status)
    "
    :title="cardTitle"
    hover
  >
    <!-- Card header with game icon, server name and status chip -->
    <v-card-title class="d-flex align-center">
      <v-icon
        :icon="displayHelpers.getGameIcon(server.gameType)"
        class="mr-3 server-card-game-icon"
        :color="displayHelpers.getOverallStatusColor(server.status)"
      ></v-icon>
      <span class="font-exo2 server-name">{{ server.name }}</span>
      <v-spacer></v-spacer>
      <!-- Status indicator chip with appropriate color and icon -->
      <v-chip
        :color="displayHelpers.getOverallStatusColor(server.status)"
        label
        small
        class="font-exo2 status-chip mr-1"
        :title="`Stav: ${displayHelpers.getServerStatusText(server.status)}`"
      >
        <v-icon
          start
          :icon="displayHelpers.getServerStatusIcon(server.status)"
          size="small"
        ></v-icon>
        {{ displayHelpers.getServerStatusText(server.status) }}
      </v-chip>
    </v-card-title>

    <!-- Server details subtitle section -->
    <v-card-subtitle class="font-inter">
      Typ: {{ displayHelpers.getGameTypeText(server.gameType) }}
      <span v-if="server.ipAddress && server.port">
        | {{ server.ipAddress }}:{{ server.port }}</span
      >
      <span v-else-if="server.ipAddress"> | {{ server.ipAddress }}</span>
      <br v-if="server.ipAddress" />
      Vytvořeno: {{ displayHelpers.formatFullDateTime(server.createdAt) }}
    </v-card-subtitle>

    <!-- Container ID and status details section -->
    <v-card-text>
      <p class="text-caption text-grey-lighten-1 mb-1">
        Kontejner: {{ server.containerId?.substring(0, 12) || 'N/A' }}
      </p>
      <p
        v-if="server.statusDetails"
        class="text-caption font-roboto-mono status-details-text mt-1"
        :title="server.statusDetails"
      >
        Detail: {{ displayHelpers.truncateText(server.statusDetails, 100) }}
      </p>
      <!-- Progress indicator for servers in loading/transitional states -->
      <v-progress-linear
        v-if="displayHelpers.isLoadingStatus(server.status)"
        indeterminate
        :color="displayHelpers.getOverallStatusColor(server.status)"
        height="5"
        class="mt-2 server-card-progress"
      ></v-progress-linear>
    </v-card-text>

    <!-- Action buttons for server management (conditionally rendered) -->
    <v-card-actions v-if="canManage" class="server-actions" @click.stop>
      <v-btn
        small
        :color="server.status === ServerStatus.Online ? 'warning' : 'success'"
        @click="$emit('toggle-state', server)"
        :loading="actionLoadingStates.toggle"
        :disabled="displayHelpers.isActionDisabled(server.status)"
        class="futuristic-btn-secondary"
      >
        <v-icon left>{{
          server.status === ServerStatus.Online ? mdiStopCircleOutline : mdiPlayCircleOutline
        }}</v-icon>
        {{ server.status === ServerStatus.Online ? 'Stop' : 'Start' }}
      </v-btn>
      <v-spacer></v-spacer>
      <!-- Delete server button -->
      <v-btn
        :icon="mdiDelete"
        color="error"
        variant="text"
        @click="$emit('delete-server', server)"
        :loading="actionLoadingStates.delete"
        title="Smazat server"
        :disabled="
          displayHelpers.isActionDisabled(server.status) &&
          server.status !== ServerStatus.Error &&
          server.status !== ServerStatus.Offline
        "
        class="futuristic-icon-btn"
      ></v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { PropType } from 'vue'
import { GameType, ServerStatus } from '@/types/enums'
import { useGameDisplayHelpers } from '@/composables/useGameDisplayHelpers'
import { mdiPlayCircleOutline, mdiStopCircleOutline, mdiDelete } from '@mdi/js'

// DTO interface for game server data
export interface GameServerDto {
  gameServerId: string
  name: string
  gameType: GameType
  status: ServerStatus
  ipAddress?: string
  port?: number
  containerId?: string
  createdAt: string
  statusDetails?: string
}

// Interface for server status updates
export interface GameServerStatusUpdateDtoFE {
  gameServerId: string
  newOverallStatus: ServerStatus
  statusDetails?: string
  errorMessage?: string
}

// Component props definition
const props = defineProps({
  server: {
    type: Object as PropType<GameServerDto>,
    required: true,
  },
  canManage: {
    type: Boolean,
    default: false,
  },
  actionLoadingStates: {
    type: Object as PropType<{ toggle?: boolean; logs?: boolean; delete?: boolean }>,
    default: () => ({ toggle: false, logs: false, delete: false }),
  },
})

// Event emitters definition
const emit = defineEmits<{
  (e: 'toggle-state', server: GameServerDto): void
  (e: 'view-logs', server: GameServerDto): void
  (e: 'delete-server', server: GameServerDto): void
  (e: 'navigate-to-detail', server: GameServerDto): void
}>()

// Helper functions for display formatting
const displayHelpers = useGameDisplayHelpers()

// Check if actions should be disabled based on server status
const isActionDisabledForCard = (status: ServerStatus): boolean => {
  return status === ServerStatus.PendingCreation || displayHelpers.isLoadingStatus(status)
}

// Dynamic card title based on server status
const cardTitle = computed(() => {
  if (
    props.server.status === ServerStatus.PendingCreation ||
    isActionDisabledForCard(props.server.status)
  ) {
    return 'Server se vytváří/je ve stavu přechodu, detail nemusí být plně dostupný.'
  }
  return `Zobrazit detail serveru ${props.server.name}`
})

// Handle card click event - navigate to server detail if not disabled
const onCardClick = () => {
  if (!isActionDisabledForCard(props.server.status)) {
    emit('navigate-to-detail', props.server)
  }
}
</script>

<style scoped>
/* Main server card styling with futuristic design */
.server-card {
  transition:
    transform 0.2s ease-in-out,
    box-shadow 0.2s ease-in-out,
    border-color 0.3s ease;
  border-left-width: 5px;
  border-left-style: solid;
  border-left-color: transparent;
  cursor: pointer;
  background-color: rgba(var(--v-theme-surface-rgb), 0.7);
  backdrop-filter: blur(5px);
}

/* Disabled server card styling */
.server-card[disabled='true'] {
  cursor: not-allowed;
  opacity: 0.6;
  filter: grayscale(50%);
}

/* Hover effect for enabled server cards */
.server-card:not([disabled='true']):hover {
  transform: translateY(-5px) scale(1.01);
  box-shadow:
    0 10px 30px rgba(var(--v-theme-primary-rgb), 0.25),
    0 0 15px rgba(var(--v-theme-secondary-rgb), 0.15) inset;
}

/* Status-specific left border colors */
.status-border-online {
  border-left-color: rgb(var(--v-theme-success)) !important;
}
.status-border-offline {
  border-left-color: rgb(var(--v-theme-error)) !important;
}
.status-border-starting,
.status-border-restarting {
  border-left-color: rgb(var(--v-theme-info)) !important;
}
.status-border-stopping {
  border-left-color: rgb(var(--v-theme-warning)) !important;
}
.status-border-error {
  border-left-color: rgb(var(--v-theme-error)) !important;
}
.status-border-pendingcreation,
.status-border-unknown {
  border-left-color: rgb(var(--v-theme-grey-darken-1)) !important;
}

/* Game icon styling with glow effect */
.server-card-game-icon {
  opacity: 0.9;
  filter: drop-shadow(0 0 3px rgba(255, 255, 255, 0.3));
}

/* Status chip styling */
.status-chip {
  font-size: 0.75rem !important;
  font-weight: 500;
  padding: 0 10px !important;
  height: 24px !important;
  box-shadow: 0 0 8px -2px currentColor;
}
.status-chip .v-icon {
  margin-right: 4px;
}

/* Server name typography */
.server-name {
  font-size: 1.2rem !important;
  font-weight: 500;
  color: var(--v-theme-text-primary);
}

/* Card subtitle typography */
.v-card-subtitle {
  font-size: 0.8rem;
  line-height: 1.4;
  color: var(--v-theme-text-secondary);
}

/* Status details text box styling */
.status-details-text {
  white-space: pre-wrap;
  max-height: 60px;
  overflow-y: auto;
  background-color: rgba(var(--v-theme-on-surface-rgb), 0.03);
  padding: 6px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  border: 1px solid rgba(var(--v-theme-on-surface-rgb), 0.08);
  line-height: 1.4;
  color: var(--v-theme-text-secondary);
}

/* Progress bar styling */
.server-card-progress {
  border-radius: 3px;
  opacity: 0.7;
}

/* Action buttons container styling */
.server-actions {
  border-top: 1px solid rgba(var(--v-theme-text-primary-rgb), 0.1);
  padding: 8px 12px !important;
  background-color: rgba(var(--v-theme-surface-rgb), 0.3);
}

/* Secondary button styling with glow effect on hover */
.futuristic-btn-secondary {
  font-family: 'Exo 2', sans-serif;
  box-shadow: 0 0 8px 0px transparent;
  transition: all 0.2s ease-in-out;
  border-radius: 6px;
  padding: 6px 12px !important;
  font-size: 0.8rem !important;
}
.futuristic-btn-secondary:hover {
  box-shadow: 0 0 12px 2px var(--v-theme-glow-color);
  transform: translateY(-1px);
}

/* Icon button styling */
.futuristic-icon-btn {
  color: rgba(var(--v-theme-on-surface-rgb), 0.7) !important;
}
.futuristic-icon-btn:hover {
  color: var(--v-theme-primary) !important;
}
</style>
