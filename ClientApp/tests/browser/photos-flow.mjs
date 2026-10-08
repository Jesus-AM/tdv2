import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';

const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, output = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !output) throw new Error('Sólo host sintético aislado.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 900 } });
context.setDefaultTimeout(15000);
let accelerate = false, photoRequests = 0;
const photoResponses = [];
async function intercept(route) {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect' || url.pathname === '/session/microsoft-logout') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            const target = new URL(headers.location);
            if (target.pathname.endsWith('/authorize')) headers.location = origin + '/connect?code=synthetic-code&state=' + encodeURIComponent(target.searchParams.get('state'));
            else { delete headers.location; return route.fulfill({ status: 200, headers, body: 'Synthetic Microsoft logout' }); }
        }
        return route.fulfill({ response, headers });
    }
    if (url.pathname === '/user/photo') {
        photoRequests++;
        const response = await route.fetch();
        photoResponses.push({ status: response.status(), ...(response.ok() ? { state: (await response.json()).estado, present: !!(await response.json()).photo } : {}) });
        if (accelerate && response.ok()) {
            const json = await response.json();
            // Sólo el reloj del verificador acelera la espera. Los límites reales se prueban en PostgreSQL.
            json.renuevaEn = new Date(Date.parse(json.servidorEn || new Date().toISOString()) + 30000).toISOString();
            return route.fulfill({ response, json });
        }
        return route.fulfill({ response });
    }
    return route.continue();
}
await context.route('**/*', intercept);
const page = await context.newPage();
const report = { synthetic: true, browser: browser.version(), completed: false, checks: [], errors: [] };
page.on('pageerror', error => report.errors.push(error.message));
const avatar = (p = page) => p.getByRole('button', { name: 'Abrir menú de usuario' }).locator('img');
async function control(action) {
    const response = await context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } });
    assert.equal(response.status(), 200); return response.json();
}
async function check(name, work) { await work(); report.checks.push(name); console.log('PHOTO PASS ' + name); }
async function navigate(name) {
    await page.getByRole('navigation', { name: 'Módulos', exact: true }).getByRole('button', { name: 'Configuración', exact: true }).click();
    await page.getByRole('menuitem', { name, exact: true }).click();
    await expect(page.getByRole('heading', { name, exact: true, level: 1 })).toBeVisible();
}
async function renew(minutes, mode = 'ok', extra = '') {
    await control(`photo-control?minutes=${minutes}&mode=${mode}${extra}`);
    const response = page.waitForResponse(r => new URL(r.url()).pathname === '/user/photo');
    await page.clock.fastForward(31000);
    return (await response).json();
}
try {
    await page.clock.install();
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    await expect(avatar()).toHaveCount(1); const original = await avatar().getAttribute('src');
    await check('Navegación SPA entre módulo y submódulos conserva imagen sin nuevas peticiones', async () => {
        const before = photoRequests;
        for (const name of ['Configuración', 'Sincronizaciones', 'Pruebas de acceso']) {
            await navigate(name); await expect(avatar()).toHaveAttribute('src', original);
        }
        assert.equal(photoRequests, before); assert.equal((await control('photo-state')).requests, 1);
    });
    await check('Recarga completa usa caché privado autorizado sin nueva descarga Graph', async () => {
        await page.reload(); await expect(avatar()).toHaveAttribute('src', original);
        assert.equal((await control('photo-state')).requests, 1);
    });
    accelerate = true;
    await check('Renovación lenta conserva la foto hasta confirmar su reemplazo', async () => {
        await control('photo-control?minutes=16&changed=true'); await control('photo-hold');
        const response = page.waitForResponse(r => new URL(r.url()).pathname === '/user/photo');
        void response.catch(() => {});
        await page.clock.fastForward(900100);
        await expect.poll(async () => (await control('photo-state')).requests).toBe(2);
        await expect(avatar()).toHaveAttribute('src', original);
        await control('photo-release'); await response;
        await expect(avatar()).not.toHaveAttribute('src', original);
    });
    const updated = await avatar().getAttribute('src');
    await check('Indisponibilidad Graph conserva última imagen; ausencia confirmada muestra iniciales', async () => {
        assert.equal((await renew(32, 'temporary')).estado, 'conservada');
        await expect(avatar()).toHaveAttribute('src', updated);
        assert.equal((await renew(34, 'missing')).estado, 'ausente');
        await expect(avatar()).toHaveCount(0);
        await renew(37); await expect(avatar()).toHaveCount(1);
    });
    await page.clock.resume(); accelerate = false;
    await check('Representación limpia foto del actor antes de completar la petición; objetivo sin foto usa iniciales', async () => {
        await page.getByRole('button', { name: 'Seleccionar usuario', exact: true }).click();
        await page.getByLabel('Nombre o correo institucional').fill('encargada');
        await page.getByRole('button', { name: 'Buscar', exact: true }).click();
        await page.getByRole('button', { name: /Encargada sintética.*encargada@uacj/ }).click();
        await page.getByLabel('Motivo de la representación').fill('Fotografía sintética');
        let held; await page.route('**/actuar-como-usuario', route => { held = route; });
        await page.getByRole('button', { name: 'Actuar como Encargada sintética', exact: true }).click();
        await expect.poll(() => !!held).toBe(true); await expect(avatar()).toHaveCount(0);
        await held.continue(); await page.unroute('**/actuar-como-usuario');
        await page.waitForURL(origin + '/inicio');
        await expect(page.getByText('Actuando como Encargada sintética', { exact: true })).toBeVisible();
        await expect(avatar()).toHaveCount(0);
    });
    await check('Identidad representada verificada recupera su propia miniatura', async () => {
        await control('photo-control?minutes=37&target=true&changed=true');
        const targetContext = await browser.newContext({ ignoreHTTPSErrors: true });
        await targetContext.route('**/*', intercept); const target = await targetContext.newPage();
        await target.goto(origin + '/connect'); await target.waitForURL(origin + '/inicio');
        await expect(avatar(target)).toHaveCount(1); const targetPhoto = await avatar(target).getAttribute('src');
        assert.notEqual(targetPhoto, updated);
        await page.reload(); await expect(avatar()).toHaveAttribute('src', targetPhoto);
        await targetContext.close();
    });
    await check('Volver al actor y menú móvil conservan identidad y fotografía correctas', async () => {
        await page.getByRole('button', { name: 'Volver a mi usuario', exact: true }).click();
        await expect(avatar()).toHaveAttribute('src', updated);
        await page.setViewportSize({ width: 360, height: 800 });
        await page.getByRole('button', { name: 'Abrir menú de usuario' }).focus(); await page.keyboard.press('Enter');
        await expect(page.getByRole('menuitem', { name: 'Usar otra cuenta' })).toBeVisible();
        await page.screenshot({ path: path.join(output, 'photo-mobile.png'), fullPage: true });
        await page.keyboard.press('Escape');
    });
    await check('Usar otra cuenta retira foto inmediatamente y avisa a otras pestañas; cuenta nueva nunca hereda imagen', async () => {
        const tab = await context.newPage(); await tab.goto(origin + '/inicio'); await expect(avatar(tab)).toHaveAttribute('src', updated);
        let held; await page.route('**/session/use-another-account', route => { held = route; });
        await page.getByRole('button', { name: 'Abrir menú de usuario' }).click();
        await page.getByRole('menuitem', { name: 'Usar otra cuenta' }).click();
        await expect.poll(() => !!held).toBe(true); await expect(avatar()).toHaveCount(0); await expect(avatar(tab)).toHaveCount(0);
        const navigation = page.waitForEvent('framenavigated', { predicate: frame => frame === page.mainFrame() && new URL(frame.url()).pathname === '/inicio' });
        await held.continue(); await page.unroute('**/session/use-another-account');
        await navigation;
        await expect(avatar()).toHaveCount(1); assert.notEqual(await avatar().getAttribute('src'), updated);
        await tab.close();
    });
    await check('Cerrar sesión limpia imagen antes de responder y revoca acceso al endpoint', async () => {
        let held; await page.route('**/logout', route => { held = route; });
        await page.getByRole('button', { name: 'Abrir menú de usuario' }).click();
        await page.getByRole('menuitem', { name: 'Cerrar sesión' }).click();
        await expect.poll(() => !!held).toBe(true); await expect(avatar()).toHaveCount(0);
        await held.continue(); await page.unroute('**/logout');
        await page.waitForURL(origin + '/session/microsoft-logout');
        assert.equal((await context.request.get(origin + '/user/photo')).status(), 401);
    });
    assert.deepEqual(report.errors, []); report.completed = true;
} catch (error) {
    report.errors.push(error.message); report.finalPath = new URL(page.url()).pathname; report.photoResponses = photoResponses;
    throw error;
} finally {
    await writeFile(path.join(output, 'photos-browser.json'), JSON.stringify(report, null, 2));
    await browser.close();
}
console.log('PASS photographs: ' + report.checks.length);
