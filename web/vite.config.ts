/// <reference types="vitest/config" />
// (Los tipos de `test` llegan por la referencia de arriba. Importar defineConfig desde 'vitest/config' hace que Vitest 5
// cargue su runtime dentro del archivo de configuración y los tests fallan con "failed to find the current suite".)
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // En desarrollo, Vite hace lo mismo que nginx en Docker: el navegador llama a /api/* en su propio origen y
    // el servidor reenvía a la API (sin el prefijo /api). Así el código del frontend es idéntico en ambos entornos
    // y no hace falta CORS. La API del compose queda publicada en el puerto 8080 por defecto.
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: false,
  },
});
