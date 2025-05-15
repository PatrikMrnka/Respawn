<template>
  <v-navigation-drawer
    :model-value="drawerVisible"
    @update:model-value="$emit('update:drawerVisible', $event)"
    app
    temporary
    color="surface"
    class="futuristic-drawer"
  >
    <v-list nav dense>
      <v-list-item
        v-for="(item, i) in menuItems"
        :key="i"
        @click="$emit('navigate', item.path)"
        class="menu-item"
        :active="isActiveRoute(item.path)"
      >
        <template v-slot:prepend>
          <v-icon :icon="item.icon" :color="iconColor(item.path)"></v-icon>
        </template>
        <v-list-item-title
          :class="isActiveRoute(item.path) ? 'text-primary' : 'text-text-primary'"
          >{{ item.title }}</v-list-item-title
        >
      </v-list-item>
    </v-list>
  </v-navigation-drawer>
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'

// Definice typu pro položku menu
interface MenuItem {
  title: string
  icon: string
  path: string
  requiresAuth?: boolean
}

const props = defineProps<{
  drawerVisible: boolean
  menuItems: MenuItem[]
}>()

defineEmits(['update:drawerVisible', 'navigate'])

const route = useRoute()

const isActiveRoute = (path: string) => {
  return route.path === path
}

const iconColor = (path: string) => {
  return isActiveRoute(path) ? 'primary' : 'text-primary'
}
</script>

<style scoped>
.futuristic-drawer {
  border-right: 1px solid rgba(var(--v-theme-primary-rgb), 0.2) !important;
}

/* Navigation drawer menu item styling */
.menu-item .v-list-item-title {
  font-family: 'Exo 2', sans-serif;
  transition: color 0.2s ease-in-out;
}

.menu-item {
  border-left: 3px solid transparent;
  transition: all 0.2s ease-in-out;
  cursor: pointer;
}

/* Barva textu a ikon pro neaktivní položky */
.menu-item:not(.v-list-item--active) .v-list-item-title {
  color: var(--v-theme-text-primary) !important;
}
.menu-item:not(.v-list-item--active) .v-icon {
  color: var(--v-theme-text-primary) !important;
}

/* Hover stav pro neaktivní položky */
.menu-item:not(.v-list-item--active):hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.08);
  border-left-color: var(--v-theme-primary);
}
.menu-item:not(.v-list-item--active):hover .v-list-item-title,
.menu-item:not(.v-list-item--active):hover .v-icon {
  color: var(--v-theme-primary) !important;
}

/* Aktivní stav položky */
.menu-item.v-list-item--active {
  border-left-color: var(--v-theme-primary) !important;
  background-color: rgba(var(--v-theme-primary-rgb), 0.12) !important;
}
.menu-item.v-list-item--active .v-list-item-title,
.menu-item.v-list-item--active .v-icon {
  color: var(--v-theme-primary) !important;
}
</style>
