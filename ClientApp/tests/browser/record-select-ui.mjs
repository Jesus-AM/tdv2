// React/MUI reales; HTTP, identidad y reservas sintéticos. No utiliza ASP.NET ni bases.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';

const artifacts = path.resolve('../.artifacts/record-select-ui'); await mkdir(artifacts, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticTransport: true, realPostgreSql: false, checks: [], errors: [] };
const gate = () => { let resolve; const promise = new Promise(r => { resolve = r; }); return { promise, resolve }; };
const row = (page, id) => page.locator(`[data-edit-block="sistemas:${id}"]`);
const module = (page, id) => row(page, id).locator('[data-field="modulosSii"]');
async function setup(width = 1366) {
    const fixture = await systemFixture();
    fixture.catalog = { estado: 'disponible', modulos: [
        { id: '11', descripcion: 'Administración de solicitudes académicas y servicios institucionales' },
        { id: '22', descripcion: 'Seguimiento de solicitudes' },
    ] };
    Object.assign(fixture.content.sistemas[0], { sistema: 'sii_v2', moduloSiiId: '11', moduloSiiDescripcion: fixture.catalog.modulos[0].descripcion });
    fixture.content.sistemas.push({ ...fixture.content.sistemas[0], id: 'segundo' });
    const context = await browser.newContext({ viewport: { width, height: 900 }, hasTouch: true });
    await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
    await fixture.webSockets(context); const page = await context.newPage(); page.setDefaultTimeout(8000);
    page.on('pageerror', error => report.errors.push(error.message));
    await page.goto(fixture.origin + '/formatos/A'); await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
    await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0);
    return { fixture, page, context, close: async () => { await context.close(); await fixture.close(); } };
}
async function check(name, work, width = 1366) {
    const test = await setup(width);
    try { await work(test); report.checks.push({ name, passed: true }); console.log('UI PASS ' + name); }
    finally {
        await test.page.screenshot({ path: path.join(artifacts, `${report.checks.length}.png`), fullPage: true }).catch(() => {});
        report.lastText = await test.page.locator('body').innerText().catch(() => ''); await test.close();
    }
}
try {
    await check('primer clic tras otra fila abre y mantiene foco durante reserva lenta', async ({ page, fixture }) => {
        await row(page, 'inicial').getByLabel('Áreas de oportunidad 1', { exact: true }).click();
        await expect(row(page, 'inicial')).toHaveAttribute('data-edit-state', 'owned');
        await row(page, 'inicial').getByLabel('Áreas de oportunidad 1', { exact: true }).fill('Cambio que debe guardarse al salir');
        const arrived = gate(), release = gate();
        await page.route('**/reservas', async route => {
            if (route.request().postDataJSON().blocks.some(b => b.key === 'sistemas:segundo')) { arrived.resolve(); await release.promise; }
            await route.continue();
        });
        try {
            await module(page, 'segundo').click(); await arrived.promise;
            // Debe abrirse antes de recibir la autorización, sin volver a pulsar.
            await expect(page.locator('.MuiAutocomplete-popper').getByRole('status')).toHaveText('Preparando edición…');
            await expect(module(page, 'segundo')).toHaveAttribute('aria-busy', 'true');
            await page.locator('.MuiAutocomplete-popper').getByRole('status').click();
            await expect(module(page, 'segundo')).toBeFocused();
            await expect(page.locator('.MuiAutocomplete-popper')).toBeVisible();
            await expect(page.getByRole('option')).toHaveCount(0);
            const control = await module(page, 'segundo').elementHandle();
            await expect(row(page, 'segundo').locator('.MuiAutocomplete-clearIndicator')).toBeDisabled();
            await module(page, 'segundo').press('ArrowDown'); await module(page, 'segundo').press('Enter');
            assert.equal(fixture.content.sistemas[1].moduloSiiId, '11');
            await page.screenshot({ path: path.join(artifacts, 'pending-reservation.png'), fullPage: true });
            release.resolve(); await expect(row(page, 'segundo')).toHaveAttribute('data-edit-state', 'owned');
            await expect(page.getByRole('option', { name: 'Seguimiento de solicitudes', exact: true })).toBeVisible();
            await expect(module(page, 'segundo')).toBeFocused();
            assert.equal(await control.evaluate(el => el.isConnected && el === document.activeElement), true);
            await page.getByRole('option', { name: 'Seguimiento de solicitudes', exact: true }).click();
            await page.getByRole('button', { name: 'Guardar', exact: true }).click();
            await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
            assert.deepEqual(fixture.content.sistemas[1].modulosSii.map(m => m.id), ['11', '22']);
            assert.equal(fixture.content.sistemas[0].fallas, 'Cambio que debe guardarse al salir');
        } finally { release.resolve(); }
    });
    for (const mode of ['ratón', 'teclado', 'táctil']) {
        await check(`alternar filas con ${mode}: primera interacción, búsqueda y presencia sin desmontar`, async ({ page, fixture }) => {
            for (const [index, id] of ['segundo', 'inicial', 'segundo'].entries()) {
                const input = module(page, id);
                await input.evaluate(el => {
                    const scroller = el.closest('.capture-row-scroll');
                    scroller.scrollLeft += el.getBoundingClientRect().left - scroller.getBoundingClientRect().left - 132;
                });
                const control = await input.elementHandle();
                const trigger = index === 2 ? input.locator('xpath=ancestor::*[contains(@class,"MuiAutocomplete-root")][1]').locator('.MuiAutocomplete-popupIndicator') : input;
                if (mode === 'ratón') await trigger.click();
                else if (mode === 'táctil') await trigger.tap();
                else {
                    await row(page, id).getByRole('combobox', { name: /^Sistema o herramienta/ }).focus();
                    await expect(row(page, id)).toHaveAttribute('data-edit-state', 'owned');
                    await page.keyboard.press('Tab'); await expect(input).toBeFocused(); await input.press('ArrowDown');
                }
                await expect(page.getByRole('option', { name: 'Seguimiento de solicitudes', exact: true })).toBeVisible();
                await expect(input).toHaveAttribute('aria-expanded', 'true'); await expect(input).toBeFocused();
                assert.equal(await control.evaluate(el => el.isConnected && el === document.activeElement), true);
                const searchLayout = await input.evaluate(el => {
                    const root = el.closest('.MuiInputBase-root'), buttons = root.querySelector('.MuiAutocomplete-endAdornment');
                    return { input: el.getBoundingClientRect().width, root: root.getBoundingClientRect().width, buttons: buttons.getBoundingClientRect().width };
                });
                assert.ok(searchLayout.input >= searchLayout.root - searchLayout.buttons - 17, JSON.stringify(searchLayout));
                if (mode === 'táctil' && index === 2) await page.screenshot({ path: path.join(artifacts, 'touch-options.png'), fullPage: true });
                await input.fill('Seguimiento');
                if (await page.getByRole('option', { name: 'Seguimiento de solicitudes', exact: true }).getAttribute('aria-selected') !== 'true') { await input.press('ArrowDown'); await input.press('Enter'); }
                await input.press('Escape');
                await expect(row(page, id).locator('.sii-module-value')).toHaveText('2 módulos seleccionados');
                await expect(row(page, id)).toHaveAttribute('data-edit-state', 'owned');
            }
            await page.getByRole('button', { name: 'Guardar', exact: true }).click();
            await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
            assert.deepEqual(fixture.content.sistemas.map(r => r.modulosSii.map(m => m.id)), [['11', '22'], ['11', '22']]);
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
        }, mode === 'táctil' ? 390 : 1366);
    }
    await check('reserva 409 denegada por otra sesión informa sin selección ni borrado', async ({ page, fixture }) => {
        // Segundo contexto de navegador con otra sesión sintética; no acredita identidad institucional.
        const other = await browser.newContext();
        await other.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
        await fixture.webSockets(other); const second = await other.newPage();
        const arrived = gate(), release = gate();
        await page.route('**/reservas', async route => { arrived.resolve(); await release.promise; await route.continue(); });
        try {
            await second.goto(fixture.origin + '/formatos/A'); await second.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
            await module(page, 'segundo').click(); await arrived.promise;
            await module(second, 'segundo').click(); await expect(row(second, 'segundo')).toHaveAttribute('data-edit-state', 'owned');
            await expect(row(page, 'segundo')).toHaveAttribute('data-edit-state', 'occupied');
            const rejected = page.waitForResponse(r => r.url().endsWith('/reservas') && r.status() === 409);
            release.resolve(); await rejected;
            await expect(page.locator('.MuiAutocomplete-popper').getByRole('status')).toContainText('reservado por Persona sintética');
            await expect(page.getByRole('option')).toHaveCount(0);
            await expect(row(page, 'segundo').locator('.MuiAutocomplete-clearIndicator')).toBeDisabled();
            await page.screenshot({ path: path.join(artifacts, 'reservation-denied.png'), fullPage: true });
            await module(page, 'segundo').press('ArrowDown'); await module(page, 'segundo').press('Enter');
            assert.equal(fixture.content.sistemas[1].moduloSiiId, '11');
            await module(page, 'segundo').press('Escape');
            await expect(row(page, 'segundo').locator('.sii-module-value')).toHaveText(fixture.catalog.modulos[0].descripcion);
            await second.getByRole('button', { name: 'Guardar', exact: true }).click();
            await expect(row(page, 'segundo')).toHaveAttribute('data-edit-state', 'idle');
            await module(page, 'segundo').click(); await expect(page.getByRole('option')).toHaveCount(2);
        } finally { release.resolve(); await other.close(); }
    });
    await check('fallo de reserva conserva nombre e ID, muestra error y permite reintento autorizado', async ({ page, fixture }) => {
        const reject = route => route.fulfill({ status: 503, contentType: 'application/json', body: JSON.stringify({ message: 'Reserva sintética no disponible.' }) });
        await page.route('**/reservas', reject); await module(page, 'segundo').click();
        await expect(module(page, 'segundo')).toHaveAttribute('aria-busy', 'false');
        await expect(page.locator('.MuiAutocomplete-popper').getByRole('status')).not.toHaveText('Preparando edición…');
        await expect(page.locator('.MuiAutocomplete-popper').getByRole('status')).not.toBeEmpty();
        await expect(page.getByRole('option')).toHaveCount(0);
        await page.screenshot({ path: path.join(artifacts, 'reservation-error.png'), fullPage: true });
        await module(page, 'segundo').press('ArrowDown'); await module(page, 'segundo').press('Enter');
        assert.equal(fixture.content.sistemas[1].moduloSiiId, '11');
        await module(page, 'segundo').press('Escape');
        await expect(row(page, 'segundo').locator('.sii-module-value')).toHaveText(fixture.catalog.modulos[0].descripcion);
        await page.unroute('**/reservas', reject); await page.evaluate(() => window.dispatchEvent(new Event('online')));
        await module(page, 'segundo').click(); await expect(page.getByRole('option')).toHaveCount(2);
    });
    await check('cambio rápido de fila cancela apertura y foco tardíos; Escape cancela otra intención pendiente', async ({ page, fixture }) => {
        const arrived = gate(), release = gate();
        await page.route('**/reservas', async route => {
            if (route.request().postDataJSON().blocks.some(b => b.key === 'sistemas:inicial')) { arrived.resolve(); await release.promise; }
            await route.continue();
        });
        try {
            await module(page, 'inicial').click(); await arrived.promise;
            await module(page, 'segundo').focus(); await module(page, 'segundo').press('ArrowDown');
            await expect(module(page, 'segundo')).toHaveAttribute('aria-busy', 'true');
            release.resolve();
            await expect(row(page, 'segundo')).toHaveAttribute('data-edit-state', 'owned');
            await expect(module(page, 'segundo')).toBeFocused(); await expect(module(page, 'segundo')).toHaveAttribute('aria-expanded', 'true');
            await expect(module(page, 'inicial')).toHaveAttribute('aria-expanded', 'false');
            await expect(row(page, 'inicial')).toHaveAttribute('data-edit-state', 'idle');
            assert.ok(!fixture.requests.some(r => r.path.endsWith('/bloques')));
        } finally { release.resolve(); }
        await module(page, 'segundo').press('Escape'); await page.getByRole('button', { name: 'Guardar', exact: true }).click();
        await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
        const arrivedAgain = gate(), releaseAgain = gate();
        await page.route('**/reservas', async route => { arrivedAgain.resolve(); await releaseAgain.promise; await route.continue(); });
        try {
            await module(page, 'inicial').click(); await arrivedAgain.promise; await module(page, 'inicial').press('Escape');
            releaseAgain.resolve(); await expect(row(page, 'inicial')).toHaveAttribute('data-edit-state', 'owned');
            await expect(page.locator('.MuiAutocomplete-popper')).toHaveCount(0);
            await expect(module(page, 'inicial')).toHaveAttribute('aria-expanded', 'false');
        } finally { releaseAgain.resolve(); }
    });
    await check('Procedimiento comparte apertura inmediata y bloquea selección hasta confirmar', async ({ page, fixture }) => {
        const arrived = gate(), release = gate();
        await page.route('**/reservas', async route => { arrived.resolve(); await release.promise; await route.continue(); });
        try {
            const input = row(page, 'segundo').getByRole('combobox', { name: 'Procedimiento 2', exact: true });
            await input.click(); await arrived.promise;
            await expect(page.locator('.MuiAutocomplete-popper').getByRole('status')).toHaveText('Preparando edición…');
            await input.fill('PO-02'); await input.press('ArrowDown'); await input.press('Enter');
            assert.equal(fixture.content.sistemas[1].proceso, '');
            release.resolve(); await expect(input).toHaveValue('PO-02');
            await expect(page.getByRole('option', { name: 'PO-02 · Actividad sintética', exact: true })).toBeVisible();
            await input.press('ArrowDown'); await input.press('Enter');
            await page.getByRole('button', { name: 'Guardar', exact: true }).click();
            await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
            assert.equal(fixture.content.sistemas[1].proceso, 'PO-02');
        } finally { release.resolve(); }
    });
    await check('volver a la fila original y abandonar la sección cancelan intenciones anteriores', async ({ page }) => {
        const arrived = gate(), release = gate();
        await page.route('**/reservas', async route => {
            if (route.request().postDataJSON().blocks.some(b => b.key === 'sistemas:inicial')) { arrived.resolve(); await release.promise; }
            await route.continue();
        });
        try {
            await module(page, 'inicial').click(); await arrived.promise;
            await module(page, 'segundo').focus(); await module(page, 'segundo').press('ArrowDown');
            await module(page, 'inicial').focus(); await module(page, 'inicial').press('ArrowDown'); release.resolve();
            await expect(page.getByRole('option')).toHaveCount(2);
            await expect(module(page, 'inicial')).toBeFocused(); await expect(module(page, 'inicial')).toHaveAttribute('aria-expanded', 'true');
            await expect(module(page, 'segundo')).toHaveAttribute('aria-expanded', 'false');
            await expect(row(page, 'segundo')).toHaveAttribute('data-edit-state', 'idle');
        } finally { release.resolve(); }
        await page.getByRole('button', { name: 'Guardar', exact: true }).click();
        await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
        const sectionArrived = gate(), sectionRelease = gate();
        await page.route('**/reservas', async route => { sectionArrived.resolve(); await sectionRelease.promise; await route.continue(); });
        try {
            await module(page, 'segundo').click(); await sectionArrived.promise;
            const tab = page.getByRole('tab', { name: 'Identificación general', exact: true });
            await tab.click(); sectionRelease.resolve();
            await expect(tab).toHaveAttribute('aria-selected', 'true'); await expect(tab).toBeFocused();
            await expect(page.locator('.MuiAutocomplete-popper')).toHaveCount(0);
            await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
            await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
            await expect(module(page, 'segundo')).toHaveAttribute('aria-expanded', 'false');
        } finally { sectionRelease.resolve(); }
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally { await writeFile(path.join(artifacts, 'report.json'), JSON.stringify(report, null, 2)); await browser.close(); }
