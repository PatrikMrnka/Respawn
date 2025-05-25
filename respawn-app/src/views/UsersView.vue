<template>
  <v-container class="futuristic-page">
    <v-card class="pa-4 futuristic-card">
      <v-card-title class="text-h4 font-oxanium mb-6"> Správa uživatelů </v-card-title>
      <v-progress-linear v-if="loading" indeterminate color="primary"></v-progress-linear>
      <v-alert v-if="error" type="error" prominent class="mb-4 font-inter">
        {{ error }}
      </v-alert>

      <v-responsive v-if="!loading && !error" min-height="300px">
        <v-table class="futuristic-table" fixed-header height="calc(100vh - 280px)">
          <thead>
            <tr>
              <th class="text-left font-exo2">ID</th>
              <th class="text-left font-exo2">Přezdívka</th>
              <th class="text-left font-exo2">Email</th>
              <th class="text-left font-exo2">Role</th>
              <th class="text-center font-exo2" style="width: 200px">Akce</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td class="font-roboto-mono" style="font-size: 0.85rem">{{ user.id }}</td>
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
                  class="futuristic-btn-icon mr-2"
                  title="Upravit role uživatele"
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
                  :disabled="isCurrentUser(user.id)"
                >
                </v-btn>
              </td>
            </tr>
            <tr v-if="users.length === 0">
              <td colspan="5" class="text-center font-inter pa-4">
                Nebyly nalezeni žádní uživatelé.
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-responsive>
    </v-card>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import Swal, { type SweetAlertOptions } from 'sweetalert2'
import { getAllUsers, updateUserRoles, deleteUser } from '@/services/adminService'
import { useAuthStore, type UserInfo } from '@/stores/authStore'
import { UserRoles } from '@/types/enums'
import { mdiPencil, mdiDelete } from '@mdi/js'

// State variables
const users = ref<UserInfo[]>([])  // Array to store all users
const loading = ref(true)          // Loading state indicator
const error = ref<string | null>(null)  // Error message if any
const authStore = useAuthStore()   // Auth store for current user info

/**
 * Fetches all users from the server
 */
const fetchUsers = async () => {
  loading.value = true
  error.value = null
  try {
    const fetchedUsers = await getAllUsers()
    if (fetchedUsers) {
      // Map fetched users to the required format
      users.value = fetchedUsers.map((u) => ({
        id: u.id,
        nickname: u.nickname,
        email: u.email,
        avatarUrl: u.avatarUrl,
        roles: u.roles || [],
      }))
    } else {
      error.value = 'Nepodařilo se načíst uživatele nebo nemáte oprávnění.'
    }
  } catch (err: any) {
    console.error('Chyba při načítání uživatelů:', err)
    error.value = err.message || 'Došlo k chybě při komunikaci se serverem.'
  } finally {
    loading.value = false
  }
}

// Fetch users when component is mounted
onMounted(fetchUsers)

/**
 * Checks if the user ID matches the current logged-in user
 * @param userId - User ID to check
 * @returns True if it's the current user
 */
const isCurrentUser = (userId: string) => {
  return authStore.user?.id === userId
}

/**
 * Gets the color for a role badge based on the role type
 * @param role - Role name
 * @returns Color name for the badge
 */
const getRoleColor = (role: string) => {
  if (role === UserRoles.Administrator) return 'primary'
  if (role === UserRoles.Spravce) return 'secondary'
  return 'grey-darken-1'
}

/**
 * Returns base SweetAlert2 options with futuristic styling
 */
const getFuturisticSwalBaseOptions = (): SweetAlertOptions => ({
  background: '#1A2033',
  color: '#E0E0E0',
  confirmButtonColor: '#00E0FF',
  cancelButtonColor: '#FF5252',
  customClass: {
    popup: 'futuristic-swal-popup',
    title: 'futuristic-swal-title font-oxanium',
    htmlContainer: 'futuristic-swal-html-container font-inter',
    input: 'futuristic-swal-input',
    confirmButton: 'futuristic-swal-confirm-button futuristic-btn',
    cancelButton: 'futuristic-swal-cancel-button futuristic-btn',
    actions: 'futuristic-swal-actions',
    validationMessage: 'futuristic-swal-validation-message font-inter',
  },
  buttonsStyling: false,
  heightAuto: false,
})

/**
 * Opens a modal dialog to edit user roles
 * @param user - User object to edit
 */
const openEditUserModal = async (user: UserInfo) => {
  const availableRoles = Object.values(UserRoles)

  // Create checkboxes for each available role
  const rolesCheckboxesHtml = availableRoles
    .map(
      (role) => `
    <label class="swal-checkbox-label font-inter">
      <input
        type="checkbox"
        id="swal-role-${role.replace(/\s+/g, '-')}"
        class="swal2-checkbox futuristic-swal-checkbox"
        value="${role}"
        ${user.roles.includes(role) ? 'checked' : ''}
      >
      ${role}
    </label>
  `,
    )
    .join('')

  const { value: selectedRolesArray, isConfirmed } = await Swal.fire({
    ...getFuturisticSwalBaseOptions(),
    title: `Upravit role pro ${user.nickname}`,
    html: `
      <div class="swal-form-container">
        <p class="swal-label font-inter mb-2">Vyberte role pro uživatele:</p>
        ${rolesCheckboxesHtml}
      </div>
    `,
    focusConfirm: false,
    showCancelButton: true,
    confirmButtonText: 'Uložit změny',
    cancelButtonText: 'Zrušit',
    showLoaderOnConfirm: true,
    preConfirm: () => {
      // Collect all checked roles
      const newRoles: string[] = []
      availableRoles.forEach((role) => {
        const checkbox = document.getElementById(
          `swal-role-${role.replace(/\s+/g, '-')}`,
        ) as HTMLInputElement
        if (checkbox && checkbox.checked) {
          newRoles.push(role)
        }
      })
      // Validate that at least one role is selected
      if (newRoles.length === 0) {
        Swal.showValidationMessage('Uživatel musí mít alespoň jednu roli.')
        return false
      }
      return newRoles
    },
  })

  // Update user roles if confirmed
  if (isConfirmed && selectedRolesArray) {
    const success = await updateUserRoles(user.id, selectedRolesArray)
    if (success) {
      await fetchUsers()
    }
  }
}

/**
 * Shows confirmation dialog and deletes user if confirmed
 * @param user - User to delete
 */
const confirmDeleteUser = async (user: UserInfo) => {
  // Prevent deleting current user
  if (isCurrentUser(user.id)) {
    Swal.fire({
      ...getFuturisticSwalBaseOptions(),
      icon: 'error',
      titleText: 'Chyba',
      text: 'Nemůžete smazat sám sebe.',
    })
    return
  }

  Swal.fire({
    ...getFuturisticSwalBaseOptions(),
    titleText: 'Opravdu smazat uživatele?',
    html: `<div class="font-inter">Opravdu si přejete smazat uživatele <strong>${user.nickname}</strong> (ID: ${user.id})?<br>Tato akce je nevratná!</div>`,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Ano, smazat',
    cancelButtonText: 'Zrušit',
    showLoaderOnConfirm: true,
    preConfirm: async () => {
      try {
        // Attempt to delete the user
        const success = await deleteUser(user.id)
        if (!success) {
          Swal.showValidationMessage('Nepodařilo se smazat uživatele. Zkuste to prosím znovu.')
        }
        return success
      } catch (apiError: any) {
        console.error('Chyba při mazání uživatele:', apiError)
        Swal.showValidationMessage(`Došlo k chybě: ${apiError.message || 'Neznámá chyba'}`)
        return false
      }
    },
    allowOutsideClick: () => !Swal.isLoading(),
  }).then((result) => {
    // Refresh user list if deletion was successful
    if (result.isConfirmed && result.value) {
      fetchUsers()
    }
  })
}
</script>

<style scoped>
.futuristic-page {
  padding-top: 20px;
}

.futuristic-card {
  background-color: var(--v-theme-surface);
  border: 1px solid rgba(var(--v-theme-primary-rgb), 0.2);
  box-shadow: 0 0 15px rgba(var(--v-theme-primary-rgb), 0.1);
}

.futuristic-table thead th {
  background-color: rgba(var(--v-theme-primary-rgb), 0.1) !important;
  color: var(--v-theme-primary) !important;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.futuristic-table tbody tr:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.05) !important;
}

.futuristic-table td,
.futuristic-table th {
  border-bottom: 1px solid rgba(var(--v-theme-text-primary-rgb), 0.1) !important;
}

.futuristic-btn-icon {
  border: 1px solid transparent;
  transition: all 0.2s ease-in-out;
}
.futuristic-btn-icon:hover {
  border-color: currentColor;
  transform: scale(1.1);
}

.swal-form-container {
  text-align: left;
}
.swal-checkbox-label {
  display: flex;
  align-items: center;
  margin: 0.75rem 0;
  padding: 0.6rem 0.8rem;
  border-radius: 6px;
  transition: background-color 0.2s ease-in-out;
  cursor: pointer;
}
.swal-checkbox-label:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.1);
}
.swal-checkbox-label input[type='checkbox'] {
  margin-right: 0.85rem;
  transform: scale(1.1);
  accent-color: var(--v-theme-primary);
}

.v-chip {
  white-space: nowrap;
}
</style>
