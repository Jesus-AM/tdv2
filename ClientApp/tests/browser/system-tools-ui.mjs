import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';

const artifacts = path.resolve('../.artifacts/system-tools-ui'); await mkdir(artifacts, { recursive: true });
const fixture = await systemFixture(), browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { syntheticTransport: true, realPostgreSql: false, checks: [], errors: [] };
const check = async (name, work) => { await work(); report.checks.push({ name, passed: true }); console.log('UI PASS ' + name); };
async function session(width = 1366) {
    const context = await browser.newContext({ viewport: { width, height: 900 }, reducedMotion: 'reduce', hasTouch: true });
    await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
    await fixture.webSockets(context); const page = await context.newPage(); page.on('pageerror', e => report.errors.push(e.message));
    page.setDefaultTimeout(10000);
    await page.goto(fixture.origin + '/formatos/A'); await systems(page); return { page, context };
}
async function systems(page) { await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); }
const row = page => page.locator('[data-edit-block="sistemas:inicial"]');
const choice = (page, name) => page.getByRole('combobox', { name: name + ' 1', exact: true });
async function select(page, label, option) { await choice(page, label).click(); await page.getByRole('option', { name: option, exact: true }).click(); }
async function finish(page) {
    await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
    await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
    await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/);
}
try {
    const { page } = await session();
    await check('catálogo pendiente, SII vacío persiste como borrador y los otros campos siguen disponibles', async () => {
        await select(page, 'Sistema o herramienta', 'SIIv2');
        await expect(page.getByText('El catálogo de módulos de SIIv2 aún no está disponible', { exact: true })).toBeVisible();
        await select(page, '¿Para qué se usa?', 'Consultar información'); await finish(page);
        assert.equal(fixture.content.sistemas[0].sistema, 'sii_v2'); assert.equal(fixture.content.sistemas[0].moduloSiiId, '');
    });
    await check('carga y error del catálogo son contextuales; búsqueda distingue duplicados y persiste ID', async () => {
        fixture.catalog = { estado: 'error', modulos: [] }; await page.reload(); await systems(page);
        await expect(page.getByText(/No fue posible consultar el catálogo local de módulos/)).toBeVisible();
        fixture.catalog = { estado: 'disponible', modulos: [{ id: '1', descripcion: 'Solicitudes' }, { id: '2', descripcion: 'Consultas' }, { id: '3', descripcion: 'Consultas' }] }; fixture.delay = 1200;
        await page.reload(); await systems(page); await expect(page.getByText('Cargando catálogo local…')).toBeVisible(); fixture.delay = 0;
        const module = choice(page, 'Módulo de SIIv2'); await module.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await module.fill('Consultas'); await expect(page.getByRole('option', { name: 'Consultas · ID 2', exact: true })).toBeVisible();
        await page.getByRole('option', { name: 'Consultas · ID 3', exact: true }).click(); await finish(page);
        assert.equal(fixture.content.sistemas[0].moduloSiiId, '3'); await page.reload(); await systems(page);
        await expect(choice(page, 'Módulo de SIIv2')).toHaveValue('Consultas · ID 3');
    });
    await check('Otra herramienta y Otro uso conservan detalles al cambiar selección sin liberar la fila', async () => {
        await select(page, 'Sistema o herramienta', 'Otra (escríbela)');
        const before = fixture.requests.filter(r => r.path.endsWith('/liberar')).length;
        await page.getByLabel('Nombre de la herramienta 1', { exact: true }).fill('Aplicación propia\nMultilínea');
        await select(page, '¿Para qué se usa?', 'Otro (escríbelo)'); await page.getByLabel('Describe el uso 1', { exact: true }).fill('Revisar solicitudes');
        await select(page, 'Sistema o herramienta', 'Hoja de cálculo (Excel)'); await expect(page.getByLabel('Nombre de la herramienta 1', { exact: true })).toHaveCount(0);
        await select(page, 'Sistema o herramienta', 'Otra (escríbela)'); await expect(page.getByLabel('Nombre de la herramienta 1', { exact: true })).toHaveValue('Aplicación propia\nMultilínea');
        assert.equal(fixture.requests.filter(r => r.path.endsWith('/liberar')).length, before);
        await finish(page); await page.reload(); await systems(page); await expect(page.getByLabel('Describe el uso 1', { exact: true })).toHaveValue('Revisar solicitudes');
    });
    await check('cuatro estados con icono, texto y color; comentarios multilínea opcionales', async () => {
        for (const [label, key] of [['Funciona bien', 'bien'], ['Funciona con fallas', 'fallas'], ['No funciona', 'no_funciona'], ['Ya no se usa', 'sin_uso']]) {
            await select(page, '¿Cómo funciona?', label); await expect(choice(page, '¿Cómo funciona?').locator('svg')).toHaveCount(1);
            await finish(page); assert.equal(fixture.content.sistemas[0].estado, key);
        }
        await page.getByLabel('Fallas o comentarios 1', { exact: true }).click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await page.getByLabel('Fallas o comentarios 1', { exact: true }).fill('En horas pico\nNo exporta'); await finish(page);
    });
    await check('ayudas: X accesible, Cerrar, Escape, retorno de foco y reserva conservada', async () => {
        await choice(page, 'Sistema o herramienta').click(); await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
        const before = fixture.requests.filter(r => r.path.endsWith('/liberar')).length;
        for (const method of ['x', 'button', 'escape']) {
            const help = page.getByRole('button', { name: 'Ayuda: Sistema o herramienta', exact: true }); await help.click();
            const dialog = page.getByRole('dialog'); await expect(dialog).toBeVisible();
            await expect(dialog.getByText('Consejo', { exact: true })).toHaveCount(0);
            if (method === 'escape') await page.keyboard.press('Escape');
            else await dialog.getByRole('button', { name: method === 'x' ? 'Cerrar ayuda' : 'Cerrar', exact: true }).click();
            await expect(help).toBeFocused(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        }
        assert.equal(fixture.requests.filter(r => r.path.endsWith('/liberar')).length, before); await finish(page);
    });
    await check('dos sesiones: reserva ajena impide seleccionar y autoguardado se recibe sin sobreescritura', async () => {
        const other = await session();
        await choice(page, 'Sistema o herramienta').click(); await expect(row(other.page)).toHaveAttribute('data-edit-state', 'occupied');
        await choice(other.page, 'Sistema o herramienta').click(); await expect(other.page.getByRole('listbox')).toHaveCount(0);
        await page.keyboard.press('Escape'); await select(page, '¿Cómo funciona?', 'Funciona bien'); await finish(page);
        await expect(row(other.page)).toHaveAttribute('data-edit-state', 'idle'); await expect(choice(other.page, '¿Cómo funciona?')).toHaveText('Funciona bien');
        await other.context.close();
    });
    await check('varias filas con mismo procedimiento; claves confirmadas sin unicidad por procedimiento', async () => {
        await select(page, 'Procedimiento', 'PO-01 · Actividad sintética'); await finish(page);
        await page.getByRole('button', { name: 'Agregar fila', exact: true }).click(); await expect(page.locator('[data-edit-block^="sistemas:"]')).toHaveCount(2);
        await finish(page); await page.getByRole('combobox', { name: 'Procedimiento 2', exact: true }).click();
        await page.getByRole('option', { name: 'PO-01 · Actividad sintética', exact: true }).click(); await finish(page);
        assert.ok(fixture.content.sistemas.every(r => r.proceso === 'PO-01'));
    });
    await check('Código y orígenes ILDA, ISO y Nuevo; Prioridad sin Ejemplo ni Consejo', async () => {
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        await expect(page.getByRole('columnheader', { name: /Código/ })).toBeVisible();
        for (const value of ['ILDA', 'ISO 21001', 'Nuevo']) await expect(page.getByText(value, { exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'Ayuda: Código', exact: true }).click(); await expect(page.getByRole('dialog').getByText('PO-01, PO-02, PO-03…', { exact: true })).toBeVisible(); await page.keyboard.press('Escape');
        await page.getByRole('button', { name: 'Ayuda: Prioridad', exact: true }).click();
        await expect(page.getByRole('dialog').getByText(/^(Ejemplo|Consejo)$/)).toHaveCount(0); await page.keyboard.press('Escape');
    });
    await check('escritorio/móvil: tabla compacta, foco teclado y scroll contenido', async () => {
        for (const width of [1920, 1366, 768, 360]) {
            await page.setViewportSize({ width, height: 900 }); await systems(page);
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            assert.equal(await row(page).locator('.MuiInputBase-root').first().evaluate(e => getComputedStyle(e).fontSize), '13px');
            await choice(page, 'Sistema o herramienta').focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
            await expect(choice(page, 'Sistema o herramienta')).toBeFocused(); await finish(page);
            await page.evaluate(() => { document.activeElement?.blur(); window.scrollTo({ top: 0, behavior: 'instant' }); });
            await page.screenshot({ path: path.join(artifacts, `systems-${width}.png`), fullPage: true });
            if (width === 360) {
                await page.locator('.capture-scroll').evaluate(e => { e.scrollLeft = 280; });
                await page.screenshot({ path: path.join(artifacts, 'systems-360-tools.png'), fullPage: true });
            }
        }
    });
    await check('históricos y enviados conservan valores y cierran la edición', async () => {
        fixture.content.sistemas[0].sistema = 'Herramienta histórica'; fixture.content.sistemas[0].uso = 'Uso histórico'; fixture.content.sistemas[0].estado = 'Parcial'; fixture.submitted = true; fixture.version++;
        await page.reload(); await systems(page); await expect(choice(page, 'Sistema o herramienta')).toHaveText('Herramienta histórica');
        await choice(page, 'Sistema o herramienta').click(); await expect(page.getByRole('listbox')).toHaveCount(0);
        await expect(page.getByLabel('Fallas o comentarios 1', { exact: true })).toHaveJSProperty('readOnly', true);
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally {
    report.requests = fixture.requests;
    for (const context of browser.contexts()) for (const page of context.pages()) {
        await page.screenshot({ path: path.join(artifacts, 'last.png'), fullPage: true }).catch(() => {});
        report.lastText = await page.locator('body').innerText().catch(() => '');
    }
    await writeFile(path.join(artifacts, 'report.json'), JSON.stringify(report, null, 2));
    await browser.close(); await fixture.close();
}
