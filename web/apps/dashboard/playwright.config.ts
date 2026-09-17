import { defineConfig, devices } from '@playwright/test';

const isCI = process.env.CI === 'true';
const port = 4175;
const channel = isCI ? undefined : 'chrome';

export default defineConfig({
  testDir: 'e2e',
  fullyParallel: true,
  forbidOnly: isCI,
  retries: isCI ? 1 : 0,
  reporter: isCI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: `http://localhost:${port}/`,
    locale: 'tr-TR',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'desktop', use: { ...devices['Desktop Chrome'], channel } }],
  webServer: {
    command: 'pnpm build:e2e && pnpm preview:e2e',
    url: `http://localhost:${port}/`,
    reuseExistingServer: !isCI,
    timeout: 120_000,
  },
});
