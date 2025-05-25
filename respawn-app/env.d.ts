/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string; // Base URL for the API
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}