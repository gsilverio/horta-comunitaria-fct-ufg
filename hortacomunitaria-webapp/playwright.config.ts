import { defineConfig, devices } from '@playwright/test'
import { administratorStorageState } from './e2e/fixtures'

export default defineConfig({
  testDir: './e2e', fullyParallel: false, workers: 1, retries: 0, timeout: 45000,
  globalSetup: './e2e/setup.ts', reporter: [['list'], ['html', { open: 'never' }]],
  // Sessão do Adm fictício gravada pelo setup global (cookie de renovação do banco descartável).
  use: { baseURL: 'http://127.0.0.1:5183', storageState: administratorStorageState, trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], timezoneId: 'Asia/Tokyo' } }],
})
