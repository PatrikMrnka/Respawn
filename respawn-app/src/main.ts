import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'

// Vuetify - UI framework
import 'vuetify/styles'
import { createVuetify, type ThemeDefinition } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg'


// custom futuristic theme
import './assets/futuristic.css'

// SweetAlert2 - pop up boxes
import 'sweetalert2/dist/sweetalert2.min.css'


// Custom dark theme
const futuristicDarkTheme: ThemeDefinition = {
  dark: true,
  colors: {
    background: '#121826',
    surface: '#1A2033',
    primary: '#00E0FF',
    'primary-darken-1': '#00B8D4',
    secondary: '#7F00FF',
    'secondary-darken-1': '#6A00D4',
    error: '#FF5252',
    info: '#2196F3',
    success: '#4CAF50',
    warning: '#FFC107',
    anchor: '#00E0FF',
    'text-primary': '#E0E0E0',
    'text-secondary': '#A0A0C0',
    'border-color': '#00E0FF',
    'glow-color': 'rgba(0, 224, 255, 0.5)',
  },
  variables: {
    'border-opacity': 0.3,
    'theme-on-background': '#121826',
    'theme-on-surface': '#E0E0E0',
  },
}

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'futuristicDarkTheme',
    themes: {
      futuristicDarkTheme,
    },
  },
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: {
      mdi,
    },
  },
})

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(vuetify)

app.mount('#app')
