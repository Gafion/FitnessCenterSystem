import { defineConfig, devices } from '@playwright/test';

// Full-stack E2E (D-043): the built frontend against the real API, PostgreSQL and
// Mailpit. Nothing is mocked and nothing is started here: CI starts the services in
// its "Full-stack E2E" job, and locally `aspire run` does. The mocked suite is
// playwright.config.ts.
//
//   E2E_BASE_URL  the frontend, with /api reaching the API (default: Aspire's port)
//   MAILPIT_URL   Mailpit's web/API address, e.g. http://localhost:8025 (required)
export default defineConfig({
  testDir: './e2e-full',
  outputDir: './test-results/full',
  reporter: [['list'], ['html', { outputFolder: 'playwright-report/full' }]],
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  // The flows share one database and one schedule; one at a time keeps the spot
  // counts each test reads from changing under it.
  workers: 1,
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
