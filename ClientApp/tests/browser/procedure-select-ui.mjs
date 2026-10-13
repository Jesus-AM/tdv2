// React/MUI reales, datos y transporte sintéticos en memoria; sin bases ni servicios institucionales.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';

const output = path.resolve('../.artifacts/procedure-select-ui');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticTransport: true, aspNet: false, database: false, checks: [], errors: [] };
try {
    for (const width of [1366, 390, 320]) {
        const fixture = await systemFixture();
        // Códigos del caso reportado con nombres explícitamente sintéticos; no son los registros institucionales.
        const procedures = fixture.content.identificacion;
        Object.assign(procedures[0], { codigo: 'PO-08', tramite: 'Procedimiento sintético de recepción y seguimiento de solicitudes académicas, revisión de documentación y comunicación de resultados a las áreas participantes' });
        Object.assign(procedures[1], { codigo: 'PO-09', tramite: 'Procedimiento sintético de coordinación de servicios y actividades institucionales\nConservación de expedientes y seguimiento de compromisos con las unidades responsables' });
        Object.assign(procedures[2], { tramite: 'Procedimiento sintético con nombre extenso '.repeat(14).trim() });
        fixture.content.sistemas = procedures.map((p, i) => ({ ...fixture.content.sistemas[0], id: `fila${i}`, proceso: p.codigo }));
        const context = await browser.newContext({ viewport: { width, height: 900 }, hasTouch: true, reducedMotion: 'reduce' });
        await fixture.webSockets(context);
        await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
        const page = await context.newPage(); page.setDefaultTimeout(15000);
        page.on('pageerror', error => report.errors.push(error.message));
        const row = i => page.locator(`[data-edit-block="sistemas:fila${i}"]`);
        const input = i => row(i).getByRole('combobox', { name: `Procedimiento ${i + 1}`, exact: true });
        const label = p => `${p.codigo} · ${p.tramite}`;
        const openSystems = async () => {
            await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
            await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0);
        };
        const save = async () => {
            await page.getByRole('button', { name: 'Guardar', exact: true }).click();
            await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
        };
        const fullValue = async (i, p) => {
            const value = row(i).locator('.procedure-value');
            await expect(value).toHaveText(label(p));
            await expect(input(i)).toHaveAccessibleDescription(label(p).replace(/\s+/g, ' '));
            const dimensions = await value.evaluate(el => {
                const root = el.closest('.MuiInputBase-root'), cell = el.closest('td');
                const text = el.getBoundingClientRect(), control = root.getBoundingClientRect();
                const arrow = root.querySelector('.MuiAutocomplete-endAdornment').getBoundingClientRect();
                return { width: cell.getBoundingClientRect().width, textHeight: text.height, line: parseFloat(getComputedStyle(el).lineHeight),
                    fits: el.scrollHeight <= el.clientHeight + 1 && el.scrollWidth <= el.clientWidth + 1,
                    arrowClear: text.right <= arrow.left && text.bottom <= control.bottom,
                    fillsCell: Math.abs(el.closest('.MuiAutocomplete-root').getBoundingClientRect().width - (cell.clientWidth - parseFloat(getComputedStyle(cell).paddingLeft) - parseFloat(getComputedStyle(cell).paddingRight))) < 2,
                    size: getComputedStyle(el).fontSize, ellipsis: getComputedStyle(el).textOverflow };
            });
            assert.ok(dimensions.width >= 280 && dimensions.width <= 360, JSON.stringify(dimensions));
            assert.ok(dimensions.textHeight > dimensions.line * 2 && dimensions.fits && dimensions.arrowClear && dimensions.fillsCell, JSON.stringify(dimensions));
            assert.equal(dimensions.size, '13px'); assert.notEqual(dimensions.ellipsis, 'ellipsis');
        };
        try {
            await page.goto(fixture.origin + '/formatos/A'); await openSystems();
            await page.evaluate(() => {
                window.procedureEvents = [];
                for (const type of ['pointerdown', 'mousedown', 'focusin', 'focusout', 'pointerup', 'mouseup', 'click'])
                    document.addEventListener(type, event => window.procedureEvents.push({ type, target: event.target.outerHTML?.slice(0, 300),
                        row: event.target.closest('[data-edit-block]')?.getAttribute('data-edit-block'), y: event.clientY,
                        open: document.querySelector('[aria-expanded="true"][data-field="proceso"]')?.getAttribute('aria-label') }), true);
            });
            const tableWidth = await page.locator('.systems-table').evaluate(el => el.getBoundingClientRect().width);
            await expect(page.getByRole('button', { name: /Ayuda: Procedimiento seleccionado/ })).toHaveCount(0);
            assert.ok(await page.locator('.systems-table thead').getByRole('button', { name: /Ayuda:/ }).count() > 0);
            for (let i = 0; i < procedures.length; i++) {
                await fullValue(i, procedures[i]);
                if (width === 1366) await input(i).click(); else await input(i).tap();
                await expect(input(i)).toHaveAttribute('aria-expanded', 'true');
                await expect(row(i)).toHaveAttribute('data-edit-state', 'owned');
                await input(i).fill(procedures[i].codigo);
                const option = page.getByRole('option').first();
                await expect(option).toHaveText(label(procedures[i]));
                const bounds = await option.boundingBox(); assert.ok(bounds.height > 40);
                assert.ok(bounds.x >= 0 && bounds.x + bounds.width <= width + 1);
                assert.ok(await option.evaluate(el => el.scrollHeight <= el.clientHeight + 1 && getComputedStyle(el).whiteSpace === 'pre-wrap'));
                await page.screenshot({ path: path.join(output, `${width}-${procedures[i].codigo}-open.png`), fullPage: true });
                await input(i).press('ArrowDown'); await input(i).press('Enter');
                await expect(input(i)).toBeFocused(); await fullValue(i, procedures[i]);
                await page.screenshot({ path: path.join(output, `${width}-${procedures[i].codigo}-closed.png`), fullPage: true });
            }
            report.checks.push(`${width}px: PO-08/09 y nombre muy largo completos, sin ayuda, primer clic/tacto, flechas/Enter y ancho estable`);
            await input(0).focus(); await expect(row(0)).toHaveAttribute('data-edit-state', 'owned');
            await input(0).fill('coordinación'); await expect(page.getByRole('option')).toHaveCount(1);
            await input(0).press('ArrowDown'); await input(0).press('Enter');
            await fullValue(0, procedures[1]);
            await input(0).press('Tab'); await expect(input(0)).not.toBeFocused();
            await save(); assert.equal(fixture.content.sistemas[0].proceso, 'PO-09');
            await page.reload(); await openSystems(); await fullValue(0, procedures[1]);
            assert.equal(await page.locator('.systems-table').evaluate(el => el.getBoundingClientRect().width), tableWidth);
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            if (width < 600) {
                await input(0).evaluate(el => { const scroll = el.closest('[data-edit-scroll]'); scroll.scrollLeft = 290; });
                const arrow = row(0).getByRole('button', { name: 'Buscar procedimiento', exact: true });
                await arrow.tap(); await expect(input(0)).toHaveAttribute('aria-expanded', 'true');
                await expect(row(0)).toHaveAttribute('data-edit-state', 'owned');
                await input(0).press('Escape'); await fullValue(0, procedures[1]);
                await page.screenshot({ path: path.join(output, `${width}-horizontal-scroll.png`), fullPage: true });
                await save();
            }
            report.checks.push(`${width}px: búsqueda por nombre, cambio de asociación, Tab, guardado/recarga simulados y tabla sin ensancharse`);
            procedures[1].validacion = 'N'; fixture.readOnly = true;
            await page.reload(); await openSystems(); await fullValue(0, procedures[1]);
            await expect(input(0)).toHaveJSProperty('readOnly', true);
            report.checks.push(`${width}px: relación histórica fuera de opciones conserva nombre completo en consulta`);
        } catch (error) {
            report.events = await page.evaluate(() => window.procedureEvents);
            await page.screenshot({ path: path.join(output, `${width}-failure.png`), fullPage: true }); throw error;
        } finally { await context.close(); await fixture.close(); }
    }
    assert.deepEqual(report.errors, []); report.passed = true;
    console.log(`UI PASS ${report.checks.length} comprobaciones de Procedimiento`);
} catch (error) { report.failure = error.message; throw error; }
finally { await writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2)); await browser.close(); }
