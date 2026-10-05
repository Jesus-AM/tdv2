import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig(({ command }) => ({
    // El proxy ASP.NET conserva 7136 como único origen del navegador, incluido OAuth y HMR.
    base: command === 'serve' ? '/__vite/' : '/',
    server: { hmr: { protocol: 'wss', host: 'localhost', clientPort: 7136 } },
    resolve: { alias: { '@': fileURLToPath(new URL('./resources/js', import.meta.url)) } },
    plugins: [react(), {
        name: 'tdv2-visual-studio-launch',
        apply: 'serve',
        configureServer(server) {
            // JSPS comprueba el puerto de launch.json antes de iniciar Kestrel.
            // Espera a Vite (5173); el navegador pasa al origen real HTTPS (7136).
            server.middlewares.use('/__vite/__launch', async (_request, response) => {
                // El orden del perfil no garantiza que Kestrel ya acepte conexiones.
                // Health/live no consulta bases ni servicios institucionales.
                const deadline = Date.now() + 30_000;
                while (!response.destroyed && Date.now() < deadline) {
                    try {
                        const health = await fetch('http://127.0.0.1:5064/health/live', {
                            signal: AbortSignal.timeout(1_000), redirect: 'error',
                        });
                        await health.body?.cancel();
                        if (health.ok) {
                            response.writeHead(302, { Location: 'https://localhost:7136/', 'Cache-Control': 'no-store' });
                            response.end();
                            return;
                        }
                    } catch { /* Kestrel todavía está arrancando. */ }
                    await new Promise(resolve => setTimeout(resolve, 250));
                }
                if (!response.destroyed) {
                    response.writeHead(503, { 'Content-Type': 'text/plain; charset=utf-8', 'Cache-Control': 'no-store' });
                    response.end('La interfaz de TDV2 no está disponible.');
                }
            });
        },
    }],
    build: { outDir: '../tdv2/wwwroot', emptyOutDir: false },
}));
