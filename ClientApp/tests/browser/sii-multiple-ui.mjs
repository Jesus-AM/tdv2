// React/MUI real con HTTP/SignalR, catálogo y persistencia en memoria.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';

const artifacts = path.resolve('../.artifacts/sii-multiple-ui'); await mkdir(artifacts, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticTransport: true, realPostgreSql: false, checks: [], errors: [] };
const longName = 'Administración y seguimiento de solicitudes académicas y servicios institucionales para estudiantes y personal universitario';
try {
    for (const width of [1366, 390, 320]) {
        const fixture = await systemFixture();
        fixture.catalog = { estado: 'disponible', modulos: [
            { id: '1', descripcion: longName }, { id: '2', descripcion: 'Consultas' }, { id: '3', descripcion: 'Consultas' },
        ] };
        fixture.content.sistemas[0] = { ...fixture.content.sistemas[0], sistema: 'sii_v2', proceso: 'PO-01', uso: 'consultar', estado: 'bien',
            moduloSiiId: '1', moduloSiiDescripcion: longName, fallas: 'Contenido anterior\nConservado' };
        const context = await browser.newContext({ viewport: { width, height: 900 }, hasTouch: true });
        await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
        await fixture.webSockets(context); const page = await context.newPage(); page.setDefaultTimeout(10000);
        page.on('pageerror', error => report.errors.push(error.message));
        const systems = async () => { await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); };
        const row = page.locator('[data-edit-block="sistemas:inicial"]'), group = row.getByRole('group', { name: 'Módulos de SIIv2', exact: true });
        const input = group.getByRole('combobox'), chips = group.locator('.sii-module-value');
        const ids = () => fixture.content.sistemas[0].modulosSii?.map(m => m.id);
        const save = async () => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(row).not.toHaveAttribute('data-edit-state', 'owned'); };
        const check = async (name, fn) => { await fn(); report.checks.push({ name: `${width}px · ${name}`, passed: true }); console.log('UI PASS ' + name + ' ' + width); };
        try {
            await page.goto(fixture.origin + '/formatos/A'); await systems();
            await check('módulo único anterior completo y compacto, sin ID; primer clic y casillas', async () => {
                await expect(chips).toHaveText([longName]); await input.scrollIntoViewIfNeeded();
                if (width < 500) await input.tap(); else await input.click();
                await expect(input).toHaveAttribute('aria-expanded', 'true'); await expect(row).toHaveAttribute('data-edit-state', 'owned');
                await expect(page.getByRole('option')).toHaveCount(3);
                await expect(page.getByRole('option', { name: longName, exact: true })).toHaveAttribute('aria-selected', 'true');
                await expect(page.getByRole('option').first().locator('input[type=checkbox]')).toBeChecked();
                await expect(input).toHaveAccessibleDescription('Selecciona uno o varios módulos utilizados para este procedimiento.');
            });
            await check('varios IDs y descripciones duplicadas, menú y foco conservados con autoguardado', async () => {
                await input.fill('Consultas');
                const options = page.getByRole('option', { name: 'Consultas', exact: true });
                await options.nth(0).click(); await expect(input).toHaveAttribute('aria-expanded', 'true');
                await input.fill('Consultas'); await options.nth(1).click();
                await expect(chips).toHaveText([longName, 'Consultas', 'Consultas']);
                await expect(input).toBeFocused(); await expect(input).toHaveAttribute('aria-expanded', 'true');
                await input.fill('Consult');
                fixture.broadcast();
                await expect.poll(ids).toEqual(['1', '2', '3']);
                await expect(input).toHaveValue('Consult'); await expect(input).toBeFocused();
                await expect(input).toHaveAttribute('aria-expanded', 'true');
                await options.nth(0).click(); await expect(chips).toHaveCount(2);
                await expect(input).toHaveAttribute('aria-expanded', 'true');
                await input.press('Escape'); await expect(input).toHaveAttribute('aria-expanded', 'false');
                await save(); assert.deepEqual(ids(), ['1', '3']);
                await page.reload(); await systems(); await expect(chips).toHaveText([longName, 'Consultas']);
                assert.equal(fixture.content.sistemas[0].fallas, 'Contenido anterior\nConservado');
                assert.equal(fixture.content.sistemas[0].proceso, 'PO-01');
            });
            await check('nombres completos, etiquetas, ancho estable y eliminación individual con teclado', async () => {
                await input.click(); await expect(row).toHaveAttribute('data-edit-state', 'owned');
                await input.press('Escape');
                const layout = await group.evaluate(el => {
                    const chip = el.querySelector('.MuiChip-root'), label = chip.querySelector('.MuiChip-label'), arrow = el.querySelector('.MuiAutocomplete-endAdornment');
                    const bounds = chip.getBoundingClientRect();
                    return { textWidth: label.scrollWidth, width: label.clientWidth, textHeight: label.scrollHeight, height: label.clientHeight,
                        chipRight: bounds.right, arrowLeft: arrow.getBoundingClientRect().left, font: getComputedStyle(label).fontSize,
                        lines: label.clientHeight / parseFloat(getComputedStyle(label).lineHeight), table: el.closest('table').getBoundingClientRect().width };
                });
                assert.equal(layout.font, '13px'); assert.ok(layout.lines > 2);
                assert.ok(layout.textWidth <= layout.width + 1 && layout.textHeight <= layout.height + 1, JSON.stringify(layout));
                assert.ok(layout.chipRight <= layout.arrowLeft, JSON.stringify(layout));
                await input.fill(''); await input.press('ArrowLeft'); await expect(chips.last()).toBeFocused();
                await page.keyboard.press('Delete'); await expect(chips).toHaveCount(1);
                await input.focus(); await input.press('ArrowDown');
                await expect(page.getByRole('option', { name: longName, exact: true })).toHaveAttribute('aria-selected', 'true');
                await input.press('Escape'); await input.press('Tab');
                await expect(input).not.toBeFocused(); await expect(row).toHaveAttribute('data-edit-state', 'owned');
                await input.focus(); await group.getByLabel(`Quitar ${longName}`, { exact: true }).click();
                await expect(chips).toHaveCount(0); await save(); assert.deepEqual(ids(), []);
                await page.reload(); await systems(); await expect(chips).toHaveCount(0);
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
                assert.equal(await page.locator('.systems-table').evaluate(el => el.getBoundingClientRect().width), layout.table);
            });
            await check('422 junto al campo conserva selección; corrección, consulta histórica y bloqueo', async () => {
                await input.click(); await expect(row).toHaveAttribute('data-edit-state', 'owned');
                const reject = route => route.fulfill({ status: 422, contentType: 'application/json', body: JSON.stringify({ message: 'Revisa los módulos seleccionados.',
                    validation: [{ block: 'sistemas:inicial', field: 'modulosSii', message: 'Revisa los módulos seleccionados.' }] }) });
                await page.route('**/bloques', reject);
                await input.fill('Administración'); await page.getByRole('option', { name: longName, exact: true }).click();
                await page.getByRole('button', { name: 'Guardar', exact: true }).click();
                await expect(input).toHaveAttribute('aria-invalid', 'true'); await expect(chips).toHaveText([longName]);
                await expect(input).toHaveAccessibleDescription('Revisa los módulos seleccionados.');
                await page.unroute('**/bloques', reject);
                await input.fill('Consultas'); await page.getByRole('option', { name: 'Consultas', exact: true }).nth(1).click();
                await save(); assert.deepEqual(ids(), ['1', '3']);
                fixture.catalog.modulos = [{ id: '3', descripcion: 'Nombre actualizado en catálogo' }];
                fixture.submitted = true; await page.reload(); await systems();
                await expect(chips).toHaveText([longName, 'Consultas']); await expect(input).toHaveJSProperty('readOnly', true);
                await expect(group.locator('.MuiChip-deleteIcon')).toHaveCount(0);
                await input.scrollIntoViewIfNeeded();
                await page.screenshot({ path: path.join(artifacts, `multiple-${width}.png`), fullPage: true });
            });
        } finally { await context.close(); await fixture.close(); }
    }
    assert.deepEqual(report.errors, []); report.passed = true;
} finally { await writeFile(path.join(artifacts, 'report.json'), JSON.stringify(report, null, 2)); await browser.close(); }
