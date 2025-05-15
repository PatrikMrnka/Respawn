<!-- prozatimni -->

<template>
  <v-row class="mt-8">
    <v-col cols="12">
      <h1 class="font-exo2 page-title text-primary mb-6">Přehled</h1>
    </v-col>
    <v-col cols="12" md="6" lg="4">
      <v-card class="futuristic-card" shaped elevation="8">
        <div class="hexagon-decoration top-left"></div>
        <div class="hexagon-decoration bottom-right"></div>
        <v-card-title class="font-oxanium card-title">
          <v-icon left class="mr-2">{{ mdiServer }}</v-icon>
          Stav Serverů
        </v-card-title>
        <v-card-text class="font-inter card-text">
          <p>CS 1.6 Server: <span class="text-success">Online</span> - Hráčů: 12/16</p>
          <p>Minecraft Server: <span class="text-warning">Restartuje se</span></p>
          <p class="font-roboto-mono mt-2">Poslední ping: 15ms</p>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn class="futuristic-btn" variant="outlined">
            <v-icon left class="mr-1">{{ mdiCog }}</v-icon>
            Spravovat
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-col>

    <v-col cols="12" md="6" lg="4">
      <v-card class="futuristic-card" shaped elevation="8">
        <v-card-title class="font-oxanium card-title">
          <v-icon left class="mr-2">{{ mdiPoll }}</v-icon>
          Aktivní Ankety
        </v-card-title>
        <v-card-text class="font-inter card-text">
          <p>Kterou mapu dnes večer? (Zbývá: 2h 15m)</p>
          <ul class="pl-4">
            <li>Dust 2 (Hlasů: 8)</li>
            <li>Inferno (Hlasů: 5)</li>
          </ul>
          <p class="font-roboto-mono mt-2">ID Ankety: #A4F8C</p>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn class="futuristic-btn" variant="tonal" color="primary"> Hlasovat </v-btn>
        </v-card-actions>
      </v-card>
    </v-col>

    <v-col cols="12" md="6" lg="4">
      <v-card class="futuristic-card" shaped elevation="8">
        <div class="hexagon-decoration top-right"></div>
        <div class="hexagon-decoration bottom-left"></div>
        <v-card-title class="font-oxanium card-title">
          <v-icon left class="mr-2">{{ mdiChartBar }}</v-icon>
          Moje Statistiky
        </v-card-title>
        <v-card-text class="font-inter card-text">
          <p>Odehráno hodin: <strong>42</strong></p>
          <p>Výher/Proher: <strong>25/17</strong></p>
          <p class="font-roboto-mono mt-2">K/D Ratio: 1.47</p>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn class="futuristic-btn" variant="text" color="secondary"> Detail </v-btn>
        </v-card-actions>
      </v-card>
    </v-col>
  </v-row>
</template>

<script lang="ts" setup>
import { mdiServer, mdiPoll, mdiChartBar, mdiCog } from '@mdi/js'
</script>

<style scoped>
.page-title {
  font-size: 2.5rem;
  font-weight: 700;
  letter-spacing: 1px;
  text-shadow: 0 0 8px var(--v-theme-glow-color);
}
.card-title {
  color: var(--v-theme-primary);
  border-bottom: 1px solid rgba(var(--v-theme-border-color-rgb), var(--v-border-opacity));
  padding-bottom: 0.5em;
  font-size: 1.25rem;
}
.card-text p {
  margin-bottom: 0.5em;
  color: var(--v-theme-text-primary);
  font-size: 0.95rem;
}
.card-text p .text-success {
  color: var(--v-theme-success) !important;
  font-weight: bold;
}
.card-text p .text-warning {
  color: var(--v-theme-warning) !important;
  font-weight: bold;
}
.card-text ul {
  list-style-type: none;
  padding-left: 0;
}
.card-text li::before {
  content: '»';
  color: var(--v-theme-primary);
  margin-right: 0.5em;
  font-weight: bold;
}

.futuristic-card {
  background-color: var(--v-theme-surface);
  border: 1px solid rgba(var(--v-theme-border-color-rgb), var(--v-border-opacity));
  transition: all 0.3s ease-in-out;
  position: relative;
  overflow: hidden;
}
.futuristic-card:hover {
  border-color: var(--v-theme-primary);
  box-shadow:
    0 0 15px 2px var(--v-theme-glow-color),
    0 4px 20px rgba(0, 0, 0, 0.3) !important;
  transform: translateY(-3px);
}
.hexagon-decoration {
  position: absolute;
  width: 50px;
  height: 28.87px;
  background-color: rgba(var(--v-theme-primary-rgb), 0.05);
  clip-path: polygon(25% 0%, 75% 0%, 100% 50%, 75% 100%, 25% 100%, 0% 50%);
  opacity: 0.5;
  pointer-events: none;
}
.hexagon-decoration.top-left {
  top: -10px;
  left: -15px;
  transform: rotate(30deg);
}
.hexagon-decoration.top-right {
  top: -10px;
  right: -15px;
  transform: rotate(-30deg);
}
.hexagon-decoration.bottom-left {
  bottom: -10px;
  left: -15px;
  transform: rotate(-30deg);
}
.hexagon-decoration.bottom-right {
  bottom: -10px;
  right: -15px;
  transform: rotate(30deg);
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
.futuristic-btn[variant='tonal'] {
  background-color: rgba(var(--v-theme-primary-rgb), 0.15) !important;
  color: var(--v-theme-primary) !important;
}
.futuristic-btn[variant='tonal']:hover {
  background-color: rgba(var(--v-theme-primary-rgb), 0.25) !important;
  box-shadow: 0 0 10px 0px var(--v-theme-glow-color);
}
</style>
