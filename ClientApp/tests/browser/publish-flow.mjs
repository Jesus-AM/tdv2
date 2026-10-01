import assert from 'node:assert/strict';
import { chromium } from '@playwright/test';
import { spawn } from 'node:child_process';
import { writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = fileURLToPath(new URL('../../../', import.meta.url));
const origin = 'https://localhost:7146';
const server = spawn('dotnet', ['tdv2.dll', '--urls', origin], {
    cwd: path.join(root, '.artifacts/publish-maintenance'), windowsHide: true,
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Production', DOTNET_ENVIRONMENT: 'Production',
        DataProtection__KeyDirectory: path.join(root, '.artifacts/publish-runtime-keys') }, stdio: 'ignore',
});
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext();
await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort());
const page = await context.newPage();
const errors = [];
page.on('pageerror', error => errors.push(error.message));
try {
    let ready = false;
    for (let i = 0; i < 30; i++) {
        try { ready = (await page.goto(origin + '/health/live')).status() === 200; } catch { }
        if (ready) break;
        if (server.exitCode !== null) throw new Error('El paquete publicado terminó al arrancar.');
        await new Promise(resolve => setTimeout(resolve, 500));
    }
    assert.ok(ready);
    assert.equal((await page.goto(origin)).status(), 200);
    await page.getByRole('link', { name: 'Registrar un proceso operativo' }).waitFor();
    assert.equal((await page.goto(origin + '/acceso-restringido')).status(), 200);
    await page.getByRole('heading', { name: 'Acceso no disponible', exact: true }).waitFor();
    assert.ok(await page.locator('script[src^="/assets/"]').count());
    assert.equal(await page.locator('script[src*="__vite"]').count(), 0);
    const checks = await page.evaluate(async () => ({
        login: (await fetch('/connect', { redirect: 'manual' })).status,
        private: (await fetch('/inicio', { headers: { 'X-TDV2-Page': '1', Accept: 'application/json' } })).status,
        csrf: (await fetch('/logout', { method: 'POST' })).status,
    }));
    assert.deepEqual(checks, { login: 503, private: 401, csrf: 419 });
    assert.deepEqual(errors, []);
    await writeFile(path.join(root, '.artifacts/publish-verification.json'), JSON.stringify({ utc: new Date().toISOString(),
        production: true, trustedHttps: true, compiledReact: true, viteRequired: false, secretsConfigured: false,
        checks, pageErrors: errors, operatingSystemTested: 'Windows', ubuntuExecuted: false, deployed: false }, null, 2));
    console.log('PASS publicación: Production HTTPS, React compilado sin Vite, secretos externos requeridos y rechazos seguros.');
} finally {
    await browser.close();
    server.kill();
    await new Promise(resolve => { if (server.exitCode !== null) resolve(); else server.once('exit', resolve); });
}
