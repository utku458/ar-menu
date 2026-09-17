/// <reference types="vitest/config" />
import babel from '@rolldown/plugin-babel';
import tailwindcss from '@tailwindcss/vite';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import { stripModelViewerDebugLogs } from '@armenu/ar-viewer/vite-plugin';
import { securityHeaders } from '@armenu/web-security';
import { defineConfig, loadEnv } from 'vite';

import { initialLoad } from './vite-plugins/initial-load.ts';

export default defineConfig(({ mode, isPreview }) => ({
  // Menus live at https://armenu.app/m/{slug}: the path printed in QR codes (see TenantSlug).
  base: '/m/',
  plugins: [
    react(),
    // React Compiler memoizes components and hooks automatically.
    babel({ presets: [reactCompilerPreset()] }),
    tailwindcss(),
    stripModelViewerDebugLogs(),
    initialLoad({
      appModule: 'src/app/start.tsx',
      budgetKiB: 130,
      lazyOnly: ['@google/model-viewer', 'three'],
    }),
  ],
  build: {
    // The 3D stack is one large chunk by nature (three.js), loaded only on demand; the initial-load plugin guards
    // what matters, the JavaScript loaded on startup.
    chunkSizeWarningLimit: 1100,
    rolldownOptions: {
      output: {
        chunkFileNames: (chunk) =>
          chunk.facadeModuleId?.includes('/packages/ar-viewer/') === true
            ? 'assets/ar-viewer-[hash].js'
            : 'assets/[name]-[hash].js',
      },
    },
  },
  server: {
    port: 5173,
    strictPort: true,
  },
  preview: {
    port: 4173,
    strictPort: true,
    // The production headers, content security policy included: end-to-end tests run under them.
    // Only built when previewing: other modes (unit tests) have no origins configured.
    headers: isPreview === true ? securityHeaders('guest', loadEnv(mode, import.meta.dirname, 'VITE_')) : {},
  },
  test: {
    include: ['src/**/*.test.ts'],
  },
}));
