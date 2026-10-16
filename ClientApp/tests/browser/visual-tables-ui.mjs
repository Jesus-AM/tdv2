// React/MUI/Edge reales; dos actores, reservas y persistencia simulados. No usa bases.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';
import { renderedContrast } from './rendered-contrast.mjs';
const output = path.resolve(process.env.TDV2_VISUAL_ARTIFACTS || '../.artifacts/visual-refresh'); await mkdir(output, { recursive: true });
const report = { mockedTransport: true, database: false, checks: [], errors: [], styles: [] };
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const check = async (name, run) => { await run(); report.checks.push(name); console.log('UI PASS ' + name); };
const row = (page, id, section = 'sistemas') => page.locator(`[data-edit-block="${section}:${id}"]`);
const tab = async (page, name) => { await page.getByRole('tab', { name, exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); };
const save = async page => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); };
const capture = async (page, name) => {
    await page.evaluate(() => { if (document.activeElement?.classList.contains('skip-link')) document.activeElement.blur(); window.scrollTo(0, 0); });
    await page.screenshot({ path: path.join(output, name + '.png'), fullPage: true });
};
const scroll = page => page.locator('.capture-scroll:visible');
async function positionField(page, locator) {
    await locator.evaluate(el => {
        const region = el.closest('[data-edit-scroll]'), c = region.getBoundingClientRect(), r = el.getBoundingClientRect();
        const scale = c.width / region.offsetWidth;
        region.scrollLeft += (r.left - c.left) / scale - (region.clientWidth > 600 ? 140 : 12);
        const head = region.querySelector('thead').getBoundingClientRect();
        if (r.top < head.bottom || r.bottom > c.bottom) region.scrollTop += (r.top - head.bottom) / scale - 12;
    });
}
async function sticky(page) {
    await scroll(page).scrollIntoViewIfNeeded();
    for (const fraction of [0, .5, 1]) {
        await scroll(page).evaluate((el, f) => { el.scrollLeft = (el.scrollWidth - el.clientWidth) * f; }, fraction);
        const values = await scroll(page).evaluate(el => {
            const box = el.getBoundingClientRect();
            return [...el.querySelectorAll('.row-actions-cell')].map(cell => {
                const r = cell.getBoundingClientRect(), s = getComputedStyle(cell), tr = cell.parentElement.getBoundingClientRect();
                return { right: r.right, limit: box.left + (el.clientLeft + el.clientWidth) * box.width / el.offsetWidth, position: s.position, z: s.zIndex,
                    top: r.top, rowTop: tr.top, height: r.height, rowHeight: tr.height, head: cell.tagName === 'TH', bg: s.backgroundColor };
            });
        });
        assert.ok(values.length > 1);
        for (const v of values) {
            assert.equal(v.position, 'sticky'); assert.ok(Math.abs(v.right - v.limit) < 2, JSON.stringify(v));
            assert.notEqual(v.bg, 'rgba(0, 0, 0, 0)');
            if (!v.head) { assert.ok(Math.abs(v.top - v.rowTop) < 1); assert.ok(Math.abs(v.height - v.rowHeight) < 1); }
        }
    }
}
try {
    for (const [width, zoom] of [[1366, 1], [390, 1], [320, 1], [1366, 2]]) {
        const fixture = await systemFixture();
        fixture.participantForRequest = req => req.headers['x-visual-actor'] === 'alba'
            ? { titular: 'Alba Mora', color: 0 } : { titular: 'Bruno Solís', color: 1 };
        const base = fixture.content.sistemas[0];
        fixture.content.sistemas = [
            { ...base, id: 'a', proceso: 'PO-01', sistema: 'microsoft_365', aplicacionesMicrosoft365: ['excel'], uso: 'consultar', estado: 'bien' },
            { ...base, id: 'b', proceso: 'PO-01', sistema: 'sii_v2', modulosSii: [{ id: '1', descripcion: 'Módulo de solicitudes y seguimiento institucional' }], uso: 'consultar', estado: 'bien', fallas: 'Texto largo\nSegunda línea\nTercera línea\nCuarta línea' },
            { ...base, id: 'c', proceso: 'PO-02', sistema: 'adobe_creative_cloud', aplicacionesAdobe: ['photoshop'], uso: 'consultar', estado: 'bien' },
            { ...base, id: 'd', proceso: 'PO-03', sistema: 'formulario', uso: 'consultar', estado: 'bien' },
        ];
        fixture.catalog = { estado: 'disponible', modulos: [{ id: '1', descripcion: 'Módulo de solicitudes y seguimiento institucional' }, { id: '2', descripcion: 'Consulta institucional' }] };
        const contexts = [];
        async function actor(name) {
            const context = await browser.newContext({ viewport: { width, height: 900 }, reducedMotion: 'reduce', hasTouch: width < 500, extraHTTPHeaders: { 'X-Visual-Actor': name } });
            contexts.push(context); await fixture.webSockets(context);
            if (zoom !== 1) await context.addInitScript(z => document.addEventListener('DOMContentLoaded', () => { document.documentElement.style.zoom = z; }), zoom);
            await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
            const page = await context.newPage(); page.setDefaultTimeout(10000);
            page.on('pageerror', error => report.errors.push(error.message));
            await page.goto(fixture.origin + '/formatos/A');
            if (zoom !== 1) await page.evaluate(z => { document.documentElement.style.zoom = z; }, zoom);
            await tab(page, 'Sistemas y herramientas'); return page;
        }
        const page = await actor('alba'), other = await actor('bruno'), key = `${width}-${zoom}x`;
        try {
            await check(`${key}: grupos legibles, primer clic, búsqueda y teclado`, async () => {
                const tool = row(page, 'a').getByRole('combobox', { name: 'Sistema o herramienta 1', exact: true });
                await positionField(page, tool); await tool.click({ position: { x: 12, y: 12 } });
                await expect(page.getByRole('listbox')).toBeVisible();
                const headings = page.locator('.tool-catalog-group-title');
                await expect(headings).toHaveCount(5);
                for (const heading of await headings.all()) {
                    assert.equal(await heading.evaluate(el => getComputedStyle(el).fontWeight), '700');
                    assert.equal(await heading.evaluate(el => getComputedStyle(el).opacity), '1');
                    assert.equal(await heading.getAttribute('role'), null);
                }
                await expect(page.getByRole('group', { name: 'Sistemas institucionales', exact: true })).toBeVisible();
                const layers = await page.locator('.MuiAutocomplete-popper').evaluate(el => {
                    const popup = el.getBoundingClientRect();
                    const cell = [...document.querySelectorAll('.row-actions-cell')].find(cell => {
                        const r = cell.getBoundingClientRect(); return r.right > popup.left && r.left < popup.right && r.top < popup.bottom && r.bottom > popup.top;
                    });
                    if (!cell) return Number(getComputedStyle(el).zIndex) > 4;
                    const r = cell.getBoundingClientRect(), x = (Math.max(r.left, popup.left) + Math.min(r.right, popup.right)) / 2,
                        y = (Math.max(r.top, popup.top, 0) + Math.min(r.bottom, popup.bottom, innerHeight)) / 2;
                    return el.contains(document.elementFromPoint(x, y));
                });
                assert.equal(layers, true, 'El menú queda por encima de Acciones');
                const contrast = await renderedContrast(page, '.tool-catalog-group-title');
                assert.ok(contrast.every(c => c.ratio >= c.required)); report.styles.push({ key, catalogContrast: contrast });
                await expect(page.getByRole('option', { name: 'Sistema Integral de Información', exact: false })).toHaveAttribute('aria-disabled', 'true');
                const geometry = await row(page, 'a').locator('.tool-select').evaluate(el => {
                    const summary = el.querySelector('.record-selected-value')?.getBoundingClientRect(), input = el.querySelector('input').getBoundingClientRect();
                    return { bottom: summary?.bottom, inputTop: input.top };
                });
                if (geometry.bottom) assert.ok(geometry.inputTop >= geometry.bottom - 1, JSON.stringify(geometry));
                const cdp = await page.context().newCDPSession(page);
                await cdp.send('DOM.enable'); await cdp.send('CSS.enable');
                const doc = await cdp.send('DOM.getDocument');
                const node = await cdp.send('DOM.querySelector', { nodeId: doc.root.nodeId, selector: '.tool-catalog-group-title' });
                report.styles.push({ key, fonts: (await cdp.send('CSS.getPlatformFontsForNode', { nodeId: node.nodeId })).fonts });
                await cdp.detach();
                await capture(page, `catalog-${key}`);
                await tool.fill('Adobe'); await expect(page.getByRole('option')).toHaveCount(1);
                await tool.press('ArrowDown'); await tool.press('Escape');
            });
            await check(`${key}: dos usuarios, estados, color estable y acciones bloqueadas`, async () => {
                const comments = row(other, 'b').getByLabel('Áreas de oportunidad 2', { exact: true });
                await positionField(other, comments); await comments.click({ position: { x: 12, y: 12 } });
                await expect(row(other, 'b')).toHaveAttribute('data-edit-state', 'owned');
                await expect(row(page, 'b')).toHaveAttribute('data-edit-state', 'occupied');
                await expect(row(page, 'b').getByText('Bruno Solís está editando')).toBeVisible();
                await expect(row(page, 'a').getByText('Estás editando', { exact: true })).toBeVisible();
                await expect(row(page, 'b').getByRole('button', { name: 'Eliminar registro', exact: true })).toBeDisabled();
                const styles = await page.locator('[data-edit-block]').evaluateAll(rows => rows.slice(0, 3).map(el => ({ state: el.dataset.editState,
                    color: getComputedStyle(el).getPropertyValue('--participant-color'), bg: getComputedStyle(el).backgroundColor,
                    cell: getComputedStyle(el.querySelector('.row-actions-cell')).backgroundColor,
                    indicator: getComputedStyle(el.querySelector('.row-editing-cell')).boxShadow })));
                assert.notEqual(styles[0].color, styles[1].color);
                assert.equal(styles[0].bg, styles[0].cell); assert.equal(styles[1].bg, styles[1].cell);
                report.styles.push({ key, presence: styles });
                const contrast = await renderedContrast(page, '.row-presence');
                assert.ok(contrast.every(c => c.ratio >= c.required)); report.styles.push({ key, presenceContrast: contrast });
                await scroll(page).evaluate(el => { el.scrollLeft = 0; el.scrollTop = 0; });
                await capture(page, `presence-${key}`);
                await sticky(page);
                const lastField = await row(page, 'a').getByLabel('Áreas de oportunidad 1', { exact: true }).evaluate(el => ({
                    fieldRight: el.closest('.MuiFormControl-root').getBoundingClientRect().right,
                    actionsLeft: el.closest('tr').querySelector('.row-actions-cell').getBoundingClientRect().left,
                }));
                assert.ok(lastField.fieldRight <= lastField.actionsLeft, JSON.stringify(lastField));
                await save(other);
            });
            await check(`${key}: multiselección, cambio de familia, autoguardado y recarga`, async () => {
                const suite = row(page, 'a').getByRole('combobox', { name: 'Aplicaciones de Microsoft 365 1', exact: true });
                await positionField(page, suite); await suite.click({ position: { x: 12, y: 12 } });
                await page.getByRole('option', { name: 'Forms — formularios', exact: true }).click();
                await expect(page.getByRole('listbox')).toBeVisible();
                await page.keyboard.press('Escape');
                await expect(row(page, 'a').locator('.sii-module-value')).toHaveText('2 aplicaciones seleccionadas');
                const comments = row(page, 'a').getByLabel('Áreas de oportunidad 1', { exact: true });
                await positionField(page, comments); await comments.fill('Texto conservado durante el autoguardado');
                const before = await scroll(page).evaluate(el => [el.scrollLeft, el.scrollTop]);
                const handle = await comments.elementHandle();
                await expect.poll(() => fixture.content.sistemas[0].fallas).toBe('Texto conservado durante el autoguardado');
                assert.equal(await handle.evaluate(el => el.isConnected && el === document.activeElement), true);
                assert.deepEqual(await scroll(page).evaluate(el => [el.scrollLeft, el.scrollTop]), before);
                await save(page); await page.reload(); await tab(page, 'Sistemas y herramientas');
                await expect(row(page, 'a').locator('.sii-module-value')).toHaveText('2 aplicaciones seleccionadas');
                const tool = row(page, 'a').getByRole('combobox', { name: 'Sistema o herramienta 1', exact: true });
                await positionField(page, tool); await tool.click({ position: { x: 12, y: 12 } }); await page.getByRole('option', { name: 'Adobe Creative Cloud', exact: true }).click();
                const adobe = row(page, 'a').getByRole('combobox', { name: 'Aplicaciones de Adobe Creative Cloud 1', exact: true });
                await positionField(page, adobe); await adobe.click({ position: { x: 12, y: 12 } }); await expect(page.getByRole('option')).toHaveCount(20);
                await page.getByRole('option', { name: 'Acrobat Pro', exact: true }).click(); await page.keyboard.press('Escape');
                await expect(row(page, 'a').locator('.sii-module-value')).toHaveText('Acrobat Pro');
                await positionField(page, tool); await tool.click({ position: { x: 12, y: 12 } }); await page.getByRole('option', { name: 'Microsoft 365', exact: true }).click();
                await expect(row(page, 'a').locator('.sii-module-value')).toHaveText('2 aplicaciones seleccionadas'); await save(page);
            });
            await check(`${key}: Acciones, confirmación, cancelar y asociaciones conservadas`, async () => {
                await sticky(page);
                await scroll(page).evaluate(el => { el.scrollLeft = 0; });
                await row(page, 'a').getByRole('button', { name: 'Eliminar registro', exact: true }).click();
                await expect(page.getByRole('dialog')).toBeVisible(); await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
                assert.equal(fixture.content.sistemas.length, 4);
                await tab(page, 'Identificación general'); await sticky(page);
                await scroll(page).evaluate(el => { el.scrollLeft = 0; });
                await row(page, 'manual', 'identificacion').getByRole('button', { name: 'Eliminar registro', exact: true }).click();
                await expect(page.getByRole('dialog')).toContainText('Las respuestas se conservarán');
                await page.getByRole('button', { name: 'Eliminar', exact: true }).click();
                await expect(page.getByRole('dialog')).toHaveCount(0);
                assert.equal(fixture.content.sistemas.find(r => r.id === 'd').sistema, 'formulario');
                assert.equal(fixture.content.sistemas.find(r => r.id === 'd').proceso, '');
                await capture(page, `identification-${key}`);
            });
            await check(`${key}: muchas filas, desplazamiento vertical y última acción`, async () => {
                const procedure = fixture.content.identificacion[0];
                for (let i = 4; i < 35; i++) fixture.content.identificacion.push({ ...procedure, id: `procedure-${i}`, codigo: `PO-${i}`, tramite: `Procedimiento ${i}` });
                for (let i = 4; i < 35; i++) fixture.content.sistemas.push({ ...base, id: `extra-${i}`, proceso: 'PO-02', sistema: 'otra', sistemaOtro: `Herramienta ${i}`, uso: 'consultar', estado: 'bien' });
                fixture.broadcast(); await tab(page, 'Sistemas y herramientas');
                await sticky(page);
                const last = row(page, 'extra-34').getByRole('button', { name: 'Eliminar registro', exact: true });
                await last.scrollIntoViewIfNeeded(); await last.click();
                await expect(page.getByRole('dialog')).toBeVisible(); await page.getByRole('button', { name: 'Eliminar', exact: true }).click();
                await expect(row(page, 'extra-34')).toHaveCount(0);
                await capture(page, `many-rows-${key}`);
                await tab(page, 'Identificación general'); await sticky(page);
                await row(page, 'procedure-34', 'identificacion').getByRole('button', { name: 'Eliminar registro', exact: true }).scrollIntoViewIfNeeded();
                await row(page, 'procedure-34', 'identificacion').getByRole('button', { name: 'Eliminar registro', exact: true }).click();
                await expect(page.getByRole('dialog')).toBeVisible(); await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
                assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
                fixture.readOnly = true; await page.reload(); await tab(page, 'Sistemas y herramientas');
                await expect(row(page, 'a').getByRole('button', { name: 'Eliminar registro', exact: true })).toBeDisabled();
            });
            await check(`${key}: Acciones en Datos y Acuerdos conserva presencia y confirmaciones`, async () => {
                fixture.readOnly = false; fixture.laterSections = true;
                fixture.content.datos = [{ id: 'dato', proceso: 'PO-01', dato: 'Dato sintético', fuente: 'Local', origen: '', detalle: '' }];
                fixture.content.acuerdos = [{ id: 'acuerdo', acuerdo: 'Acuerdo sintético', responsable: 'Área de prueba', fecha: '' }];
                await page.reload(); await page.getByRole('switch', { name: 'Mostrar secciones posteriores' }).check();
                for (const [name, section, id, label] of [['Datos', 'datos', 'dato', 'Dato o información 1'], ['Acuerdos', 'acuerdos', 'acuerdo', 'Acuerdo 1']]) {
                    await tab(page, name); await sticky(page);
                    const field = row(page, id, section).getByLabel(label, { exact: true });
                    await positionField(page, field); await field.click({ position: { x: 10, y: 12 } });
                    await expect(row(page, id, section).getByText('Estás editando', { exact: true })).toBeVisible();
                    await row(page, id, section).getByRole('button', { name: 'Eliminar registro', exact: true }).click();
                    await expect(page.getByRole('dialog')).toBeVisible(); await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
                    await save(page);
                }
            });
        } finally { for (const c of contexts) await c.close(); await fixture.close(); }
    }
    assert.equal(report.errors.length, 0, report.errors.join('\n')); report.completed = true;
} finally { await writeFile(path.join(output, 'internal-report.json'), JSON.stringify(report, null, 2)); await browser.close(); }
