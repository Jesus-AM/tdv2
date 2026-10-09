import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';

const origin = process.env.TDV2_BROWSER_ORIGIN;
const fixtureKey = process.env.TDV2_BROWSER_KEY;
const artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !fixtureKey || !artifacts) throw new Error('Requires the isolated native test host.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
// Microsoft's browser and HTTPS peer are simulated. React, ASP.NET, cookies, CSRF and PostgreSQL are real.
// Intercept the local redirect itself: Playwright route handlers do not intercept subsequent redirects.
// No external request is allowed, including fonts. Never send even synthetic OAuth data to a real service.
let microsoftLogoutTarget;
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect' || url.pathname === '/session/microsoft-logout') {
        const response = await route.fetch({ maxRedirects: 0 });
        const headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            const target = new URL(headers.location);
            if (target.pathname.endsWith('/authorize')) {
                const state = target.searchParams.get('state');
                headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(state)}`;
                return route.fulfill({ response, headers });
            }
            assert.ok(target.pathname.endsWith('/logout'));
            microsoftLogoutTarget = target;
            delete headers.location;
            return route.fulfill({ status: 200, headers, body: 'Synthetic Microsoft sign-out' });
        }
        return route.fulfill({ response });
    }
    return route.continue();
});
const page = await context.newPage();
const errors = [];
page.on('pageerror', error => errors.push(error.message));
const checks = [];
async function check(name, work) { await work(); checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name); }
async function control(action) {
    const response = await context.request.post(`${origin}/__fixture/${action}`, { headers: { 'X-Fixture-Key': fixtureKey } });
    assert.equal(response.status(), 200); return response;
}
async function stored() { return (await control('stored')).json(); }
async function header(target = page) {
    await target.getByRole('tab', { name: 'Identificación general', exact: true }).click();
    return target.getByLabel('Trámite o servicio 1', { exact: true });
}
async function savedAfter(work, status = 200, target = page) {
    const [response] = await Promise.all([target.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/bloques' && r.request().method() === 'PATCH'), work()]);
    assert.equal(response.status(), status);
}

const syncPath = '/configuracion/sincronizaciones';
async function report() { return (await control('sync-report')).json(); }
const latestRow = () => page.getByRole('table', { name: 'Historial de sincronizaciones' }).getByRole('row').nth(1);
async function refresh() {
    const response = page.waitForResponse(r => new URL(r.url()).pathname === syncPath && r.request().method() === 'GET');
    await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click();
    assert.equal((await response).status(), 200);
}
async function queue(source) {
    const name = source === 'ambas' ? 'Sincronizar SII e ILDA ahora' : 'Sincronizar ' + source.toUpperCase();
    const response = page.waitForResponse(r => new URL(r.url()).pathname === syncPath + '/ejecutar' && r.request().method() === 'POST');
    await page.getByRole('button', { name, exact: true }).click();
    assert.equal((await response).status(), 202);
    await page.locator('p').filter({ hasText: /^Iniciando…$/ }).waitFor();
}
async function saveSchedule(target = page, status = 200) {
    const response = target.waitForResponse(r => new URL(r.url()).pathname === syncPath + '/programacion' && r.request().method() === 'PUT');
    const refreshed = status === 200 ? target.waitForResponse(r => new URL(r.url()).pathname === syncPath && r.request().method() === 'GET') : null;
    await target.getByRole('button', { name: 'Guardar programación', exact: true }).click();
    assert.equal((await response).status(), status);
    // Wait for the subsequent props refresh, keeping the next action in the rendered version.
    if (refreshed) await (await refreshed).finished();
}
async function finished(label = 'Completada') {
    await refresh();
    await latestRow().getByText(label, { exact: true }).waitFor();
}
try {
    await check('Pantalla original: administrador, programación pausada, zona Juárez y sin alertas innecesarias', async () => {
        await page.goto(origin);
        await page.getByRole('link', { name: 'Registrar un procedimiento institucional' }).click();
        await page.waitForURL(origin + '/inicio');
        assert.equal((await page.goto(origin + syncPath)).status(), 200);
        await page.getByRole('heading', { name: 'Sincronizaciones', exact: true }).waitFor();
        const navigation = page.getByRole('navigation', { name: 'Módulos', exact: true });
        assert.equal(await navigation.getByRole('button').count(), 2);
        await navigation.getByRole('button', { name: 'Configuración', exact: true }).click();
        const submenu = page.getByRole('menuitem', { name: 'Sincronizaciones', exact: true });
        await submenu.waitFor(); assert.equal(await submenu.getAttribute('aria-current'), 'page');
        assert.deepEqual(await page.getByRole('menuitem').allTextContents(), ['Configuración', 'Sincronizaciones', 'Pruebas de acceso']);
        await page.screenshot({ path: path.join(artifacts, 'modulos-escritorio.png') });
        await page.keyboard.press('Escape');
        assert.equal(await page.getByLabel('Zona horaria', { exact: true }).inputValue(), 'America/Ciudad_Juarez');
        assert.equal(await page.getByRole('alert').count(), 0);
        assert.equal((await report()).remoteCommands, 0);
        await expect(page.getByRole('heading', { name: 'SII · Unidades responsables y módulos de SIIv2', exact: true })).toBeVisible();
        await expect(page.getByText('Módulos de SIIv2: 0', { exact: true })).toBeVisible();
    });
    await check('React encola ambas; PostgreSQL conserva pendiente y ejecutando, excluye nuevas solicitudes', async () => {
        await control('sync-hold');
        await queue('ambas');
        assert.equal((await report()).runs[0].estado, 'pendiente');
        assert.equal((await report()).remoteCommands, 0);
        assert.equal(await page.getByRole('button', { name: 'Iniciando…', exact: true }).first().isDisabled(), true);
        await control('sync-worker-start');
        await refresh();
        await page.getByText('Sincronizando… ILDA', { exact: true }).waitFor();
        assert.equal((await report()).runs[0].resultado.sii.estado, 'completada');
        assert.equal((await report()).runs[0].resultado.sii_modulos.estado, 'completada');
        assert.equal((await report()).ilda.length, 0);
        await control('sync-worker-release');
        await finished();
        assert.equal((await report()).ilda.length, 5);
        await page.screenshot({ path: path.join(artifacts, 'sincronizaciones-completada.png') });
    });
    await check('Identificación usa sólo copia local, fuente ILDA y trámite informacion_generada; guarda desde React', async () => {
        const before = await report();
        await page.goto(origin + '/formatos/A');
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        assert.equal(await page.getByLabel('Trámite o servicio 1', { exact: true }).inputValue(), 'Constancias sintéticas');
        await page.getByLabel('Trámite o servicio 1', { exact: true }).focus();
        await expect(page.getByLabel('Trámite o servicio 1', { exact: true })).toHaveJSProperty('readOnly', false);
        await savedAfter(() => page.getByLabel('Trámite o servicio 1', { exact: true }).fill('Respuesta conservada React'));
        const after = await report();
        assert.equal(after.remoteCommands, before.remoteCommands);
        assert.equal(after.forms[0].contenido.identificacion[0].fuente, 'ILDA');
        assert.equal(after.forms[0].contenido.identificacion[0].tramite, 'Respuesta conservada React');
        await page.screenshot({ path: path.join(artifacts, 'formato-ilda-local.png') });
    });
    await check('Cambio y baja ILDA conservan respuesta y versión; GET incorpora IDs nuevos sin persistir', async () => {
        const before = await report();
        await control('sync-change-source');
        await page.goto(origin + syncPath);
        await queue('ilda'); await control('sync-tick'); await finished();
        await page.goto(origin + '/formatos/A');
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        assert.equal(await page.getByLabel('Trámite o servicio 1', { exact: true }).inputValue(), 'Respuesta conservada React');
        assert.equal(await page.getByLabel('Trámite o servicio 3', { exact: true }).inputValue(), 'Nuevo trámite ILDA');
        const after = await report();
        assert.equal(after.forms[0].version, before.forms[0].version);
        assert.deepEqual(after.forms[0].contenido, before.forms[0].contenido);
        assert.equal(after.ilda.find(r => r.id_origen === '1').presente, false);
    });
    await check('Fallo de una fuente muestra Parcial y conserva la última copia ILDA íntegra', async () => {
        const before = (await report()).ilda;
        await control('sync-fail-ilda');
        await page.goto(origin + syncPath);
        await queue('ambas'); await control('sync-tick'); await finished('Parcial');
        const after = await report();
        assert.equal(after.runs[0].resultado.sii.estado, 'completada');
        assert.equal(after.runs[0].resultado.ilda.estado, 'fallida');
        assert.deepEqual(after.ilda, before);
        assert.equal((await page.locator('body').innerText()).includes('SECRET'), false);
        await control('sync-restore-source');
    });
    await check('Error SQL revierte publicación completa, muestra Con error y permite reintento', async () => {
        const before = (await report()).ilda;
        await control('sync-fail-sql');
        await queue('ilda'); await control('sync-tick'); await finished('Con error');
        assert.deepEqual((await report()).ilda, before);
        await control('sync-recover-sql');
        await queue('ilda'); await control('sync-tick'); await finished();
    });
    await check('SII distingue UR completada y módulos fallidos, muestra Parcial y permite reintento', async () => {
        await control('sync-fail-modules');
        await queue('sii'); await control('sync-tick'); await finished('Parcial');
        const run = (await report()).runs[0];
        assert.equal(run.resultado.sii.estado, 'completada');
        assert.equal(run.resultado.sii_modulos.estado, 'fallida');
        await expect(latestRow()).toContainText('SII · Módulos de SIIv2');
        await expect(latestRow()).toContainText('No se pudo sincronizar el catálogo de módulos de SIIv2');
        await control('sync-restore-source'); await queue('sii'); await control('sync-tick'); await finished();
    });
    await check('Programación diaria, hora y zona se persisten; otra pestaña no sobrescribe versión vigente', async () => {
        await page.getByLabel('Activar sincronización automática', { exact: true }).check();
        await page.getByRole('combobox', { name: 'Frecuencia', exact: true }).click();
        await page.getByRole('option', { name: 'Diariamente', exact: true }).click();
        await page.getByLabel('Hora local', { exact: true }).fill('09:30');
        await page.getByLabel('Incluir ILDA en la sincronización automática', { exact: true }).check();
        await saveSchedule();
        await page.goto(origin + syncPath);
        assert.equal(await page.getByLabel('Hora local', { exact: true }).inputValue(), '09:30');
        const other = await context.newPage();
        await other.goto(origin + syncPath);
        // Navigation returns the HTML shell; wait until the second tab has loaded its old props.
        assert.equal(await other.getByLabel('Hora local', { exact: true }).inputValue(), '09:30');
        await page.getByLabel('Hora local', { exact: true }).fill('10:30'); await saveSchedule();
        await other.getByLabel('Hora local', { exact: true }).fill('09:45'); await saveSchedule(other, 409);
        await other.getByRole('button', { name: 'Cargar programación vigente', exact: true }).waitFor();
        assert.equal(await other.getByLabel('Hora local', { exact: true }).inputValue(), '09:45');
        await other.close();
    });
    await check('Automática siempre incluye SII, admite ILDA optativa y no acumula intervalos vencidos', async () => {
        await control('sync-due'); await control('sync-tick'); await finished();
        assert.equal((await report()).runs[0].fuentes, 'ambas');
        assert.equal((await report()).runs[0].resultado.sii_modulos.estado, 'completada');
        assert.equal((await report()).runs[0].origen, 'programada');
        await page.getByLabel('Incluir ILDA en la sincronización automática', { exact: true }).uncheck();
        await saveSchedule();
        await control('sync-due'); await control('sync-tick'); await finished();
        assert.equal((await report()).runs[0].fuentes, 'sii');
        assert.equal((await report()).runs[0].resultado.sii_modulos.estado, 'completada');
        assert.equal((await report()).runs[0].resultado.ilda, undefined);
        const count = (await report()).runs.length;
        await control('sync-tick'); assert.equal((await report()).runs.length, count);
        await page.getByLabel('Activar sincronización automática', { exact: true }).uncheck();
        await saveSchedule(); await page.goto(origin + syncPath);
    });
    await check('Reserva interrumpida visible termina fallida y permite nueva ejecución manual', async () => {
        await queue('sii'); await control('sync-interrupted');
        await refresh(); await latestRow().getByText('En proceso', { exact: true }).waitFor();
        await control('sync-tick'); await finished('Con error');
        await queue('sii'); await control('sync-tick'); await finished();
    });
    await check('Revocar módulo Nexo bloquea pantalla y llamada directa; auditoría no contiene secretos', async () => {
        await control('sync-access-off');
        assert.equal((await page.goto(origin + syncPath)).status(), 403);
        const status = await page.evaluate(async syncPath => {
            const csrf = await (await fetch('/session/csrf')).json();
            return (await fetch(syncPath + '/ejecutar', { method: 'POST', headers: {
                Accept: 'application/json', 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own'
            }, body: JSON.stringify({ fuentes: 'sii' }) })).status;
        }, syncPath);
        assert.equal(status, 403);
        await control('sync-access-on');
        const final = await report();
        assert.ok(final.audits > 15); assert.equal(final.leaks, 0);
        assert.equal((await page.goto(origin + syncPath)).status(), 200);
        await page.screenshot({ path: path.join(artifacts, 'sincronizaciones-historial.png') });
    });
    await check('Encabezado adaptable, refresco conserva borrador y error temporal permite reintento', async () => {
        await page.getByRole('heading', { name: 'Sincronizaciones', exact: true }).waitFor();
        await page.setViewportSize({ width: 1440, height: 1000 });
        const title = await page.getByRole('heading', { name: 'Sincronizaciones', exact: true }).boundingBox();
        const action = await page.getByRole('button', { name: 'Actualizar estado', exact: true }).boundingBox();
        assert.ok(action.x > title.x && Math.abs(action.y - title.y) < 45);
        await page.screenshot({ path: path.join(artifacts, 'sincronizaciones-encabezado-escritorio.png') });
        await page.getByLabel('Zona horaria', { exact: true }).fill('UTC');
        await page.route('**/configuracion/sincronizaciones', async route => {
            if (route.request().resourceType() === 'xhr') return route.fulfill({ status: 503, contentType: 'application/json', body: JSON.stringify({ message: 'Fallo temporal sintético.' }) });
            return route.continue();
        });
        await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click();
        await page.getByText('Fallo temporal sintético.', { exact: true }).waitFor();
        assert.equal(await page.getByLabel('Zona horaria', { exact: true }).inputValue(), 'UTC');
        await page.unroute('**/configuracion/sincronizaciones');
        await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click();
        await page.getByText('Estado actualizado.', { exact: true }).waitFor();
        assert.equal(await page.getByLabel('Zona horaria', { exact: true }).inputValue(), 'UTC');
        await page.setViewportSize({ width: 390, height: 844 });
        await page.emulateMedia({ reducedMotion: 'reduce' });
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        assert.equal(await page.locator('.page-header').evaluate(el => getComputedStyle(el).animationName), 'none');
        await page.screenshot({ path: path.join(artifacts, 'sincronizaciones-encabezado-movil.png') });
    });
    await check('catálogo sin esquema muestra diagnóstico local y contador no disponible', async () => {
        await control('sync-schema-missing');
        try {
            await page.reload();
            await expect(page.getByText('Módulos de SIIv2: No disponible', { exact: true })).toBeVisible();
            await expect(page.getByText(/Falta public\.sii_modulos\./)).toBeVisible();
            await page.screenshot({ path: path.join(artifacts, 'sincronizaciones-esquema-ausente.png'), fullPage: true });
        } finally { await control('sync-schema-restore'); }
        await page.reload(); await expect(page.getByText('Módulos de SIIv2: No disponible', { exact: true })).toHaveCount(0);
    });
    assert.deepEqual(errors, []);
    await writeFile(path.join(artifacts, 'browser-sync.json'), JSON.stringify({
        utc: new Date().toISOString(), browser: browser.version(), checks, pageErrors: errors, externalFontsBlocked: true,
        identity: 'Microsoft HTTP and Nexo SQL simulated', sources: 'Synthetic ADO.NET readers; real mapping/publication; no SQL Server/MySQL wire'
    }, null, 2));
} catch (error) {
    console.error('BROWSER FAIL', error.stack);
    await page.screenshot({ path: path.join(artifacts, 'browser-sync-failure.png') }).catch(() => {});
    await writeFile(path.join(artifacts, 'browser-sync.json'), JSON.stringify({ checks, failed: true, error: error.message, pageErrors: errors }, null, 2));
    process.exitCode = 1;
} finally {
    await control('sync-worker-release').catch(() => {});
    await control('sync-recover-sql').catch(() => {});
    await browser.close();
}
