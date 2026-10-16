// React/MUI -> ASP.NET -> PostgreSQL aislado. Identidad sintética; ninguna fuente institucional.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Sólo host aislado.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticIdentity: true, realPostgreSql: true, checks: [], errors: [] };
const check = async (name, fn) => { await fn(); report.checks.push({ name, passed: true }); console.log('NATIVE UI PASS ' + name); };
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 900 }, hasTouch: true });
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const page = await context.newPage(); page.setDefaultTimeout(15000); page.on('pageerror', e => report.errors.push(e.message));
const control = async action => assert.equal((await context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } })).status(), 200);
const stored = async () => (await (await context.request.get(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } })).json()).contenido;
const systems = () => page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
const review = () => page.getByRole('tab', { name: 'Revisar y enviar', exact: true }).click();
const finish = async () => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); };
const select = async (label, value) => { await page.getByRole('combobox', { name: label, exact: true }).click(); await page.getByRole('option', { name: value, exact: true }).click(); };
try {
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio'); await control('coverage-seed');
    await page.goto(origin + '/formatos/A');
    await check('dos procedimientos y una herramienta: pendiente real, revisión y navegación sin filas ficticias', async () => {
        await systems(); await expect(page.getByText('Procedimientos y captura de herramientas', { exact: true })).toHaveCount(0);
        await expect(page.getByRole('region', { name: 'Cobertura de procedimientos', exact: true })).toHaveCount(0);
        assert.equal((await stored()).sistemas.length, 1); assert.equal((await stored()).versionEstructura, undefined);
        await review(); await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toBeDisabled();
        const add = page.locator('[data-pending-block="cobertura:segundo"]').getByRole('button', { name: 'Agregar registro' });
        await add.focus(); await page.keyboard.press('Enter');
        await expect(page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true })).toHaveAttribute('aria-selected', 'true');
        await expect(page.locator('[data-edit-block^="sistemas:"]')).toHaveCount(2);
        await expect(page.getByRole('combobox', { name: 'Procedimiento 2', exact: true })).toBeFocused();
        await finish(); assert.equal((await stored()).sistemas[1].proceso, 'PO-02');
    });
    await check('captura dirigida conserva pendiente de campos, completa cobertura y persiste al recargar', async () => {
        await select('Sistema o herramienta 2', 'Hoja de cálculo (Excel)');
        await select('¿Para qué se usa? 2', 'Consultar información'); await select('¿Cómo funciona? 2', 'Funciona bien');
        await finish(); await review(); await expect(page.locator('[data-pending-block="cobertura:segundo"]')).toHaveCount(0);
        assert.equal((await stored()).versionEstructura, 1);
        await page.reload(); await systems(); await expect(page.getByRole('combobox', { name: 'Sistema o herramienta 2', exact: true })).toHaveText('Hoja de cálculo (Excel)');
        await review(); await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toBeEnabled();
    });
    await check('última herramienta eliminada restaura pendiente y capturar de nuevo no duplica filas existentes', async () => {
        await systems(); const rows = page.locator('[data-edit-block^="sistemas:"]');
        await rows.nth(1).getByRole('button', { name: 'Eliminar registro', exact: true }).click();
        await page.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click();
        await expect(rows).toHaveCount(1); await review();
        await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toBeDisabled();
        await page.locator('[data-pending-block="cobertura:segundo"]').getByRole('button', { name: 'Agregar registro' }).click();
        await expect(rows).toHaveCount(2); await finish(); await review();
        await page.locator('[data-pending-block="cobertura:segundo"]').getByRole('button', { name: 'Ir al campo' }).click();
        await expect(page.getByRole('combobox', { name: 'Sistema o herramienta 2', exact: true })).toBeFocused();
        await finish();
        assert.equal((await stored()).sistemas.length, 2);
        await select('Sistema o herramienta 2', 'Hoja de cálculo (Excel)');
        await select('¿Para qué se usa? 2', 'Consultar información'); await select('¿Cómo funciona? 2', 'Funciona bien'); await finish();
    });
    await check('eliminar procedimiento informa cantidad, conserva respuesta y permite reasignar', async () => {
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        await page.locator('[data-edit-block="identificacion:inicial"]').getByRole('button', { name: 'Eliminar registro', exact: true }).click();
        await expect(page.getByRole('dialog')).toContainText('1 registro de sistemas/herramientas o datos perderá su asociación');
        await expect(page.getByRole('dialog')).toContainText('Las respuestas se conservarán');
        await page.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click();
        await expect(page.locator('[data-edit-block="identificacion:inicial"]')).toHaveCount(0);
        await systems(); const first = page.locator('[data-edit-block="sistemas:inicial"]');
        await expect(first).toContainText('Sin procedimiento asociado');
        assert.equal((await stored()).sistemas[0].fallas, 'Respuesta que se conserva al reasignar');
        await first.getByRole('button', { name: 'Asignar procedimiento', exact: true }).click();
        await expect(page.getByRole('combobox', { name: 'Procedimiento 1', exact: true })).toBeFocused();
        await page.getByRole('option', { name: /PO-02/ }).click(); await finish();
        assert.equal((await stored()).sistemas[0].proceso, 'PO-02');
    });
    await check('nombres completos, móvil emulado, teclado y consulta sin permiso de edición', async () => {
        for (const width of [1366, 390, 320]) {
            await page.setViewportSize({ width, height: 900 });
            const table = page.locator('.systems-table');
            await expect(table.locator('.procedure-value').first()).toContainText('comprobar la distribución en dispositivos móviles');
            await expect(page.getByText('Procedimientos y captura de herramientas', { exact: true })).toHaveCount(0);
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            await table.locator('.procedure-value').first().scrollIntoViewIfNeeded();
            await page.screenshot({ path: path.join(artifacts, `coverage-${width}.png`), fullPage: true });
        }
        await review(); await page.getByRole('button', { name: 'Enviar formato', exact: true }).click();
        await expect(page.getByRole('dialog')).toBeVisible();
        await page.getByRole('dialog').getByRole('button', { name: 'Confirmar envío', exact: true }).click();
        await expect(page.getByText('Formato enviado', { exact: true })).toBeVisible();
        await systems(); await expect(page.getByRole('button', { name: 'Agregar fila', exact: true })).toBeDisabled();
        assert.equal((await stored()).sistemas.length, 2);
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally {
    await writeFile(path.join(artifacts, 'coverage-native-browser.json'), JSON.stringify(report, null, 2));
    await context.close(); await browser.close();
}
