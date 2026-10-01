import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig(({ command }) => ({
    // El proxy ASP.NET conserva 7136 como único origen del navegador, incluido OAuth y HMR.
    base: command === 'serve' ? '/__vite/' : '/',
    server: { hmr: { protocol: 'wss', host: 'localhost', clientPort: 7136 } },
    resolve: { alias: { '@': fileURLToPath(new URL('./resources/js', import.meta.url)) } },
    plugins: [react()],
    build: { outDir: '../tdv2/wwwroot', emptyOutDir: false },
}));
