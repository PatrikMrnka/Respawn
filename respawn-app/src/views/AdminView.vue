<template>
  <v-container fluid class="admin-view pa-md-8">
    <v-responsive class="mx-auto" max-width="1200">
      <h1 class="text-h3 font-oxanium mb-8 text-center text-primary">Administrace</h1>

      <v-card class="mb-8 futuristic-card" elevation="8">
        <v-card-title class="text-h5 font-exo2 card-header d-flex align-center">
          <v-icon start color="primary">{{ mdiAccountGroup }}</v-icon>
          Sprava Uzivatelu
        </v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="4">
               <v-text-field
                v-model="searchQuery"
                label="Hledat uzivatele (ID, Nick, Email)"
                prepend-inner-icon="mdi-magnify"
                variant="outlined"
                density="compact"
                clearable
                class="mb-4 futuristic-input"
              ></v-text-field>
            </v-col>
            <v-col cols="12" class="text-right">
                <v-btn @click="fetchUsers" color="primary" class="futuristic-btn" :loading="loadingUsers">
                    <v-icon left>{{ mdiRefresh }}</v-icon>
                    Obnovit seznam
                </v-btn>
            </v-col>
          </v-row>

          <v-data-table
            :headers="userHeaders"
            :items="filteredUsers"
            item-value="id"
            class="elevation-2 futuristic-table"
            :loading="loadingUsers"
            loading-text="Nacitam uzivatele..."
            no-data-text="Zadni uzivatele nebyli nalezeni."
            :items-per-page="10"
            :search="searchQuery"
          >
            <template v-slot:item.roles="{ item }">
              <v-chip-group column>
                <v-chip
                  v-for="role in item.roles"
                  :key="role"
                  :color="getRoleColor(role)"
                  label
                  small
                  class="mr-1 mb-1"
                >
                  {{ role }}
                </v-chip>
              </v-chip-group>
            </template>

            <template v-slot:item.actions="{ item }">
              <v-tooltip location="top" text="Upravit role">
                <template v-slot:activator="{ props }">
                  <v-btn
                    v-if="authStore.currentUser?.id !== item.id"
                    icon
                    variant="text"
                    color="secondary"
                    size="small"
                    @click="openEditRolesModal(item)"
                    v-bind="props"
                  >
                    <v-icon>{{ mdiAccountEdit }}</v-icon>
                  </v-btn>
                </template>
              </v-tooltip>
              <v-tooltip location="top" text="Smazat uzivatele">
                 <template v-slot:activator="{ props }">
                    <v-btn
                      v-if="authStore.currentUser?.id !== item.id"
                      icon
                      variant="text"
                      color="error"
                      size="small"
                      @click="confirmDeleteUser(item.id)"
                      v-bind="props"
                    >
                      <v-icon>{{ mdiDelete }}</v-icon>
                    </v-btn>
                 </template>
              </v-tooltip>
            </template>
             <template v-slot:loading>
                <v-skeleton-loader type="table-row@5"></v-skeleton-loader>
            </template>
          </v-data-table>
        </v-card-text>
      </v-card>

      <v-card class="futuristic-card" elevation="8">
        <v-card-title class="text-h5 font-exo2 card-header d-flex align-center">
          <v-icon start color="error">{{ mdiDatabaseAlert }}</v-icon>
          Sprava Databaze (Nebezpecne Akce)
        </v-card-title>
        <v-card-text>
          <v-alert
            type="warning"
            variant="outlined"
            class="mb-4 futuristic-alert"
            border="start"
            density="compact"
          >
            Tyto akce jsou nevratne a mohou vest ke ztrate dat. Pouzivejte s extremni opatrnosti!
          </v-alert>
          <v-row>
            <v-col cols="12" md="6">
              <v-btn
                color="warning"
                block
                class="futuristic-btn"
                @click="handleResetDatabase"
                :loading="resettingDb"
              >
                <v-icon left>{{ mdiDatabaseRefresh }}</v-icon>
                Resetovat Databazi (Seed)
              </v-btn>
              <p class="text-caption mt-1 text-text-secondary">
                Vrati databazi do vychoziho stavu (vytvori role, admin ucet atd.).
              </p>
            </v-col>
            <v-col cols="12" md="6">
              <v-btn
                color="error"
                block
                class="futuristic-btn"
                @click="handleDeleteDatabase"
                :loading="deletingDb"
              >
                <v-icon left>{{ mdiDatabaseRemove }}</v-icon>
                Smazat Celou Databazi
              </v-btn>
               <p class="text-caption mt-1 text-text-secondary">
                Kompletne smaze vsechna data z databaze.
              </p>
            </v-col>
          </v-row>
        </v-card-text>
      </v-card>
    </v-responsive>

    <v-dialog v-model="editRolesDialog" max-width="500px" persistent>
      <v-card class="futuristic-card">
        <v-card-title class="text-h5 font-exo2 card-header">
          Upravit role pro: {{ editingUser?.nickname }}
        </v-card-title>
        <v-card-text>
          <v-chip-group v-model="selectedRoles" column multiple>
            <v-chip
              v-for="role in availableRoles"
              :key="role"
              :value="role"
              filter
              :color="getRoleColor(role)"
              label
            >
              {{ role }}
            </v-chip>
          </v-chip-group>
        </v-card-text>
        <v-card-actions class="card-actions">
          <v-spacer></v-spacer>
          <v-btn color="grey-darken-1" variant="text" @click="closeEditRolesModal" class="futuristic-btn-cancel">Zrusit</v-btn>
          <v-btn color="primary" variant="flat" @click="saveUserRoles" :loading="savingRoles" class="futuristic-btn">Ulozit role</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup lang="ts">
import { UserRoles } from '@/types/enums';
import { ref, onMounted, computed } from 'vue';
import { useAuthStore, type UserInfo } from '@/stores/authStore';
import * as adminService from '@/services/adminService';
import Swal from 'sweetalert2';
import {
    mdiAccountGroup,
    mdiRefresh,
    mdiAccountEdit,
    mdiDelete,
    mdiDatabaseAlert,
    mdiDatabaseRefresh,
    mdiDatabaseRemove
} from '@mdi/js';

interface AdminUserDto extends UserInfo {
  // Pokud má AdminUserDto více polí než UserInfo, doplňte je zde
}

const authStore = useAuthStore();
const users = ref<AdminUserDto[]>([]);
const loadingUsers = ref(false);
const resettingDb = ref(false);
const deletingDb = ref(false);
const searchQuery = ref('');

const editRolesDialog = ref(false);
const editingUser = ref<AdminUserDto | null>(null);
const selectedRoles = ref<string[]>([]);
const savingRoles = ref(false);

const availableRoles = Object.values(UserRoles);

const userHeaders = ref([
  { title: 'ID Uzivatele', key: 'id', sortable: true, align: 'start' },
  { title: 'Prezdivka', key: 'nickname', sortable: true },
  { title: 'Email', key: 'email', sortable: true },
  { title: 'Role', key: 'roles', sortable: false, width: '250px' },
  { title: 'Akce', key: 'actions', sortable: false, align: 'end' },
]);

const fetchUsers = async () => {
  loadingUsers.value = true;
  console.log("Nacitam uzivatele...");
  users.value = await adminService.getAllUsers();
  console.log("Uzivatele nacteni: ", users.value.length);
  loadingUsers.value = false;
};

const filteredUsers = computed(() => {
    if (!searchQuery.value) {
        return users.value;
    }
    const lowerQuery = searchQuery.value.toLowerCase();
    return users.value.filter(user =>
        user.id.toLowerCase().includes(lowerQuery) ||
        user.nickname.toLowerCase().includes(lowerQuery) ||
        user.email.toLowerCase().includes(lowerQuery)
    );
});


const getRoleColor = (role: string) => {
  switch (role) {
    case UserRoles.Administrator: return 'error';
    case UserRoles.Spravce: return 'warning';
    case UserRoles.Uzivatel: return 'info';
    default: return 'grey';
  }
};

const openEditRolesModal = (user: AdminUserDto) => {
  editingUser.value = user;
  selectedRoles.value = [...user.roles];
  editRolesDialog.value = true;
};

const closeEditRolesModal = () => {
  editRolesDialog.value = false;
  editingUser.value = null;
  selectedRoles.value = [];
};

const saveUserRoles = async () => {
  if (!editingUser.value) return;
  savingRoles.value = true;
  const success = await adminService.updateUserRoles(editingUser.value.id, selectedRoles.value);
  if (success) {
    await fetchUsers(); // Refresh user list
    closeEditRolesModal();
  }
  savingRoles.value = false;
};

const confirmDeleteUser = async (userId: string) => {
  const success = await adminService.deleteUser(userId);
  if (success) {
    await fetchUsers(); // Refresh user list
  }
};

const handleResetDatabase = async () => {
  resettingDb.value = true;
  await adminService.resetDatabase();
  resettingDb.value = false;
  // Mozna pridat dalsi akce po resetu, napr. fetchUsers nebo notifikaci
};

const handleDeleteDatabase = async () => {
  deletingDb.value = true;
  await adminService.deleteDatabase();
  deletingDb.value = false;
  // Aplikace by se mela pravdepodobne presmerovat nebo uzivatel odhlasit
};

onMounted(() => {
  fetchUsers();
});
</script>

<style scoped>
.admin-view {
  background: radial-gradient(circle at 50% 0%, rgba(var(--v-theme-primary-rgb), 0.1), transparent 70%);
  min-height: 100vh;
}

.futuristic-card {
  background-color: rgba(var(--v-theme-surface-rgb), 0.9) !important;
  backdrop-filter: blur(10px);
  border: 1px solid rgba(var(--v-theme-border-color-rgb), 0.3) !important;
  box-shadow: 0 8px 32px 0 rgba(var(--v-theme-primary-rgb), 0.2) !important;
}

.card-header {
  border-bottom: 1px solid rgba(var(--v-theme-border-color-rgb), 0.2);
  padding-bottom: 0.75rem; /* 12px */
  margin-bottom: 1rem; /* 16px */
  color: var(--v-theme-primary);
}

.futuristic-table {
  background-color: transparent !important;
}

.futuristic-table :deep(thead tr th) {
  color: var(--v-theme-secondary) !important;
  font-weight: bold;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}
.futuristic-table :deep(tbody tr:hover) {
  background-color: rgba(var(--v-theme-primary-rgb), 0.05) !important;
}
.futuristic-table :deep(.v-data-table-footer) {
  border-top: 1px solid rgba(var(--v-theme-border-color-rgb), 0.1);
}

.futuristic-btn {
  transition: all 0.3s ease;
}
.futuristic-btn:hover {
    box-shadow: 0 0 15px rgba(var(--v-theme-primary-rgb), 0.7) !important;
    transform: translateY(-2px);
}
.futuristic-btn-cancel:hover {
    box-shadow: 0 0 15px rgba(var(--v-theme-text-secondary-rgb), 0.5) !important;
}

.text-primary {
  color: var(--v-theme-primary) !important;
}
.text-text-secondary {
  color: var(--v-theme-text-secondary) !important;
}

.futuristic-input :deep(.v-field__outline__start),
.futuristic-input :deep(.v-field__outline__end),
.futuristic-input :deep(.v-field__outline__notch)::before,
.futuristic-input :deep(.v-field__outline__notch)::after {
  border-color: rgba(var(--v-theme-primary-rgb), 0.4) !important;
}

.futuristic-input :deep(.v-field--active .v-field__outline__start),
.futuristic-input :deep(.v-field--active .v-field__outline__end),
.futuristic-input :deep(.v-field--active .v-field__outline__notch)::before,
.futuristic-input :deep(.v-field--active .v-field__outline__notch)::after {
  border-color: var(--v-theme-primary) !important;
  border-width: 2px;
}

.futuristic-alert {
  background-color: rgba(var(--v-theme-warning-rgb), 0.1) !important;
  border-color: var(--v-theme-warning) !important;
  color: var(--v-theme-warning) !important;
}
.futuristic-alert :deep(.v-alert__prepend .v-icon) {
  color: var(--v-theme-warning) !important;
}

.card-actions {
    background-color: rgba(var(--v-theme-surface-rgb), 0.7);
    border-top: 1px solid rgba(var(--v-theme-border-color-rgb),0.2);
}

</style>
