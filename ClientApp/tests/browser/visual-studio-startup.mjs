import assert from 'node:assert/strict';
import { chromium } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

// Ejecutar con TDV2 HTTPS + React ya iniciado desde Visual Studio.
// Sólo páginas anónimas: no solicita OAuth ni consulta datos institucionales/locales.
const artifacts = new URL('../../../.artifacts/visual-studio/', import.meta.url);
await mkdir(artifacts, { recursive: true });
const vite = 'http://127.0.0.1:5173';
const origin = 'https://localhost:7136';
const index = await fetch(vite + '/__vite/index.html');
assert.equal(index.status, 200);
assert.ok((await index.text()).includes('/__vite/@vite/client'));
const launch = await fetch(vite + '/__vite/__launch', { redirect: 'manual' });
assert.equal(launch.status, 302);
assert.equal(launch.headers.get('location'), origin + '/');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
    const context = await browser.newContext();
    await context.route('**/*', route => [origin, vite].includes(new URL(route.request().url()).origin) ? route.continue() : route.abort());
    const page = await context.newPage();
    const errors = [];
    const sockets = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('websocket', socket => sockets.push(new URL(socket.url()).origin));
    assert.equal((await page.goto(vite + '/__vite/__launch')).status(), 200);
    assert.equal(page.url(), origin + '/');
    await page.getByRole('link', { name: 'Registrar un procedimiento institucional' }).waitFor();
    const props = page.waitForResponse(r => new URL(r.url()).pathname === '/acceso-restringido' && r.request().headers()['x-tdv2-page'] === '1');
    assert.equal((await page.goto(origin + '/acceso-restringido', { timeout: 90000 })).status(), 200);
    assert.equal((await props).status(), 200);
    await page.getByRole('heading', { name: 'Acceso no disponible', exact: true }).waitFor();
    assert.ok(await page.locator('script[src*="/__vite/@vite/client"]').count());
    assert.equal((await page.goto(origin + '/formatos/ajena')).status(), 401);
    await page.getByRole('heading', { name: 'Acceso no disponible', exact: true }).waitFor();
    assert.ok(sockets.includes('wss://localhost:7136'));
    assert.deepEqual(errors, []);
    await page.screenshot({ path: fileURLToPath(new URL('react.png', artifacts)) });
    await writeFile(new URL('verification.json', artifacts), JSON.stringify({
        utc: new Date().toISOString(), viteIndexStatus: index.status, launchRedirectStatus: launch.status,
        launchDestination: origin, trustedHttps: true, razorLandingRendered: true, reactRendered: true,
        reactPropsStatus: 200, privateHtmlStatus: 401, sameOriginHmr: true,
        browser: browser.version(), externalFontsBlocked: true,
        institutionalConnectionsAttempted: false, databaseQueriesRequested: false,
        microsoftLoginValidated: false, pageErrors: errors,
    }, null, 2) + '\n');
    console.log('PASS: Vite 200; lanzamiento 302 a HTTPS; portada y React; props 200; privado 401; HMR wss 7136; cero errores JS.');
} finally {
    await browser.close();
}
