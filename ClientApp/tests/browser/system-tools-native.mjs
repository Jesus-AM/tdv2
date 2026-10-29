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
    const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 900 }, reducedMotion: 'reduce', hasTouch: true });
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
    await page.goto(origin + '/connect?returnUrl=/inicio'); await page.waitForURL(origin + '/inicio'); return { context, page };
}
const row = page => page.locator('[data-edit-block="sistemas:inicial"]');
const field = (page, label) => page.getByRole('combobox', { name: label + ' 1', exact: true });
async function systems(page) { await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); }
async function select(page, label, value) { await field(page, label).click(); await page.getByRole('option', { name: value, exact: true }).click(); }
async function finish(page) { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); }
try {
    const first = await session(), { page } = first;
    const control = async action => { assert.equal((await first.context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } })).status(), 200); };
    const stored = async () => (await (await first.context.request.get(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } })).json()).contenido;
    await control('systems-seed'); await page.goto(origin + '/formatos/A'); await systems(page);
    await check('SII sin catálogo ni módulo admite borrador pendiente en PostgreSQL', async () => {
        await select(page, 'Sistema o herramienta', 'SIIv2'); await expect(page.getByText('El catálogo de módulos de SIIv2 aún no está disponible', { exact: true })).toBeVisible();
        await select(page, '¿Para qué se usa?', 'Consultar información'); await select(page, '¿Cómo funciona?', 'Funciona bien'); await finish(page);
        assert.equal((await stored()).sistemas[0].sistema, 'sii_v2'); assert.equal((await stored()).sistemas[0].moduloSiiId, '');
        await page.reload(); await systems(page);
        await expect(field(page, 'Módulos de SIIv2')).toHaveAttribute('placeholder', 'Selecciona módulos');
        await expect(row(page).locator('.sii-module-value')).toHaveCount(0);
        await page.getByRole('tab', { name: 'Revisar y enviar', exact: true }).click();
        const pending = page.locator('[data-pending-block="sistemas:inicial"][data-pending-field="modulosSii"]');
        await expect(pending).toContainText('Módulos de SIIv2'); await pending.getByRole('button', { name: 'Ir al campo' }).click();
        await expect(field(page, 'Módulos de SIIv2')).toBeFocused(); await finish(page);
    });
    await check('publicación real local, búsqueda de duplicados, ID y descripción autorizados persisten', async () => {
        await control('systems-sync'); await page.reload(); await systems(page);
        const module = field(page, 'Módulos de SIIv2'); await module.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await module.fill('Consultas'); await expect(page.getByRole('option', { name: 'Consultas', exact: true })).toHaveCount(2);
        await page.getByRole('option', { name: 'Consultas', exact: true }).nth(1).click();
        await expect(module).toHaveAttribute('aria-expanded', 'true');
        await finish(page); await page.reload(); await systems(page);
        await expect(row(page).locator('.sii-module-value')).toHaveText('Consultas');
        assert.deepEqual((await stored()).sistemas[0].modulosSii.map(m => m.id), ['3']);
        await module.click();
        await module.fill('Consultas'); await page.getByRole('option', { name: 'Consultas', exact: true }).nth(0).click();
        await expect.poll(async () => (await stored()).sistemas[0].modulosSii?.map(m => m.id)).toEqual(['3', '2']);
        await expect(module).toHaveAttribute('aria-expanded', 'true'); await expect(module).toBeFocused();
        await finish(page);
        assert.equal((await stored()).sistemas[0].modulosSii[0].id, '3'); assert.equal((await stored()).sistemas[0].modulosSii[0].descripcion, 'Consultas');
        await page.reload(); await systems(page); await expect(row(page).locator('.sii-module-value')).toHaveText('2 módulos seleccionados');
        await page.getByRole('tab', { name: 'Revisar y enviar', exact: true }).click();
        await expect(page.locator('[data-pending-field="modulosSii"]')).toHaveCount(0); await systems(page);
    });
    await check('renombrar/retirar no cambia la respuesta; permite guardar comentarios históricos', async () => {
        const before = await stored(); await control('systems-rename'); assert.deepEqual(await stored(), before);
        await control('systems-remove'); assert.deepEqual(await stored(), before);
        await page.reload(); await systems(page); await expect(row(page).locator('.sii-module-value')).toHaveText('2 módulos seleccionados');
        await expect(page.getByText('La selección incluye módulos históricos que ya no están disponibles para nuevas selecciones.', { exact: true })).toBeVisible();
        const comments = page.getByLabel('Áreas de oportunidad 1', { exact: true }); await comments.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await comments.fill('Comentario con referencia histórica'); await finish(page); assert.equal((await stored()).sistemas[0].modulosSii[0].descripcion, 'Consultas');
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
    await control('applications-names'); await page.reload(); await systems(page);
    for (const width of [1366, 360]) for (const [label, collection] of [['Aplicaciones móviles', 'aplicacionesMoviles'], ['Aplicaciones Web', 'aplicacionesWeb']]) {
        await check(`${label} ${width}px: cero/una/varias, nombres completos, búsqueda, autoguardado y recarga nativos`, async () => {
            await page.setViewportSize({ width, height: 900 });
            await select(page, 'Sistema o herramienta', label); await finish(page); await page.reload(); await systems(page);
            const application = field(page, label);
            await expect(application).toHaveAttribute('placeholder', 'Selecciona aplicaciones');
            await expect(row(page).locator('.sii-module-value')).toHaveCount(0);
            // Cambiar de fila antes de abrir exige adquisición automática en la primera interacción.
            const other = page.getByLabel('Áreas de oportunidad 2', { exact: true }); await other.click();
            if (width === 360) await application.tap(); else await application.click();
            await expect(page.getByRole('listbox')).toBeVisible();
            await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
            const longName = 'Aplicación para atención, consulta y seguimiento de solicitudes institucionales con un nombre completo suficientemente largo para comprobar varias líneas sin truncamiento';
            await application.fill('seguimiento'); await page.getByRole('option', { name: longName, exact: true }).click();
            await expect(application).toHaveAttribute('aria-expanded', 'true');
            await expect.poll(async () => (await stored()).sistemas[0][collection]?.map(a => a.descripcion)).toEqual([longName]);
            assert.match((await stored()).sistemas[0][collection][0].id, /^local:/);
            await expect(application).toBeFocused(); await expect(application).toHaveAttribute('aria-expanded', 'true');
            await finish(page); await page.reload(); await systems(page);
            await expect(row(page).locator('.sii-module-value')).toHaveText(longName);
            assert.equal((await stored()).sistemas[0][collection][0].descripcion, longName);
            await application.click(); await application.fill('reportes');
            await page.getByRole('option', { name: 'Aplicación de reportes', exact: true }).click();
            await expect.poll(async () => (await stored()).sistemas[0][collection]?.length).toBe(2);
            await expect(application).toHaveAttribute('aria-expanded', 'true'); await expect(application).toBeFocused();
            await page.keyboard.press('Escape'); await finish(page); await page.reload(); await systems(page);
            await expect(row(page).locator('.sii-module-value')).toHaveText('2 aplicaciones seleccionadas');
            await expect(row(page).locator('.MuiChip-root')).toHaveCount(0);
            await application.click(); await application.fill('reportes');
            await expect(page.getByRole('option', { name: 'Aplicación de reportes', exact: true })).toHaveAttribute('aria-selected', 'true');
            await application.press('ArrowDown'); await application.press('Enter'); await page.keyboard.press('Escape');
            await finish(page); await page.reload(); await systems(page);
            await expect(row(page).locator('.sii-module-value')).toHaveText(longName);
            await application.scrollIntoViewIfNeeded();
            const size = await row(page).locator('.sii-module-value').evaluate(el => ({ height: el.clientHeight, scroll: el.scrollHeight }));
            assert.ok(size.height > 30 && size.scroll <= size.height + 1);
            await page.screenshot({ path: path.join(artifacts, `${collection}-${width}-single.png`), fullPage: true });
            await application.click(); await application.fill('seguimiento'); await page.getByRole('option', { name: longName, exact: true }).click();
            await finish(page); await page.reload(); await systems(page);
            await expect(application).toHaveAttribute('placeholder', 'Selecciona aplicaciones');
            assert.deepEqual((await stored()).sistemas[0][collection], []);
            await page.getByRole('tab', { name: 'Revisar y enviar', exact: true }).click();
            await page.locator(`[data-pending-field="${collection}"]`).getByRole('button', { name: 'Ir al campo' }).click();
            await expect(application).toBeFocused(); await finish(page);
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        });
    }
    await check('móvil y teclado con backend real, enviado conserva consulta', async () => {
        await page.setViewportSize({ width: 360, height: 900 }); await field(page, 'Sistema o herramienta').focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape'); await finish(page);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' })); await page.screenshot({ path: path.join(artifacts, 'systems-native-mobile.png'), fullPage: true });
        await control('recipients-submitted'); await page.reload(); await systems(page); await field(page, 'Sistema o herramienta').click(); await expect(page.getByRole('listbox')).toHaveCount(0);
        await expect(page.getByLabel('Áreas de oportunidad 1', { exact: true })).toHaveJSProperty('readOnly', true);
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally {
    await writeFile(path.join(artifacts, 'systems-native-browser.json'), JSON.stringify(report, null, 2)); await browser.close();
}
