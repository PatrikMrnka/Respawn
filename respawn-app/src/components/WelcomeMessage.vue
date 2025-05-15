<template>
  <v-row class="text-center mt-16">
    <v-col cols="12">
      <h1 class="font-exo2 page-title text-primary mb-6">Vítejte v Respawn LAN Organizer!</h1>
      <p class="font-inter text-text-secondary mb-8" style="font-size: 1.2rem">
        Přihlaste se nebo zaregistrujte pro plný přístup k funkcím aplikace.
      </p>
      <div>
        <v-btn
          class="futuristic-btn-filled mx-2"
          color="primary"
          size="large"
          @click="$emit('register')"
        >
          <v-icon left class="mr-2">{{ mdiAccountPlus }}</v-icon>
          Registrovat
        </v-btn>
        <v-btn class="futuristic-btn mx-2" variant="outlined" size="large" @click="$emit('login')">
          <v-icon left class="mr-1">{{ mdiLogin }}</v-icon>
          Přihlásit se
        </v-btn>
      </div>
    </v-col>
  </v-row>
</template>

<script lang="ts" setup>
import { mdiLogin, mdiAccountPlus } from '@mdi/js'
defineEmits(['login', 'register'])
</script>

<style scoped>
.page-title {
  font-size: 2.5rem;
  font-weight: 700;
  letter-spacing: 1px;
  text-shadow: 0 0 8px var(--v-theme-glow-color);
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
.futuristic-btn-filled {
  font-family: 'Exo 2', sans-serif;
  box-shadow: 0 0 8px 0px transparent;
}
.futuristic-btn-filled:hover {
  box-shadow: 0 0 12px 2px var(--v-theme-glow-color);
  transform: translateY(-2px);
}
</style>
