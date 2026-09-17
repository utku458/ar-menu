/// <reference types="vitest/config" />
import babel from '@rolldown/plugin-babel';
import tailwindcss from '@tailwindcss/vite';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import { stripModelViewerDebugLogs } from '@armenu/ar-viewer/vite-plugin';
import { securityHeaders } from '@armenu/web-security';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode, isPreview }) => ({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] }), tailwindcss(), stripModelViewerDebugLogs()],
  build: {
    // The 3D preview chunk (three.js) is large by nature and loads only when a model is previewed.
    chunkSizeWarningLimit: 1100,
  },
  server: {
    port: 5174,
    strictPort: true,
  },
  preview: {
    // The production headers, content security policy included: end-to-end tests run under them.
    // Only built when previewing: other modes (unit tests) have no origins configured.
    headers:
      isPreview === true ? securityHeaders('dashboard', loadEnv(mode, import.meta.dirname, 'VITE_')) : {},
  },
  test: {
    include: ['src/**/*.test.ts'],
  },
}));
