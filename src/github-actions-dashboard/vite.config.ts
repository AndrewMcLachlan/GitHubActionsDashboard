import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import { tanstackRouter } from '@tanstack/router-plugin/vite'

export default defineConfig({
  plugins: [
    tanstackRouter({
      target: 'react',
      autoCodeSplitting: true,
    }),
    react()
  ],
  server: {
    port: 3010,
    proxy: {
      "/api": "http://localhost:5010",
      "/login": "http://localhost:5010",
      "/callback": "http://localhost:5010",
    }
  },
},)
