import { defineConfig, devices } from '@playwright/test';

const WEB_URL = 'http://localhost:4200';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: WEB_URL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  // Sobe o frontend se ainda não estiver no ar. A API (porta 5080) e o banco
  // precisam estar rodando antes, conforme o README.
  webServer: {
    command: 'npm start',
    url: WEB_URL,
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
