<template>
  <v-container class="futuristic-page">
    <v-card class="pa-md-6 pa-4 futuristic-card">
      <v-card-title class="text-h4 font-oxanium mb-6">
        Administrační Panel
      </v-card-title>

      <v-tabs v-model="activeTab" color="primary" grow class="mb-6 futuristic-tabs">
        <v-tab value="general">Obecné & Uživatelé</v-tab>
        <v-tab value="docker" v-if="isSuperAdmin">Docker Správa</v-tab>
      </v-tabs>

      <v-window v-model="activeTab">
        <v-window-item value="general">
          <v-card-text>
            <p class="font-inter mb-4">Vítejte v administraci. Zde můžete spravovat různé aspekty aplikace.</p>
            
            <div v-if="isSuperAdmin" class="mb-8">
                <h3 class="text-h6 font-exo2 mb-3">Správa Databáze</h3>
                <v-btn
                  color="error"
                  @click="confirmResetDatabase"
                  class="futuristic-btn error-btn"
                  :loading="resettingDatabase"
                >
                  <v-icon left>{{ mdiDatabaseRefresh }}</v-icon>
                  Resetovat Databázi (Nebezpečné!)
                </v-btn>
            </div>
            <v-divider v-if="isSuperAdmin" class="my-6"></v-divider>

            <div>
                <h3 class="text-h6 font-exo2 mb-4">Správa Uživatelů</h3>
                <v-progress-linear v-if="usersLoading" indeterminate color="secondary" class="mb-3"></v-progress-linear>
                <v-alert v-if="usersError" type="error" prominent class="mb-4 font-inter">
                    {{ usersError }}
                </v-alert>

                <v-responsive v-if="!usersLoading && !usersError" min-height="200px">
                    <v-table class="futuristic-table" fixed-header density="compact">
                    <thead>
                        <tr>
                        <th class="text-left font-exo2">ID</th>
                        <th class="text-left font-exo2">Přezdívka</th>
                        <th class="text-left font-exo2">Email</th>
                        <th class="text-left font-exo2">Role</th>
                        <th class="text-center font-exo2" style="width: 150px;">Akce</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="user in userList" :key="user.id">
                        <td class="font-roboto-mono" style="font-size: 0.8rem;">{{ user.id.substring(0,8) }}...</td>
                        <td class="font-inter">{{ user.nickname }}</td>
                        <td class="font-inter">{{ user.email }}</td>
                        <td class="font-inter">
                            <v-chip
                            v-for="role in user.roles"
                            :key="role"
                            label
                            size="small"
                            class="mr-1"
                            :color="getRoleColor(role)"
                            >
                            {{ role }}
                            </v-chip>
                        </td>
                        <td class="text-center">
                            <v-btn
                            :icon="mdiPencil"
                            variant="text"
                            size="small"
                            color="warning"
                            @click="openEditUserModal(user)"
                            class="futuristic-btn-icon mr-1"
                            title="Upravit role"
                            :disabled="!canEditUser(user)"
                            >
                            </v-btn>
                            <v-btn
                            :icon="mdiDelete"
                            variant="text"
                            size="small"
                            color="error"
                            @click="confirmDeleteUser(user)"
                            class="futuristic-btn-icon"
                            title="Smazat uživatele"
                            :disabled="isCurrentUser(user.id) || !canDeleteUser(user)"
                            >
                            </v-btn>
                        </td>
                        </tr>
                        <tr v-if="userList.length === 0">
                        <td colspan="5" class="text-center font-inter pa-4">
                            Nebyly nalezeni žádní uživatelé.
                        </td>
                        </tr>
                    </tbody>
                    </v-table>
                </v-responsive>
            </div>

          </v-card-text>
        </v-window-item>

        <v-window-item value="docker" v-if="isSuperAdmin">
          <v-card-text>
            <h2 class="text-h5 font-exo2 mb-4">Správa Dockeru</h2>
            <v-tabs v-model="dockerSubTab" color="secondary" grow class="mb-4 futuristic-tabs-secondary">
              <v-tab value="containers">Kontejnery</v-tab>
              <v-tab value="images">Images</v-tab>
              <v-tab value="volumes">Volumes</v-tab>
            </v-tabs>

            <v-window v-model="dockerSubTab">
              <v-window-item value="containers">
                <v-btn @click="fetchContainers(true)" :loading="dockerLoading.containers" small class="mb-3 futuristic-btn-secondary" :prepend-icon="mdiRefresh">Obnovit</v-btn>
                <v-table class="futuristic-table" density="compact">
                  <thead><tr><th>ID</th><th>Název</th><th>Image</th><th>Stav</th><th>Status</th><th>Akce</th></tr></thead>
                  <tbody>
                    <tr v-for="item in dockerData.containers" :key="item.id">
                      <td>{{ item.id.substring(0, 12) }}</td><td>{{ item.names.join(', ') }}</td>
                      <td>{{ item.image }}</td>
                      <td><v-chip :color="getDockerStateColor(item.state)" size="small">{{ item.state }}</v-chip></td>
                      <td>{{ item.status }}</td>
                      <td>
                        <v-btn :icon="item.state === 'running' ? mdiStop : mdiPlay" :color="item.state === 'running' ? 'warning' : 'success'" variant="text" size="small" @click="item.state === 'running' ? stopDockerContainer(item.id) : startDockerContainer(item.id)" :loading="actionLoading[`container_toggle_${item.id}`]" :title="item.state === 'running' ? 'Zastavit' : 'Spustit'"></v-btn>
                        <v-btn :icon="mdiFileDocumentOutline" color="info" variant="text" size="small" @click="viewContainerLogs(item.id)" :loading="actionLoading[`container_logs_${item.id}`]" title="Logy"></v-btn>
                        <v-btn :icon="mdiDelete" color="error" variant="text" size="small" @click="deleteDockerContainer(item.id)" :loading="actionLoading[`container_delete_${item.id}`]" title="Smazat"></v-btn>
                      </td></tr>
                     <tr v-if="!dockerLoading.containers && dockerData.containers.length === 0"><td colspan="6" class="text-center">Žádné kontejnery.</td></tr>
                  </tbody></v-table>
              </v-window-item>
              <v-window-item value="images">
                <v-btn @click="fetchImages(false)" :loading="dockerLoading.images" small class="mb-3 futuristic-btn-secondary" :prepend-icon="mdiRefresh">Obnovit</v-btn>
                <v-table class="futuristic-table" density="compact">
                  <thead><tr><th>ID</th><th>Tagy</th><th>Velikost</th><th>Vytvořeno</th><th>Akce</th></tr></thead>
                  <tbody>
                    <tr v-for="item in dockerData.images" :key="item.id">
                      <td>{{ item.id }}</td><td>{{ item.repoTags.join(', ') }}</td>
                      <td>{{ formatBytes(item.size) }}</td><td>{{ new Date(item.created).toLocaleString() }}</td>
                      <td><v-btn :icon="mdiDelete" color="error" variant="text" size="small" @click="deleteDockerImage(item.fullId)" :loading="actionLoading[`image_delete_${item.id}`]" title="Smazat"></v-btn></td></tr>
                    <tr v-if="!dockerLoading.images && dockerData.images.length === 0"><td colspan="5" class="text-center">Žádné images.</td></tr>
                  </tbody></v-table>
              </v-window-item>
              <v-window-item value="volumes">
                <v-btn @click="fetchVolumes" :loading="dockerLoading.volumes" small class="mb-3 futuristic-btn-secondary" :prepend-icon="mdiRefresh">Obnovit</v-btn>
                <v-table class="futuristic-table" density="compact">
                  <thead><tr><th>Název</th><th>Driver</th><th>Vytvořeno</th><th>Akce</th></tr></thead>
                  <tbody>
                    <tr v-for="item in dockerData.volumes" :key="item.name">
                      <td>{{ item.name }}</td><td>{{ item.driver }}</td>
                      <td>{{ new Date(item.createdAt).toLocaleString() }}</td>
                      <td><v-btn :icon="mdiDelete" color="error" variant="text" size="small" @click="deleteDockerVolume(item.name)" :loading="actionLoading[`volume_delete_${item.name}`]" title="Smazat"></v-btn></td></tr>
                    <tr v-if="!dockerLoading.volumes && dockerData.volumes.length === 0"><td colspan="4" class="text-center">Žádné volumes.</td></tr>
                  </tbody></v-table>
              </v-window-item>
            </v-window>
          </v-card-text>
        </v-window-item>
      </v-window>
    </v-card>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, reactive, watch } from 'vue';
import Swal, { type SweetAlertOptions } from 'sweetalert2';
import { useAuthStore, type UserInfo } from '@/stores/authStore';
import { UserRoles } from '@/types/enums';
import { resetDatabase, getAllUsers, updateUserRoles, deleteUser as deleteUserService } from '@/services/adminService';
import {
    getVolumes, deleteVolume, getContainers, startContainer, stopContainer, deleteContainer, getContainerLogs, getImages, deleteImage
} from '@/services/dockerAdminService';
import type { DockerVolumeDto, DockerContainerDto, DockerImageDto } from '@/types/dockerAdmin';
import { mdiDatabaseRefresh, mdiRefresh, mdiPlay, mdiStop, mdiFileDocumentOutline, mdiDelete, mdiPencil } from '@mdi/js';

const authStore = useAuthStore();
const activeTab = ref('general');
const dockerSubTab = ref('containers');
const resettingDatabase = ref(false);

const userList = ref<UserInfo[]>([]);
const usersLoading = ref(true);
const usersError = ref<string | null>(null);

const dockerData = reactive<{
  volumes: DockerVolumeDto[],
  containers: DockerContainerDto[],
  images: DockerImageDto[]
}>({ volumes: [], containers: [], images: [] });

const dockerLoading = reactive({ volumes: false, containers: false, images: false });
const actionLoading = reactive<Record<string, boolean>>({});

const isSuperAdmin = computed(() => authStore.user?.roles?.includes(UserRoles.Administrator));

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

const confirmResetDatabase = () => {
  Swal.fire({
    ...getFuturisticSwalBaseOptions('Opravdu resetovat databázi?'),
    html: `<div class="font-inter">Tato akce je <strong>extrémně nebezpečná</strong> a smaže všechna data v databázi!<br>Jste si absolutně jisti?</div>`,
    icon: 'error',
    showCancelButton: true,
    confirmButtonText: 'Ano, resetovat!',
    cancelButtonText: 'Zrušit',
    confirmButtonColor: '#d33',
    showLoaderOnConfirm: true,
    preConfirm: async () => {
      resettingDatabase.value = true;
      try {
        const success = await resetDatabase();
        if (!success) {
          Swal.showValidationMessage('Reset databáze selhal.');
        }
        return success;
      } catch (err: any) {
        Swal.showValidationMessage(`Chyba: ${err.message}`);
        return false;
      } finally {
        resettingDatabase.value = false;
      }
    }
  }).then((result) => {
    if (result.isConfirmed && result.value) {
      Swal.fire({...getFuturisticSwalBaseOptions('Resetováno!'), text: 'Databáze byla (nebo se pokusila být) resetována.', icon: 'success'});
    }
  });
};

const fetchAdminUsers = async () => {
  if (!isSuperAdmin.value) {
    usersLoading.value = false;
    return;
  }
  usersLoading.value = true;
  usersError.value = null;
  try {
    const fetchedUsers = await getAllUsers();
    if (fetchedUsers) {
      userList.value = fetchedUsers;
    } else {
      usersError.value = 'Nepodařilo se načíst uživatele nebo nemáte oprávnění.';
    }
  } catch (err: any) {
    usersError.value = err.message || 'Došlo k chybě při komunikaci se serverem.';
  } finally {
    usersLoading.value = false;
  }
};

const isCurrentUser = (userId: string) => authStore.user?.id === userId;

const getRoleColor = (role: string) => {
  if (role === UserRoles.Administrator) return 'primary';
  if (role === UserRoles.Spravce) return 'secondary';
  return 'grey-darken-1';
};

const canEditUser = (user: UserInfo): boolean => {
    if (!authStore.user) return false;
    if (isSuperAdmin.value) {
        return true; 
    }
    return false;
};

const canDeleteUser = (user: UserInfo): boolean => {
    if (!authStore.user) return false;
    if (isCurrentUser(user.id)) return false;
    if (isSuperAdmin.value) {
        return true;
    }
    return false;
};


const openEditUserModal = async (user: UserInfo) => {
  const availableRoles = Object.values(UserRoles);
  const rolesCheckboxesHtml = availableRoles.map(role => `
    <label class="swal-checkbox-label font-inter">
      <input type="checkbox" id="swal-role-${role.replace(/\s+/g, '-')}" class="swal2-checkbox futuristic-swal-checkbox" value="${role}" ${user.roles.includes(role) ? 'checked' : ''}>
      ${role}
    </label>
  `).join('');

  const { value: selectedRolesArray, isConfirmed } = await Swal.fire({
    ...getFuturisticSwalBaseOptions(`Upravit role pro ${user.nickname}`),
    html: `<div class="swal-form-container"><p class="swal-label font-inter mb-2">Vyberte role:</p>${rolesCheckboxesHtml}</div>`,
    customClass: { popup: 'futuristic-swal-popup large-swal', htmlContainer: 'futuristic-swal-html-container font-inter swal-form-container-custom-padding' },
    focusConfirm: false, showCancelButton: true, confirmButtonText: 'Uložit změny', cancelButtonText: 'Zrušit', showLoaderOnConfirm: true,
    preConfirm: () => {
      const newRoles: string[] = [];
      availableRoles.forEach(role => {
        const checkbox = document.getElementById(`swal-role-${role.replace(/\s+/g, '-')}`) as HTMLInputElement;
        if (checkbox && checkbox.checked) newRoles.push(role);
      });
      if (newRoles.length === 0) { Swal.showValidationMessage('Uživatel musí mít alespoň jednu roli.'); return false; }
      if (user.roles.includes(UserRoles.Administrator) && !newRoles.includes(UserRoles.Administrator)) {
        const adminUsers = userList.value.filter(u => u.roles.includes(UserRoles.Administrator));
        if (adminUsers.length === 1 && adminUsers[0].id === user.id) {
          Swal.showValidationMessage('Nemůžete odebrat roli poslednímu administrátorovi.');
          return false;
        }
      }
      return newRoles;
    },
  });

  if (isConfirmed && selectedRolesArray) {
    const success = await updateUserRoles(user.id, selectedRolesArray);
    if (success) fetchAdminUsers();
  }
};

const confirmDeleteUser = async (user: UserInfo) => {
  if (isCurrentUser(user.id)) { return; }
  Swal.fire({
    ...getFuturisticSwalBaseOptions('Opravdu smazat uživatele?'),
    html: `<div class="font-inter">Opravdu si přejete smazat uživatele <strong>${user.nickname}</strong>?<br>Tato akce je nevratná!</div>`,
    icon: 'warning', showCancelButton: true, confirmButtonText: 'Ano, smazat', cancelButtonText: 'Zrušit', showLoaderOnConfirm: true,
    preConfirm: async () => {
      try {
        const success = await deleteUserService(user.id);
        if (!success) Swal.showValidationMessage('Nepodařilo se smazat uživatele.');
        return success;
      } catch (apiError: any) { Swal.showValidationMessage(`Došlo k chybě: ${apiError.message || 'Neznámá chyba'}`); return false; }
    },
  }).then((result) => { if (result.isConfirmed && result.value) fetchAdminUsers(); });
};

const fetchVolumes = async () => {
  if (!isSuperAdmin.value) return;
  dockerLoading.volumes = true;
  try {
    dockerData.volumes = await getVolumes();
  } catch (e: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: e.message, icon: 'error'}); }
  finally { dockerLoading.volumes = false; }
};

const deleteDockerVolume = async (name: string) => {
  Swal.fire({
    ...getFuturisticSwalBaseOptions(`Smazat volume ${name}?`),
    text: "Tato akce je nevratná!",
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Ano, smazat',
    cancelButtonText: 'Zrušit',
    showLoaderOnConfirm: true,
    preConfirm: async () => {
      actionLoading[`volume_delete_${name}`] = true;
      return deleteVolume(name, true).finally(() => actionLoading[`volume_delete_${name}`] = false);
    }
  }).then(result => {
    if (result.isConfirmed && result.value) {
      Swal.fire({...getFuturisticSwalBaseOptions('Smazáno'), text: `Volume ${name} bylo smazáno.`, icon: 'success'});
      fetchVolumes();
    } else if (result.isConfirmed && !result.value) { /* Chyba byla již zobrazena */ }
  });
};

const fetchContainers = async (all: boolean = true) => {
  if (!isSuperAdmin.value) return;
  dockerLoading.containers = true;
  try {
    dockerData.containers = await getContainers(all);
  } catch (e: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: e.message, icon: 'error'}); }
  finally { dockerLoading.containers = false; }
};

const startDockerContainer = async (id: string) => {
  actionLoading[`container_toggle_${id}`] = true;
  try {
    if (await startContainer(id)) {
      Swal.fire({...getFuturisticSwalBaseOptions('Úspěch'), text: 'Kontejner spuštěn.', icon: 'success', timer: 1500, showConfirmButton: false});
      fetchContainers(); // Refresh listu
    }
  } catch (e:any) { /* Chyba je již zobrazena v service */ }
  finally { actionLoading[`container_toggle_${id}`] = false; }
};

const stopDockerContainer = async (id: string) => {
  actionLoading[`container_toggle_${id}`] = true;
  try {
    if (await stopContainer(id)) {
      Swal.fire({...getFuturisticSwalBaseOptions('Úspěch'), text: 'Kontejner zastaven.', icon: 'success', timer: 1500, showConfirmButton: false});
      fetchContainers();
    }
  } catch (e:any) { /* Chyba je již zobrazena v service */ }
  finally { actionLoading[`container_toggle_${id}`] = false; }
};

const deleteDockerContainer = (id: string) => {
   Swal.fire({
    ...getFuturisticSwalBaseOptions(`Smazat kontejner ${id.substring(0,12)}?`),
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Smazat',
    cancelButtonText: 'Zrušit',
    showLoaderOnConfirm: true,
    showLoaderOnDeny: true,
    preConfirm: async (resultFromSwal) => { // resultFromSwal je true pro confirm, false pro deny, undefined pro escape
      actionLoading[`container_delete_${id}`] = true;
      let removeVolumesFlag = true;
      return deleteContainer(id, removeVolumesFlag).finally(() => actionLoading[`container_delete_${id}`] = false);
    },
  }).then(result => {
    if ((result.isConfirmed || result.isDenied) && result.value) { // result.value je výsledek z deleteContainer
      Swal.fire({...getFuturisticSwalBaseOptions('Smazáno'), text: `Kontejner ${id.substring(0,12)} byl smazán.`, icon: 'success'});
      fetchContainers();
    } else if ((result.isConfirmed || result.isDenied) && !result.value) {
      // Chyba byla již zobrazena v deleteContainer
    }
  });
};

const viewContainerLogs = async (id: string) => {
  actionLoading[`container_logs_${id}`] = true;
  try {
    const logs = await getContainerLogs(id, 500);
    Swal.fire({
        ...getFuturisticSwalBaseOptions(`Logy kontejneru ${id.substring(0,12)}`),
        html: `<pre style="text-align: left;" class="server-logs-pre">${logs.replace(/</g, "&lt;").replace(/>/g, "&gt;")}</pre>`,
        width: '90vw',
        customClass: { 
            popup: 'futuristic-swal-popup logs-swal',
            htmlContainer: 'futuristic-swal-html-container font-inter'
        },
        confirmButtonText: 'Zavřít'
    });
  } catch (e: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: e.message, icon: 'error'}); }
  finally { actionLoading[`container_logs_${id}`] = false; }
};

const fetchImages = async (all: boolean = false) => {
  if (!isSuperAdmin.value) return;
  dockerLoading.images = true;
  try {
    dockerData.images = await getImages(all);
  } catch (e: any) { Swal.fire({...getFuturisticSwalBaseOptions('Chyba'), text: e.message, icon: 'error'}); }
  finally { dockerLoading.images = false; }
};

const deleteDockerImage = (id: string) => {
  Swal.fire({
    ...getFuturisticSwalBaseOptions(`Smazat image ${id.substring(0,12)}?`),
    text: "Tato akce může být nevratná a ovlivnit běžící kontejnery, pokud není použito 'force'.",
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Ano, smazat',
    cancelButtonText: 'Zrušit',
    showLoaderOnConfirm: true,
    preConfirm: async () => {
      actionLoading[`image_delete_${id}`] = true;
      return deleteImage(id, false, false).finally(() => actionLoading[`image_delete_${id}`] = false);
    }
  }).then(result => {
    if (result.isConfirmed && result.value) {
      Swal.fire({...getFuturisticSwalBaseOptions('Smazáno'), text: `Image ${id.substring(0,12)} byla smazána.`, icon: 'success'});
      fetchImages();
    } else if (result.isConfirmed && !result.value) { /* Chyba byla již zobrazena */ }
  });
};

const formatBytes = (bytes: number, decimals = 2) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
};
const getDockerStateColor = (state: string) => {
    if (state === 'running') return 'success';
    if (state === 'exited') return 'error';
    if (state === 'created') return 'info';
    if (state === 'restarting') return 'warning';
    return 'grey';
};

onMounted(() => {
  if (isSuperAdmin.value) {
    fetchAdminUsers();
    if (activeTab.value === 'docker') {
        if(dockerSubTab.value === 'containers') fetchContainers();
        else if (dockerSubTab.value === 'images') fetchImages();
        else if (dockerSubTab.value === 'volumes') fetchVolumes();
    }
  }
});

watch(activeTab, (newTab) => {
    if (newTab === 'general' && isSuperAdmin.value && userList.value.length === 0) {
        fetchAdminUsers();
    } else if (newTab === 'docker' && isSuperAdmin.value) {
        if(dockerSubTab.value === 'containers' && dockerData.containers.length === 0) fetchContainers();
        else if (dockerSubTab.value === 'images' && dockerData.images.length === 0) fetchImages();
        else if (dockerSubTab.value === 'volumes' && dockerData.volumes.length === 0) fetchVolumes();
    }
});
watch(dockerSubTab, (newSubTab) => {
    if (activeTab.value === 'docker' && isSuperAdmin.value) {
        if(newSubTab === 'containers' && dockerData.containers.length === 0) fetchContainers();
        else if (newSubTab === 'images' && dockerData.images.length === 0) fetchImages();
        else if (newSubTab === 'volumes' && dockerData.volumes.length === 0) fetchVolumes();
    }
});

</script>

<style scoped>
.futuristic-page { max-width: 1200px; margin: auto; }
.futuristic-card { background-color: var(--v-theme-surface); border: 1px solid rgba(var(--v-theme-primary-rgb), 0.2); }
.page-title { color: var(--v-theme-primary); }

.futuristic-tabs .v-tab { font-family: var(--font-family-headings-exo2); text-transform: uppercase; }
.futuristic-tabs-secondary .v-tab { font-family: var(--font-family-inter); font-size: 0.9rem; }

.futuristic-table { background-color: transparent; }
.futuristic-table th { font-family: var(--font-family-headings-exo2); color: var(--v-theme-primary) !important; text-transform: uppercase; font-weight: bold; white-space: nowrap; }
.futuristic-table td { font-family: var(--font-family-inter); white-space: nowrap; }
.futuristic-btn-icon { border: 1px solid transparent; transition: all 0.2s ease-in-out; }
.futuristic-btn-icon:hover { border-color: currentColor; transform: scale(1.1); }

:deep(.swal-form-container) { text-align: center; max-height: 70vh; overflow-y: auto; padding: 0 1em 1em 1em; }
:deep(.swal-form-container .swal-label) { display: block; color: var(--v-theme-text-secondary); margin-bottom: .25em; margin-top: .75em; font-size: 0.9rem; }
:deep(.swal-form-container .futuristic-swal-input) { width: 100%; box-sizing: border-box; }
:deep(.large-swal) { width: 600px !important; }

/* Odstraněny :deep(.extra-large-swal) a :deep(.very-extra-large-swal), šířka se nastavuje přímo v JS */

:deep(.logs-swal .swal2-html-container) { max-width: 100%; }
:deep(.server-logs-pre) {
    text-align: left !important;
     max-height: 70vh; overflow-y: auto;
    background-color: #010409; color: #c9d1d9;
    padding: 15px; border-radius: 6px; font-size: 0.8rem;
    white-space: pre-wrap; word-break: break-all;
    border: 1px solid rgba(var(--v-theme-primary-rgb), 0.3);
}
:deep(.swal-checkbox-label) { display: flex; align-items: center; cursor: pointer; padding: 0.5rem 0.2rem; border-radius: 4px; transition: background-color 0.2s ease-in-out; }
:deep(.swal-checkbox-label:hover) { background-color: rgba(var(--v-theme-primary-rgb), 0.05); }
:deep(.swal-checkbox-label input[type="checkbox"]) { margin-right: 0.75rem; transform: scale(1.1); accent-color: var(--v-theme-primary); }
</style>
