import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  // En intégration continue (variable CI) : un `test.only` oublié fait échouer, un test en échec est rejoué deux fois.
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: process.env['CI'] ? 1 : undefined,
  // `open: 'never'` : le rapport HTML s'écrit dans playwright-report/ sans démarrer de serveur à la fin.
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    // `ng e2e` démarre le serveur de développement et fournit son adresse par cette variable.
    baseURL: process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
