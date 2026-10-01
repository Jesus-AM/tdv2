import assert from 'node:assert/strict';
import { chromium } from '@playwright/test';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = fileURLToPath(new URL('../../../', import.meta.url));
const source = path.join(root, 'ClientApp/resources/js/pages/AccesoRestringido.tsx');
const artifact = path.join(root, '.artifacts/development-verification.json');
const origin = 'https://localhost:7136';
const original = await readFile(source);
const changed = Buffer.from(original.toString('utf8').replace('Acceso no disponible', 'Acceso no disponible · HMR'));
assert.notDeepEqual(original, changed);
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext();
await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort());
const page = await context.newPage();
const errors = [];
const sockets = [];
page.on('pageerror', error => errors.push(error.message));
page.on('websocket', socket => sockets.push(new URL(socket.url()).origin));
let edited = false;
try {
    assert.equal((await page.goto(origin + '/acceso-restringido', { timeout: 90000 })).status(), 200);
    await page.getByRole('heading', { name: 'Acceso no disponible', exact: true }).waitFor();
    assert.ok(await page.locator('script[src*="/__vite/@vite/client"]').count());
    await page.waitForFunction(() => document.querySelector('#app')?.textContent?.includes('Acceso no disponible'));
    let documents = 0;
    page.on('request', request => { if (request.isNavigationRequest()) documents++; });
    await page.evaluate(() => { window.__tdv2HmrProbe = 'preservado'; });
    await writeFile(source, changed); edited = true;
    await page.getByRole('heading', { name: 'Acceso no disponible · HMR', exact: true }).waitFor({ timeout: 30000 });
    assert.equal(await page.evaluate(() => window.__tdv2HmrProbe), 'preservado');
    assert.equal(documents, 0);
    assert.ok(sockets.includes('wss://localhost:7136'));
    assert.ok(sockets.every(socket => socket === 'wss://localhost:7136'));
    // Una URL privada HTML sigue autorizándose antes del shell servido por Vite.
    assert.equal((await page.goto(origin + '/formatos/ajena')).status(), 401);
    await page.getByRole('heading', { name: 'Acceso no disponible · HMR', exact: true }).waitFor();
    const csrf = await page.evaluate(async () => {
        const missing = await fetch('/logout', { method: 'POST' });
        const token = await (await fetch('/session/csrf')).json();
        const valid = await fetch('/logout', { method: 'POST', headers: { 'X-CSRF-TOKEN': token.token, Accept: 'application/json' } });
        return [missing.status, valid.status];
    });
    assert.deepEqual(csrf, [419, 200]);
    const cookies = await context.cookies();
    assert.ok(cookies.filter(c => c.name === 'tdv2.aspnet.csrf').every(c => c.secure && c.sameSite === 'Strict'));
    assert.deepEqual(errors, []);
    await mkdir(path.dirname(artifact), { recursive: true });
    await writeFile(artifact, JSON.stringify({ utc: new Date().toISOString(), trustedHttps: true, sameOriginHmr: true,
        updateWithoutNavigation: true, privateHtmlDenied: true, csrfStatuses: csrf, visualStudioUiExercised: false,
        microsoftLoginValidated: false, pageErrors: errors }, null, 2));
    console.log('PASS Vite/HTTPS: React cambia sin recargar; HMR, permisos y CSRF conservan el origen 7136.');
} finally {
    if (edited) {
        if ((await readFile(source)).equals(changed)) await writeFile(source, original);
        else throw new Error('El archivo cambió concurrentemente; no se sobrescribió.');
    }
    await browser.close();
}
