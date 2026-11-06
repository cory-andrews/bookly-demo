import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vitest/config'

// Aspire injects the API's URLs. The fallback matches the API's launch profile for running without Aspire.
const apiUrl =
  process.env.services__API__https__0 ??
  process.env.services__API__http__0 ??
  'https://localhost:7233'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      // Same-origin requests through the proxy mean the API doesn't need CORS.
      '/api': { target: apiUrl, changeOrigin: true, secure: false },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['src/lib/luxonConfig.ts'],
    // A zone away from UTC, so any time accidentally handled as local time fails the tests.
    env: { TZ: 'America/New_York' },
  },
})
