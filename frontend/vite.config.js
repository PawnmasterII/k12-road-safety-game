import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Override with VITE_API_TARGET to point the dev server at another
      // backend (e.g. an isolated test DB). Defaults to the documented port.
      '/api': process.env.VITE_API_TARGET || 'http://localhost:8000',
    },
  },
})
