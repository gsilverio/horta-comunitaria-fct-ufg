import react, { reactCompilerPreset } from '@vitejs/plugin-react'
import babel from '@rolldown/plugin-babel'
import { defineConfig, loadEnv } from 'vite'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const target = process.env.API_PROXY_TARGET || env.API_PROXY_TARGET || 'http://127.0.0.1:5163'
  return {
    plugins: [
      tailwindcss(),
      react(),
      babel({ presets: [reactCompilerPreset()] })
    ],
    server: { host: '127.0.0.1', port: 5173, strictPort: true, proxy: { '/api': { target, changeOrigin: true } } },
    preview: { host: '127.0.0.1', port: 5173, strictPort: true, proxy: { '/api': { target, changeOrigin: true } } },
  }
})
