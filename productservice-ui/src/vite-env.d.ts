/// <reference types="vite/client" />

// Type definitions for environment variables exposed to the app.
// Strongly typing them here means typos like VITE_API_BAS_URL fail at compile time.
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
