/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_GUEST_MENU_BASE_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
