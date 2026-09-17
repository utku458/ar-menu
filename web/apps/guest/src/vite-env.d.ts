/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_ASSETS_ORIGIN: string;
  readonly VITE_DEMO_MENUS?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
