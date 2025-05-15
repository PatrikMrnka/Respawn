<template>
  <v-footer app color="surface" class="futuristic-footer" elevation="4">
    <span class="font-inter text-text-secondary"
      >&copy; {{ new Date().getFullYear() }} Respawn LAN Organizer - Všechna práva vyhrazena.</span
    >
  </v-footer>
</template>

<script lang="ts" setup></script>

<style scoped>
.futuristic-footer {
  border-top: 1px solid rgba(var(--v-theme-primary-rgb), 0.2) !important;
  font-size: 0.875rem;
  display: flex;
  justify-content: center;
  padding: 0.5rem 1rem !important;
}
</style>
