import assert from 'node:assert/strict';
import { chromium, expect as baseExpect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw Error('Only isolated synthetic host');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const expect = baseExpect.configure({ timeout: 20000 });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', hasTouch: true });
const report = { realPostgreSql: true, realAspNet: true, mockedEditingTransport: false, microsoft: 'synthetic HTTP peer', mobile: 'emulated', checks: [], errors: [] };
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
const page = await context.newPage(); page.setDefaultTimeout(20000); page.on('pageerror', e => report.errors.push(e.message));
const check = async (name, work) => { await work(); report.checks.push(name); console.log('VALIDATION PASS ' + name); };
async function control(action) { const r = await context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } }); assert.equal(r.status(), 200); return r; }
const stored = async () => (await (await control('stored')).json()).contenido;
const row = id => page.locator(`[data-edit-block="identificacion:${id}"]`);
const select = n => page.getByRole('combobox', { name: 'Validación ' + n, exact: true });
async function header() { await page.getByRole('tab', { name: 'Identificación general', exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); }
async function finish() { await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); }
async function options(n) { await select(n).click(); await expect(page.getByRole('option')).toHaveText(['V · Vigente', 'A · Ajustar']); }
async function showValidation(n) {
    // Desplaza la tabla como el usuario: el campo debe quedar a la derecha de Edición, que es fija.
    await select(n).evaluate(e => { const table = e.closest('.capture-scroll'), field = e.getBoundingClientRect();
        table.scrollLeft += field.left - table.getBoundingClientRect().left - 140; });
}
async function live() { return page.evaluate(async () => (await fetch('/formatos/A/estado?tab=' + crypto.randomUUID(), { headers: { 'X-TDV2-Context': 'own' } })).json()); }
try {
    await check('portal: texto completo y alineado a 1440, 390 y 320 px; acceso conserva autenticación', async () => {
        await page.goto(origin);
        const link = page.getByRole('link', { name: 'Registrar un procedimiento institucional', exact: true });
        await expect(link).toHaveAttribute('href', '/connect');
        for (const width of [1440, 390, 320]) {
            await page.setViewportSize({ width, height: width === 1440 ? 1000 : 844 }); await link.scrollIntoViewIfNeeded();
            const layout = await link.evaluate(e => {
                const a = e.getBoundingClientRect(), text = e.firstElementChild.getBoundingClientRect();
                return { left: a.left, right: a.right, textLeft: text.left, textRight: text.right, textHeight: text.height, height: a.height,
                    overflows: document.documentElement.scrollWidth > innerWidth + 1, align: getComputedStyle(e).alignItems };
            });
            assert.ok(layout.left >= 0 && layout.right <= width && layout.textLeft >= layout.left && layout.textRight <= layout.right);
            assert.ok(layout.textHeight <= layout.height && !layout.overflows); assert.equal(layout.align, 'center');
            await page.screenshot({ path: path.join(artifacts, `validation-portal-${width}.png`) });
        }
        await link.click(); await page.waitForURL(origin + '/inicio');
        await page.goto(origin); await expect(link).toHaveAttribute('href', '/inicio'); await link.click(); await page.waitForURL(origin + '/inicio');
        await page.setViewportSize({ width: 1440, height: 1000 }); await page.goto(origin + '/formatos/A'); await header();
    });
    await check('selector vacío sin opción indicativa, sólo V/A; reserva, guardado y recarga', async () => {
        await expect(select(1)).toHaveText('Selecciona una opción'); await options(1);
        await expect(row('inicial')).toHaveAttribute('data-edit-state', 'owned');
        await page.getByRole('option', { name: 'V · Vigente', exact: true }).click(); await finish();
        assert.equal((await stored()).identificacion[0].validacion, 'V'); assert.equal((await stored()).identificacion[0].codigo, 'PO-01');
        await page.reload(); await header(); await expect(select(1)).toHaveText('V · Vigente');
    });
    await check('D/N anteriores legibles y pendientes; otros campos de la misma fila se autoguardan sin convertirlos', async () => {
        await control('validation-history'); await page.reload(); await header();
        await expect(select(1)).toHaveText('D · Duplicado o relacionado'); await expect(select(2)).toHaveText('N · No corresponde');
        await expect(page.getByText('Validación pendiente de actualizar.', { exact: true })).toHaveCount(2);
        const initial = await stored();
        for (const [n, id] of [[1, 'inicial'], [2, 'segundo']]) {
            const field = page.getByLabel('¿Qué entrega? ' + n, { exact: true }); await field.click(); await expect(row(id)).toHaveAttribute('data-edit-state', 'owned');
            await field.fill('Entrega histórica modificada ' + n);
            await expect.poll(async () => (await stored()).identificacion[n - 1].resultado).toBe('Entrega histórica modificada ' + n);
            await page.getByRole('tab', { name: 'Identificación general', exact: true }).focus(); await expect(row(id)).toHaveAttribute('data-edit-state', 'idle');
        }
        const after = await stored(); assert.deepEqual(after.identificacion.map(r => r.validacion), ['D', 'N']);
        for (const section of ['sistemas', 'datos', 'evaluaciones', 'preguntas', 'acuerdos']) assert.deepEqual(after[section], initial[section]);
        const state = await live(); assert.equal(state.revisionEnvio.listo, false); assert.ok(state.porcentajeEtapa < 100); assert.deepEqual(state.procedimientosDisponibles, []);
        await page.reload(); await header(); await expect(select(1)).toHaveText('D · Duplicado o relacionado');
        await showValidation(1);
        await page.screenshot({ path: path.join(artifacts, 'validation-historical-desktop.png'), fullPage: true });
    });
    await check('históricos se corrigen a A/V; menú sin D/N, teclado, móvil y pendientes actualizados', async () => {
        await options(1); await page.getByRole('option', { name: 'A · Ajustar', exact: true }).click(); await finish();
        await page.setViewportSize({ width: 390, height: 844 }); await select(2).focus(); await page.keyboard.press('Enter');
        await expect(page.getByRole('option')).toHaveText(['V · Vigente', 'A · Ajustar']);
        await page.keyboard.press('Home'); await page.keyboard.press('Enter'); await finish();
        assert.deepEqual((await stored()).identificacion.map(r => r.validacion), ['A', 'V']);
        assert.equal((await live()).revisionEnvio.listo, true); assert.equal((await live()).porcentajeEtapa, 100);
        await page.reload(); await header(); await expect(select(1)).toHaveText('A · Ajustar'); await expect(select(2)).toHaveText('V · Vigente');
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        await select(2).scrollIntoViewIfNeeded(); await showValidation(2); await page.screenshot({ path: path.join(artifacts, 'validation-mobile.png') }); await finish();
    });
    await check('eliminación directa de N desde fila libre conserva los demás datos', async () => {
        await control('validation-history'); await page.reload(); await header();
        await expect(row('segundo')).toHaveAttribute('data-edit-state', 'idle');
        await row('segundo').getByRole('button', { name: 'Eliminar registro', exact: true }).click();
        await page.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click(); await expect(row('segundo')).toHaveCount(0);
        assert.equal((await stored()).identificacion[0].validacion, 'D');
    });
    await check('formato enviado mantiene las etiquetas históricas, sin edición ni aviso de corrección', async () => {
        await control('validation-history'); await control('recipients-submitted'); await page.reload(); await header();
        const before = await stored();
        await expect(select(1)).toHaveText('D · Duplicado o relacionado'); await expect(select(2)).toHaveText('N · No corresponde');
        await select(2).click(); await expect(page.getByRole('option')).toHaveCount(0);
        await expect(page.getByText('Validación pendiente de actualizar.', { exact: true })).toHaveCount(0);
        await expect(row('inicial').getByRole('button', { name: 'Eliminar registro', exact: true })).toBeDisabled(); assert.deepEqual(await stored(), before);
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (e) { report.failure = e.message; await page.screenshot({ path: path.join(artifacts, 'validation-failure.png'), fullPage: true }).catch(() => {}); throw e; }
finally { await writeFile(path.join(artifacts, 'procedure-validation-browser.json'), JSON.stringify(report, null, 2)); await browser.close(); }
