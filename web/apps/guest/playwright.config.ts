import { defineConfig, devices } from '@playwright/test';

const isCI = process.env.CI === 'true';
const port = 4174;

// Locally the installed Google Chrome is used, so no browser download is needed; CI installs Chromium.
const channel = isCI ? undefined : 'chrome';

export default defineConfig({
  testDir: 'e2e',
  fullyParallel: true,
  forbidOnly: isCI,
  retries: isCI ? 1 : 0,
  reporter: isCI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: `http://localhost:${port}/m/`,
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'phone', use: { ...devices['Pixel 7'], channel } },
    { name: 'desktop', use: { ...devices['Desktop Chrome'], channel } },
  ],
  // Tests run against the production build: code splitting, preloading and the entry chunk are what guests get.
  webServer: {
    command: 'pnpm build:e2e && pnpm preview:e2e',
    url: `http://localhost:${port}/m/`,
    reuseExistingServer: !isCI,
    timeout: 120_000,
  },
});
