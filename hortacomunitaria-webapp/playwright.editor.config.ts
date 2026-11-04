import { defineConfig, devices } from '@playwright/test'

// Exercita somente o rascunho visual; as respostas cadastrais são isoladas no navegador.
export default defineConfig({
  testDir: './editor-tests', workers: 1, retries: 0,
  use: { baseURL: 'http://127.0.0.1:5186', ...devices['Desktop Chrome'] },
  webServer: { command: 'node node_modules/vite/bin/vite.js --host 127.0.0.1 --port 5186 --strictPort', url: 'http://127.0.0.1:5186', reuseExistingServer: false },
})
