import assert from 'node:assert/strict';
import { chromium } from '@playwright/test';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../../../', import.meta.url));
const profile = JSON.parse((await readFile(path.join(root, 'tdv2/Properties/launchSettings.json'), 'utf8')).replace(/^\uFEFF/, '')).profiles.https;
const origin = profile.applicationUrl.split(';').find(url => url.startsWith('https://'));
assert.equal(new URL(origin).hostname, 'localhost');
const artifacts = path.join(root, '.artifacts/local-validation/browser');
await mkdir(artifacts, { recursive: true });
// This is the real application, without fixture routes or replacements for authentication.
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
// Validate the real trusted certificate. Never intercept/simulate Microsoft or Nexo.
await context.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort());
const page = await context.newPage();
const pageErrors = [];
page.on('pageerror', error => pageErrors.push(error.message));
// Sólo diagnóstico de recursos locales; omite queries OAuth, cookies y cabeceras.
page.on('response', response => {
    if (response.status() >= 400 && new URL(response.url()).pathname.startsWith('/__vite/'))
        console.log('VITE RESOURCE ' + response.status() + ' ' + new URL(response.url()).pathname);
});
page.on('pageerror', error => console.log('PAGE ERROR ' + error.message));
const checks = [];
async function check(name, work) { await work(); checks.push({ name, passed: true }); console.log('PASS ' + name); }
let loginStatus;
try {
    await check('HTTPS confiable y portada real servida por ASP.NET', async () => {
        assert.equal((await page.goto(origin)).status(), 200);
        await page.getByRole('link', { name: 'Registrar un proceso operativo' }).waitFor();
        // Use Edge's Windows trust store for API calls too, without ignoring TLS errors.
        assert.equal(await page.evaluate(async () => (await fetch('/health/live')).status), 200);
        await page.screenshot({ path: path.join(artifacts, 'portada.png') });
    });
    await check('React carga y obtiene props JSON de ASP.NET en el mismo origen', async () => {
        // La primera optimización de dependencias de Vite puede tardar más que una navegación compilada.
        const props = page.waitForResponse(r => new URL(r.url()).pathname === '/acceso-restringido' && r.request().headers()['x-tdv2-page'] === '1', { timeout: 90000 });
        props.catch(() => {});
        assert.equal((await page.goto(origin + '/acceso-restringido', { timeout: 90000 })).status(), 200);
        const response = await props;
        assert.equal(response.status(), 200);
        const data = await response.json();
        assert.equal(data.component, 'AccesoRestringido');
        assert.equal(data.props.auth.user, null);
        assert.ok(data.props.csrfToken);
        await page.getByRole('heading', { name: 'Acceso no disponible' }).waitFor();
        await page.screenshot({ path: path.join(artifacts, 'react-backend.png') });
    });
    await check('Reintentar acceso desde React recibe 401, sin usuario ficticio', async () => {
        const denied = page.waitForResponse(r => new URL(r.url()).pathname === '/inicio' && r.request().headers()['x-tdv2-page'] === '1');
        await page.getByRole('button', { name: 'Reintentar acceso' }).click();
        assert.equal((await denied).status(), 401);
        await page.getByRole('heading', { name: 'Acceso no disponible' }).waitFor();
        const csrf = await page.evaluate(async () => {
            const response = await fetch('/session/csrf');
            const data = await response.json();
            return { status: response.status, tokenPresent: typeof data.token === 'string' && data.token.length > 0 };
        });
        assert.deepEqual(csrf, { status: 200, tokenPresent: true });
    });
    await check('Microsoft no se sustituye: falta configuración o redirección real sin canjear identidad', async () => {
        // Fetch manual never follows Microsoft's URL. Chromium's extra-info event exposes
        // the original local status/headers even though JS sees an opaque redirect.
        const devtools = await context.newCDPSession(page);
        await devtools.send('Network.enable');
        const localRequests = new Set();
        let timer;
        const entry = new Promise((resolve, reject) => {
            timer = setTimeout(() => reject(new Error('No local OAuth response observed')), 10000);
            devtools.on('Network.requestWillBeSent', event => {
                if (event.request.url === origin + '/connect') localRequests.add(event.requestId);
            });
            devtools.on('Network.responseReceivedExtraInfo', event => {
                if (localRequests.has(event.requestId)) { clearTimeout(timer); resolve(event); }
            });
        });
        let response;
        try {
            await page.evaluate(async () => { await fetch('/connect', { redirect: 'manual' }); });
            response = await entry;
        } finally { clearTimeout(timer); await devtools.detach(); }
        loginStatus = response.statusCode;
        if (loginStatus === 503) assert.equal(loginStatus, 503);
        else {
            assert.equal(loginStatus, 302);
            const location = Object.entries(response.headers).find(([key]) => key.toLowerCase() === 'location')?.[1];
            const authorization = new URL(location);
            assert.equal(authorization.hostname, 'login.microsoftonline.com');
            assert.equal(authorization.searchParams.get('redirect_uri'), origin + '/connect');
        }
        // Neither a 302 nor this test establishes a Microsoft login or a Nexo connection.
        assert.equal((await context.cookies()).some(cookie => cookie.name === 'tdv2.aspnet.session'), false);
    });
    assert.deepEqual(pageErrors, []);
    await writeFile(path.join(artifacts, 'verification.json'), JSON.stringify({ utc: new Date().toISOString(), origin, browser: browser.version(), trustedHttps: true,
        realApplication: true, authenticationSimulated: false, microsoftLoginValidated: false, nexoConnectivityValidated: false,
        microsoftEntryStatus: loginStatus, localCallbackVerified: loginStatus === 302, externalFontsBlocked: true, checks, pageErrors }, null, 2));
} catch (error) {
    console.error('FAIL local startup: ' + error.message);
    process.exitCode = 1;
} finally { await browser.close(); }
