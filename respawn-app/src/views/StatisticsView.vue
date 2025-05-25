<script setup lang="ts">
import WelcomeMessage from '@/components/WelcomeMessage.vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
</script>

<template>
  <main>
    <WelcomeMessage v-if="!authStore.isLoggedIn" />
  </main>
</template>
