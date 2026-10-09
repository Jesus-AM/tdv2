import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';

const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Requires isolated native host.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
const checks = [], errors = [];
let posts = 0, holdAck = null, loseAck = false, failReads = false;
const syncPath = '/configuracion/sincronizaciones';
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            const target = new URL(headers.location);
            headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(target.searchParams.get('state'))}`;
        }
        return route.fulfill({ response, headers });
    }
    if (url.pathname === syncPath + '/ejecutar') {
        posts++;
        const response = await route.fetch();
        if (holdAck) await holdAck;
        if (loseAck) return route.abort();
        return route.fulfill({ response });
    }
    if (url.pathname === syncPath && route.request().headers()['x-tdv2-page'] && failReads) return route.abort();
    return route.continue();
});
const control = async action => {
    const response = await context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } });
    assert.equal(response.status(), 200); return response;
};
let page = await context.newPage();
const observe = target => { target.setDefaultTimeout(15000); target.on('pageerror', e => errors.push(e.message)); };
observe(page);
const check = async (name, action) => { await action(); checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name); };
const row = () => page.getByRole('table', { name: 'Historial de sincronizaciones' }).getByRole('row').nth(1);
const running = () => expect(page.getByText('Sincronizando… ILDA', { exact: true })).toBeVisible();
const complete = () => expect(row()).toContainText('Completada');
try {
    await page.goto(origin + '/connect'); await page.goto(origin + syncPath);
    await check('inicio desde el web con horarios pausados; ACK real demorado y doble clic', async () => {
        await expect(page.getByLabel('Activar sincronización automática')).not.toBeChecked();
        await control('sync-hold');
        let release; holdAck = new Promise(resolve => { release = resolve; });
        const before = posts;
        await page.getByRole('button', { name: 'Sincronizar SII e ILDA ahora', exact: true }).evaluate(button => { button.click(); button.click(); });
        await expect(page.getByRole('button', { name: 'Iniciando…', exact: true }).first()).toBeVisible();
        await expect(page.getByText('Sincronizando… ILDA', { exact: true })).toHaveCount(0);
        await expect.poll(() => posts).toBe(before + 1);
        release(); holdAck = null;
        await running(); await expect(page.getByLabel('Activar sincronización automática')).not.toBeChecked();
        const report = await (await control('sync-report')).json(); assert.equal(report.runs.length, 1);
        assert.equal(report.runs[0].estado, 'ejecutando');
        await page.screenshot({ path: path.join(artifacts, 'sync-manual-desktop.png'), fullPage: true });
    });
    await check('recarga, navegación y cierre de página conservan el trabajo y muestran su estado real', async () => {
        await page.reload(); await running();
        await page.getByRole('link', { name: 'Configuración', exact: true }).click();
        await expect(page.getByRole('heading', { name: 'Configuración', exact: true })).toBeVisible();
        await page.goto(origin + syncPath); await running();
        await page.close();
        page = await context.newPage(); observe(page); await page.goto(origin + syncPath); await running();
        await control('sync-worker-release'); await complete();
        await expect(row()).toContainText('SII · Unidades responsables: 4 registros');
        await expect(row()).toContainText('SII · Módulos de SIIv2: 5 registros');
        await expect(row()).toContainText('ILDA: 5 registros');
        assert.equal(posts, 1);
    });
    await check('fallo de módulos muestra Parcial, conserva resultados independientes y permite reintentar', async () => {
        await control('sync-fail-modules');
        await page.getByRole('button', { name: 'Sincronizar SII e ILDA ahora', exact: true }).click();
        await expect(row()).toContainText('Parcial');
        await expect(row()).toContainText('No se pudo sincronizar el catálogo de módulos de SIIv2');
        await expect(row()).toContainText('ILDA: 5 registros');
        await control('sync-restore-source');
        await page.getByRole('button', { name: 'Sincronizar SII', exact: true }).click();
        await complete();
    });
    await check('fallo al reclamar explica el motivo y el mismo trabajo se recupera sin duplicarse', async () => {
        await control('sync-claim-fail');
        await page.getByRole('button', { name: 'Sincronizar SII', exact: true }).click();
        await expect(page.getByText(/No se pudo acceder a la cola local/)).toBeVisible();
        const before = (await (await control('sync-report')).json()).runs.length;
        await expect(page.getByRole('button', { name: 'Iniciando…', exact: true }).first()).toBeDisabled();
        await control('sync-claim-recover'); await complete();
        assert.equal((await (await control('sync-report')).json()).runs.length, before);
    });
    await check('ACK y lectura perdidos conservan la solicitud y bloquean otro envío hasta confirmar', async () => {
        await control('sync-hold'); loseAck = true; failReads = true;
        await page.getByRole('button', { name: 'Sincronizar SII e ILDA ahora', exact: true }).click();
        await expect(page.getByText(/No se pudo confirmar el estado de la solicitud/)).toBeVisible();
        await expect(page.getByRole('button', { name: 'Sincronizar SII e ILDA ahora', exact: true })).toBeDisabled();
        loseAck = false; failReads = false; await running();
        await control('sync-worker-release'); await complete();
    });
    await check('móvil conserva progreso, botones bloqueados y resultado sin desbordamiento', async () => {
        await page.setViewportSize({ width: 390, height: 844 });
        await control('sync-hold');
        await page.getByRole('button', { name: 'Sincronizar ILDA', exact: true }).click();
        await running();
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await page.getByRole('heading', { name: 'Sincronizaciones', exact: true }).click();
        await page.evaluate(() => window.scrollTo(0, 0));
        await page.screenshot({ path: path.join(artifacts, 'sync-manual-mobile.png'), fullPage: true });
        await control('sync-worker-release'); await complete();
    });
    assert.deepEqual(errors, []);
} finally {
    await writeFile(path.join(artifacts, 'browser-sync-manual.json'), JSON.stringify({ realAspNet: true, realPostgreSql: true,
        realHostedWorker: true, externalSources: 'synthetic ADO.NET', delayedOrLostHttp: 'only the ACK/network-failure scenarios', checks, errors }, null, 2));
    await browser.close();
}
