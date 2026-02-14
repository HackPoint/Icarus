import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  retries: 1,
  use: {
    baseURL: process.env.API_BASE_URL || 'http://localhost:5000',
    extraHTTPHeaders: {
      'Accept': 'application/json',
    },
  },
  reporter: [
    ['html', { open: 'never' }],
    ['list'],
  ],
});
