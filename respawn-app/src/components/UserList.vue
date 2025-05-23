<template>
  <v-card class="futuristic-card user-list-card" elevation="0" tile>
    <v-toolbar density="compact" class="futuristic-toolbar">
      <v-toolbar-title class="font-oxanium text-subtitle-1">
        Uživatelé
      </v-toolbar-title>
      <v-spacer></v-spacer>
      <v-chip
        label
        size="small"
        color="green-accent-4"
        class="mr-1 font-weight-bold"
      >
        {{ onlineCount }} Online
      </v-chip>
    </v-toolbar>
    <v-divider></v-divider>

    <v-progress-linear v-if="loading" indeterminate color="primary" height="3"></v-progress-linear>
    <v-alert v-if="error" type="error" density="compact" class="ma-2 font-inter" variant="tonal">
      {{ error }}
    </v-alert>

    <v-list dense class="user-list-scrollable" v-if="!loading && usersToDisplay.length > 0">
      <v-list-item
        v-for="user in usersToDisplay" :key="user.userId"
        class="user-list-item pa-2"
        lines="two"
      >
        <template v-slot:prepend>
          <v-avatar size="36" class="mr-3">
            <v-img :src="user.avatarUrl || defaultAvatar" :alt="user.nickname">
                <template v-slot:placeholder>
                    <v-icon :icon="mdiAccountCircleOutline" size="36"></v-icon>
                </template>
                 <template v-slot:error>
                    <v-icon :icon="mdiAccountCircleOutline" size="36"></v-icon>
                </template>
            </v-img>
          </v-avatar>
        </template>

        <v-list-item-title class="font-inter user-nickname font-weight-medium">{{ user.nickname }}</v-list-item-title>
        <v-list-item-subtitle class="font-inter text-caption">
            <span :class="['status-dot', user.isOnline ? 'online' : 'offline']"></span>
            {{ user.isOnline ? 'Online' : (user.lastSeen ? `Naposledy: ${formatLastSeen(user.lastSeen)}` : 'Offline') }}
        </v-list-item-subtitle>

      </v-list-item>
    </v-list>
    <div v-if="!loading && usersToDisplay.length === 0 && !error" class="text-center pa-4 text-grey-darken-1 font-inter">
      Žádní další uživatelé k zobrazení.
    </div>
  </v-card>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed } from 'vue';
import { presenceSignalRService } from '@/services/presenceSignalrService';
import { mdiAccountCircleOutline } from '@mdi/js';
import { useAuthStore } from '@/stores/authStore';

interface UserStatus {
  userId: string;
  nickname: string;
  avatarUrl?: string;
  isOnline: boolean;
  lastSeen?: string; // ISO string
}

const users = ref<UserStatus[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);
const defaultAvatar = 'https://via.placeholder.com/40/1A2033/E0E0E0?text=U';
const authStore = useAuthStore();

const fetchInitialUserList = async () => {
  loading.value = true;
  error.value = null;
  try {
    const token = authStore.token;
    if (!token) {
        error.value = "Pro zobrazení seznamu uživatelů je nutné přihlášení.";
        loading.value = false;
        return;
    }

    const response = await fetch('http://localhost:5207/api/presence/users', {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Nepodařilo se načíst seznam uživatelů: ${response.statusText}`);
    }
    const data: UserStatus[] = await response.json();
    users.value = data;
  } catch (err: any) {
    console.error("Chyba při načítání seznamu uživatelů:", err);
    error.value = err.message;
  } finally {
    loading.value = false;
  }
};

const handleUserOnline = (userStatus: UserStatus) => {
  console.log('SignalR UserList: UserOnline', userStatus);
  if (userStatus.userId === authStore.user?.id) return; // Ignorovat aktualizaci pro sebe sama

  const index = users.value.findIndex(u => u.userId === userStatus.userId);
  if (index !== -1) {
    users.value[index].isOnline = true;
    users.value[index].lastSeen = userStatus.lastSeen || new Date().toISOString();
  } else {
    users.value.push({ ...userStatus, isOnline: true });
  }
};

const handleUserOffline = (userStatus: UserStatus) => {
  console.log('SignalR UserList: UserOffline', userStatus);
  if (userStatus.userId === authStore.user?.id) return; // Ignorovat aktualizaci pro sebe sama

  const index = users.value.findIndex(u => u.userId === userStatus.userId);
  if (index !== -1) {
    users.value[index].isOnline = false;
    users.value[index].lastSeen = userStatus.lastSeen || new Date().toISOString();
  }
};

// Nová computed property pro filtrovaný a seřazený seznam uživatelů
const usersToDisplay = computed(() => {
  const currentUserId = authStore.user?.id;
  return users.value
    .filter(user => user.userId !== currentUserId) // Odstranění aktuálně přihlášeného uživatele
    .sort((a, b) => {
      if (a.isOnline && !b.isOnline) return -1;
      if (!a.isOnline && b.isOnline) return 1;
      return a.nickname.localeCompare(b.nickname, 'cs', { sensitivity: 'base' });
    });
});

// Původní sortedUsers již není potřeba, pokud používáme usersToDisplay
// const sortedUsers = computed(() => { ... });

const onlineCount = computed(() => {
    // Počítáme online uživatele z filtrovaného seznamu, aby se nezapočítával aktuální uživatel
    return usersToDisplay.value.filter(u => u.isOnline).length;
});

const formatLastSeen = (isoDateTime?: string): string => {
    if (!isoDateTime) return 'Neznámé';
    const date = new Date(isoDateTime);
    const now = new Date();
    const diffSeconds = Math.round((now.getTime() - date.getTime()) / 1000);

    if (diffSeconds < 60) return `před ${diffSeconds} s`;
    const diffMinutes = Math.round(diffSeconds / 60);
    if (diffMinutes < 60) return `před ${diffMinutes} min`;
    const diffHours = Math.round(diffMinutes / 60);
    if (diffHours < 24) return `před ${diffHours} h`;
    const diffDays = Math.round(diffHours / 24);
    return `před ${diffDays} dny`;
};


onMounted(async () => {
  if (authStore.isLoggedIn) {
    await fetchInitialUserList();
    try {
      await presenceSignalRService.startConnection();
      presenceSignalRService.on("UserOnline", handleUserOnline);
      presenceSignalRService.on("UserOffline", handleUserOffline);
    } catch (err: any) {
      console.error("SignalR Presence connection error on mount:", err);
      error.value = "Chyba real-time spojení pro status uživatelů.";
    }
  } else {
    loading.value = false;
  }
});

onBeforeUnmount(() => {
  presenceSignalRService.off("UserOnline", handleUserOnline);
  presenceSignalRService.off("UserOffline", handleUserOffline);
  // Spojení se zastaví globálně, pokud je to potřeba (např. v App.vue při odhlášení)
});

</script>

<style scoped>
.user-list-card {
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: rgba(var(--v-theme-surface-rgb), 0.9) !important;
  backdrop-filter: blur(5px);
  border-left: 1px solid rgba(var(--v-theme-primary-rgb), 0.2);
}

.futuristic-toolbar {
  background-color: transparent !important;
  color: var(--v-theme-text-primary);
}

.user-list-scrollable {
  flex-grow: 1;
  overflow-y: auto;
  background-color: transparent !important;
}

.user-list-item {
  border-bottom: 1px solid rgba(var(--v-theme-text-primary-rgb), 0.08);
  transition: background-color 0.2s ease-in-out;
}

.user-list-item:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.05);
}

.user-list-item:last-child {
  border-bottom: none;
}

.user-nickname {
  color: var(--v-theme-text-primary);
}

.status-indicator-wrapper {
  display: flex;
  align-items: center;
}

.status-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  display: inline-block;
  margin-right: 6px;
  border: 1px solid rgba(0,0,0,0.2);
}

.status-dot.online {
  background-color: #4CAF50; /* Zelená */
  box-shadow: 0 0 5px #4CAF50;
}

.status-dot.offline {
  background-color: #F44336; /* Červená */
}
.status-text {
    color: var(--v-theme-text-secondary);
}
</style>
