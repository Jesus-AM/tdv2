// React/MUI real; HTTP, SignalR y persistencia simulados. No acredita PostgreSQL ni SQL Server.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';
const artifacts = path.resolve('../.artifacts/applications-ui'); await mkdir(artifacts, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticTransport: true, realPostgreSql: false, checks: [], errors: [] };
const names = { moviles: 'Aplicaciones móviles', web: 'Aplicaciones Web' };
const fields = { moviles: 'aplicacionesMoviles', web: 'aplicacionesWeb' };
try {
    for (const width of [1366, 390]) for (const kind of ['moviles', 'web']) {
        const fixture = await systemFixture();
        const longName = `${names[kind]} para el seguimiento de solicitudes y servicios universitarios con un nombre completo que ocupa varias líneas`;
        fixture.applications = Object.fromEntries(['moviles', 'web'].map(catalogo => [catalogo, { estado: 'disponible', aplicaciones: [
            { id: '1', catalogo, descripcion: catalogo === kind ? longName : `Servicio ${catalogo}` },
            { id: '2', catalogo, descripcion: 'Consultas' }, { id: '3', catalogo, descripcion: 'Consultas' },
        ] }]));
        const context = await browser.newContext({ viewport: { width, height: 900 }, hasTouch: true });
        await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
        await fixture.webSockets(context); const page = await context.newPage(); page.setDefaultTimeout(12000);
        page.on('pageerror', e => report.errors.push(e.message));
        const row = page.locator('[data-edit-block="sistemas:inicial"]');
        const group = row.getByRole('group', { name: names[kind], exact: true });
        const input = group.getByRole('combobox'), summary = group.locator('.record-selected-value');
        const systems = () => page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
        const save = async () => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(row).not.toHaveAttribute('data-edit-state', 'owned'); };
        const choose = async label => { await row.getByRole('combobox', { name: 'Sistema o herramienta 1', exact: true }).click(); await page.getByRole('option', { name: label, exact: true }).click(); };
        const check = async (name, fn) => { await fn(); report.checks.push({ name: `${kind} ${width} · ${name}`, passed: true }); console.log(`UI PASS ${kind} ${width} ${name}`); };
        try {
            await page.goto(fixture.origin + '/formatos/A'); await systems();
            await check('cero, uno, nombres completos y recarga', async () => {
                await choose(names[kind]); await expect(input).toHaveAttribute('placeholder', 'Selecciona aplicaciones');
                await save(); await page.reload(); await systems(); await expect(summary).toHaveCount(0);
                if (width < 500) await input.tap(); else await input.click();
                await expect(input).toHaveAttribute('aria-expanded', 'true'); await expect(row).toHaveAttribute('data-edit-state', 'owned');
                await input.fill('seguimiento'); await page.getByRole('option', { name: longName, exact: true }).click();
                await expect(input).toHaveAttribute('aria-expanded', 'true'); await input.press('Escape');
                await expect(summary).toHaveText(longName); await save(); await page.reload(); await systems();
                await expect(summary).toHaveText(longName);
                assert.deepEqual(fixture.content.sistemas[0][fields[kind]], [{ id: '1', catalogo: kind, descripcion: longName }]);
                await input.scrollIntoViewIfNeeded(); await page.screenshot({ path: path.join(artifacts, `${kind}-single-${width}.png`), fullPage: true });
            });
            await check('varias casillas, búsqueda, teclado, presencia y autoguardado sin cerrar', async () => {
                await input.click(); await input.fill('Consultas'); const options = page.getByRole('option', { name: 'Consultas', exact: true });
                await options.nth(0).click(); await input.fill('Consultas'); await options.nth(1).click();
                await expect(summary).toHaveText('3 aplicaciones seleccionadas'); await expect(group.locator('.MuiChip-root')).toHaveCount(0);
                await input.fill('Consulta'); fixture.broadcast();
                await expect.poll(() => fixture.content.sistemas[0][fields[kind]]?.length).toBe(3);
                await expect(input).toHaveValue('Consulta'); await expect(input).toBeFocused(); await expect(input).toHaveAttribute('aria-expanded', 'true');
                await input.press('Escape'); await save(); await page.reload(); await systems();
                await expect(summary).toHaveText('3 aplicaciones seleccionadas'); await input.click();
                await expect(page.getByRole('option').locator('input:checked')).toHaveCount(3);
                await input.press('Home'); await input.press('Enter'); await expect(summary).toHaveText('2 aplicaciones seleccionadas');
                await input.press('Home'); await input.press('ArrowDown'); await input.press('Enter'); await expect(summary).toHaveText('Consultas');
                await input.press('Escape'); await save(); assert.deepEqual(fixture.content.sistemas[0][fields[kind]].map(v => v.id), ['3']);
            });
            await check('catálogos separados al alternar; vaciar, recargar y reserva en otra fila', async () => {
                const other = kind === 'moviles' ? 'web' : 'moviles'; await choose(names[other]);
                const alternate = row.getByRole('group', { name: names[other], exact: true }).getByRole('combobox');
                await expect(alternate).toHaveAttribute('placeholder', 'Selecciona aplicaciones'); await alternate.click();
                await page.getByRole('option', { name: `Servicio ${other}`, exact: true }).click(); await alternate.press('Escape'); await save();
                assert.equal(fixture.content.sistemas[0][fields[kind]][0].catalogo, kind);
                assert.equal(fixture.content.sistemas[0][fields[other]][0].catalogo, other);
                await choose(names[kind]); await expect(summary).toHaveText('Consultas'); await save(); await page.reload(); await systems();
                await input.click(); await group.getByRole('button', { name: 'Quitar selecciones' }).click(); await input.press('Escape'); await save();
                await page.reload(); await systems(); await expect(summary).toHaveCount(0); await expect(input).toHaveAttribute('placeholder', 'Selecciona aplicaciones');
                assert.deepEqual(fixture.content.sistemas[0][fields[kind]], []);
                await page.getByRole('button', { name: 'Agregar fila', exact: true }).click(); await save();
                if (width < 500) await input.tap(); else await input.click();
                await expect(input).toHaveAttribute('aria-expanded', 'true'); await expect(row).toHaveAttribute('data-edit-state', 'owned');
                await input.press('Escape'); await save();
                await expect(page.getByText('Procedimientos y captura de herramientas', { exact: true })).toHaveCount(0);
                assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            });
        } finally { await context.close(); await fixture.close(); }
    }
    assert.deepEqual(report.errors, []); report.passed = true;
} finally { await writeFile(path.join(artifacts, 'report.json'), JSON.stringify(report, null, 2)); await browser.close(); }
