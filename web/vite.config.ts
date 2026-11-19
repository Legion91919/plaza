/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The API runs on http://localhost:5102 (see .agentic/runbook.md).
const api = process.env.PLAZA_API ?? 'http://localhost:5102';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': api,
      '/health': api,
      '/hubs': { target: api, ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
  },
});
