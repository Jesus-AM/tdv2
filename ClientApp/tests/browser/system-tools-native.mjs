import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Sólo host sintético aislado.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticSources: true, realPostgreSql: true, checks: [], errors: [] };
const check = async (name, work) => { await work(); report.checks.push({ name, passed: true }); console.log('NATIVE UI PASS ' + name); };
async function session() {
    const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 900 }, reducedMotion: 'reduce' });
    await context.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.origin !== origin && !(url.protocol === 'wss:' && url.host === new URL(origin).host)) return route.abort();
        if (url.pathname === '/connect') {
            const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
            if (headers.location?.startsWith('https://login.microsoftonline.com/')) headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
            return route.fulfill({ response, headers });
        }
        return route.continue();
    });
    const page = await context.newPage(); page.setDefaultTimeout(15000); page.on('pageerror', e => report.errors.push(e.message));
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio'); return { context, page };
}
const row = page => page.locator('[data-edit-block="sistemas:inicial"]');
const field = (page, label) => page.getByRole('combobox', { name: label + ' 1', exact: true });
async function systems(page) { await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); }
async function select(page, label, value) { await field(page, label).click(); await page.getByRole('option', { name: value, exact: true }).click(); }
async function finish(page) { await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); }
try {
    const first = await session(), { page } = first;
    const control = async action => { assert.equal((await first.context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } })).status(), 200); };
    const stored = async () => (await (await first.context.request.get(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } })).json()).contenido;
    await control('systems-seed'); await page.goto(origin + '/formatos/A'); await systems(page);
    await check('SII sin catálogo ni módulo admite borrador pendiente en PostgreSQL', async () => {
        await select(page, 'Sistema o herramienta', 'SIIv2'); await expect(page.getByText('El catálogo de módulos de SIIv2 aún no está disponible', { exact: true })).toBeVisible();
        await select(page, '¿Para qué se usa?', 'Consultar información'); await select(page, '¿Cómo funciona?', 'Funciona bien'); await finish(page);
        assert.equal((await stored()).sistemas[0].sistema, 'sii_v2'); assert.equal((await stored()).sistemas[0].moduloSiiId, '');
    });
    await check('publicación real local, búsqueda de duplicados, ID y descripción autorizados persisten', async () => {
        await control('systems-sync'); await page.reload(); await systems(page);
        const module = field(page, 'Módulo de SIIv2'); await module.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await module.fill('Consultas'); await expect(page.getByRole('option', { name: 'Consultas · ID 2', exact: true })).toBeVisible();
        await page.getByRole('option', { name: 'Consultas · ID 3', exact: true }).click(); await finish(page);
        assert.equal((await stored()).sistemas[0].moduloSiiId, '3'); assert.equal((await stored()).sistemas[0].moduloSiiDescripcion, 'Consultas');
        await page.reload(); await systems(page); await expect(field(page, 'Módulo de SIIv2')).toHaveValue('Consultas · ID 3');
    });
    await check('renombrar/retirar no cambia la respuesta; permite guardar comentarios históricos', async () => {
        const before = await stored(); await control('systems-rename'); assert.deepEqual(await stored(), before);
        await control('systems-remove'); assert.deepEqual(await stored(), before);
        await page.reload(); await systems(page); await expect(field(page, 'Módulo de SIIv2')).toHaveValue('Consultas · ID 3');
        await expect(page.getByText('Módulo histórico · Ya no está disponible para nuevas selecciones.', { exact: true })).toBeVisible();
        const comments = page.getByLabel('Fallas o comentarios 1', { exact: true }); await comments.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await comments.fill('Comentario con referencia histórica'); await finish(page); assert.equal((await stored()).sistemas[0].moduloSiiDescripcion, 'Consultas');
    });
    await check('Otra/Otro mantienen detalle al alternar controles y guardar en PostgreSQL', async () => {
        await select(page, 'Sistema o herramienta', 'Otra (escríbela)'); await page.getByLabel('Nombre de la herramienta 1', { exact: true }).fill('Herramienta local');
        await select(page, '¿Para qué se usa?', 'Otro (escríbelo)'); await page.getByLabel('Describe el uso 1', { exact: true }).fill('Uso particular');
        await select(page, 'Sistema o herramienta', 'Hoja de cálculo (Excel)'); await select(page, 'Sistema o herramienta', 'Otra (escríbela)');
        await expect(page.getByLabel('Nombre de la herramienta 1', { exact: true })).toHaveValue('Herramienta local'); await finish(page);
        assert.equal((await stored()).sistemas[0].sistemaOtro, 'Herramienta local'); assert.equal((await stored()).sistemas[0].usoOtro, 'Uso particular');
    });
    await check('dos sesiones y SignalR: reserva, ayuda, autoguardado y liberación', async () => {
        const other = await session(); await other.page.goto(origin + '/formatos/A'); await systems(other.page);
        await field(page, 'Sistema o herramienta').click(); await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
        await expect(row(other.page)).toHaveAttribute('data-edit-state', 'occupied'); await field(other.page, 'Sistema o herramienta').click(); await expect(other.page.getByRole('listbox')).toHaveCount(0);
        const help = page.getByRole('button', { name: 'Ayuda: ¿Cómo funciona?', exact: true }); await help.click(); await page.getByRole('button', { name: 'Cerrar ayuda', exact: true }).click(); await expect(help).toBeFocused();
        await expect(row(page)).toHaveAttribute('data-edit-state', 'owned'); await select(page, '¿Cómo funciona?', 'Funciona con fallas');
        await expect.poll(async () => (await stored()).sistemas[0].estado).toBe('fallas'); await finish(page);
        await expect(row(other.page)).toHaveAttribute('data-edit-state', 'idle'); await expect(field(other.page, '¿Cómo funciona?')).toHaveText('Funciona con fallas'); await other.context.close();
    });
    await check('varias herramientas del mismo procedimiento en filas independientes', async () => {
        await page.getByRole('button', { name: 'Agregar fila', exact: true }).click();
        await expect(page.locator('[data-edit-block^="sistemas:"]')).toHaveCount(2); await finish(page);
        await page.getByRole('combobox', { name: 'Procedimiento 2', exact: true }).click(); await page.getByRole('option', { name: /PO-01/, exact: false }).click();
        await page.getByRole('combobox', { name: 'Sistema o herramienta 2', exact: true }).click(); await page.getByRole('option', { name: 'Hoja de cálculo (Excel)', exact: true }).click(); await finish(page);
        const rows = (await stored()).sistemas; assert.equal(rows.length, 2); assert.ok(rows.every(r => r.proceso === 'PO-01'));
        assert.equal(rows[0].sistema, 'otra'); assert.equal(rows[1].sistema, 'excel');
    });
    await check('móvil y teclado con backend real, enviado conserva consulta', async () => {
        await page.setViewportSize({ width: 360, height: 900 }); await field(page, 'Sistema o herramienta').focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape'); await finish(page);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' })); await page.screenshot({ path: path.join(artifacts, 'systems-native-mobile.png'), fullPage: true });
        await control('recipients-submitted'); await page.reload(); await systems(page); await field(page, 'Sistema o herramienta').click(); await expect(page.getByRole('listbox')).toHaveCount(0);
        await expect(page.getByLabel('Fallas o comentarios 1', { exact: true })).toHaveJSProperty('readOnly', true);
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally {
    await writeFile(path.join(artifacts, 'systems-native-browser.json'), JSON.stringify(report, null, 2)); await browser.close();
}
