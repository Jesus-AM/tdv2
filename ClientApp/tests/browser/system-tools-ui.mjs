import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';

const artifacts = path.resolve('../.artifacts/system-tools-ui'); await mkdir(artifacts, { recursive: true });
const fixture = await systemFixture(), browser = await chromium.launch({ channel: 'msedge', headless: true });
fixture.content.sistemas[0].fallas = 'Comentario anterior conservado\nSin cambiar su asociación';
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
async function select(page, label, option) {
    await choice(page, label).click();
    try { await page.getByRole('option', { name: option, exact: true }).click(); }
    catch (error) { await page.screenshot({ path: path.join(artifacts, 'selection-failure.png'), fullPage: true }); throw error; }
}
async function finish(page) {
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0);
    await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/);
}
async function typography(group, floating) {
    await expect(group.locator('.MuiInputLabel-root')).toHaveAttribute('data-shrink', String(floating));
    const metrics = await group.evaluate(el => {
        const primary = el.previousElementSibling.querySelector('.MuiInputBase-root');
        const input = el.querySelector('input, textarea'), label = el.querySelector('label');
        const font = element => { const s = getComputedStyle(element); return [s.fontFamily, s.fontSize, s.fontWeight]; };
        return { primary: font(primary), input: font(input), label: font(label), placeholder: getComputedStyle(input, '::placeholder').fontSize,
            gap: label.getBoundingClientRect().top - primary.getBoundingClientRect().bottom,
            labelLeft: label.getBoundingClientRect().left - el.getBoundingClientRect().left,
            legendSize: getComputedStyle(el.querySelector('legend')).fontSize };
    });
    assert.equal(metrics.primary[1], '13px'); assert.deepEqual(metrics.input, metrics.primary); assert.deepEqual(metrics.label, metrics.primary);
    assert.equal(metrics.placeholder, '13px'); assert.equal(metrics.legendSize, '9.75px');
    assert.ok(metrics.gap >= 10 && metrics.labelLeft >= 20, JSON.stringify(metrics));
}
try {
    const { page } = await session();
    await expect(page.getByLabel('Áreas de oportunidad 1', { exact: true })).toHaveValue(fixture.content.sistemas[0].fallas);
    await check('catálogo pendiente, SII vacío persiste como borrador y los otros campos siguen disponibles', async () => {
        await select(page, 'Sistema o herramienta', 'SIIv2');
        await expect(page.getByText('El catálogo de módulos de SIIv2 aún no está disponible', { exact: true })).toBeVisible();
        await select(page, '¿Para qué se usa?', 'Consultar información'); await finish(page);
        assert.equal(fixture.content.sistemas[0].sistema, 'sii_v2'); assert.equal(fixture.content.sistemas[0].moduloSiiId, '');
    });
    await check('catálogo muestra nombres sin ID añadido; duplicados conservan su ID al guardar y recargar', async () => {
        fixture.catalog = { estado: 'error', modulos: [] }; await page.reload(); await systems(page);
        await expect(page.getByText(/No fue posible consultar el catálogo local de módulos/)).toBeVisible();
        fixture.catalog = { estado: 'disponible', modulos: [{ id: '1', descripcion: 'Solicitudes' }, { id: '2', descripcion: 'Consultas' }, { id: '3', descripcion: 'Consultas' }] }; fixture.delay = 1200;
        await page.reload(); await systems(page); await expect(page.getByText('Cargando catálogo local…')).toBeVisible(); fixture.delay = 0;
        const module = choice(page, 'Módulos de SIIv2'); await module.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await module.fill('Consultas'); await expect(page.getByRole('option', { name: 'Consultas', exact: true })).toHaveCount(2);
        await page.getByRole('option', { name: 'Consultas', exact: true }).nth(1).click(); await finish(page);
        assert.equal(fixture.content.sistemas[0].modulosSii[0].id, '3'); await page.reload(); await systems(page);
        await expect(row(page).locator('.sii-module-value')).toHaveText('Consultas');
        await expect(choice(page, 'Módulos de SIIv2')).toHaveAccessibleDescription('Selecciona uno o varios módulos utilizados para este procedimiento.');
        await module.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned'); await module.press('ArrowDown');
        await expect(page.getByRole('option', { name: 'Consultas', exact: true }).nth(0)).toHaveAttribute('aria-selected', 'false');
        await expect(page.getByRole('option', { name: 'Consultas', exact: true }).nth(1)).toHaveAttribute('aria-selected', 'true');
        await page.keyboard.press('Escape'); await finish(page);
        fixture.catalog.modulos.find(m => m.id === '3').descripcion = 'Consultas actualizadas';
        fixture.catalog.modulos.push({ id: '34', descripcion: 'Nombre legítimo · ID 34' });
        await page.reload(); await systems(page); await expect(row(page).locator('.sii-module-value')).toHaveText('Consultas');
        await module.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await row(page).locator('.sii-module-select .MuiAutocomplete-clearIndicator').click(); await module.fill('Nombre legítimo');
        await page.getByRole('option', { name: 'Nombre legítimo · ID 34', exact: true }).click(); await finish(page);
        assert.equal(fixture.content.sistemas[0].modulosSii[0].id, '34');
        await page.reload(); await systems(page); await expect(row(page).locator('.sii-module-value')).toHaveText('Nombre legítimo · ID 34');
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
        await page.getByLabel('Áreas de oportunidad 1', { exact: true }).click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await page.getByLabel('Áreas de oportunidad 1', { exact: true }).fill('En horas pico\nNo exporta'); await finish(page);
        await page.reload(); await systems(page);
        await expect(page.getByLabel('Áreas de oportunidad 1', { exact: true })).toHaveValue('En horas pico\nNo exporta');
        await page.getByRole('button', { name: 'Ayuda: Áreas de oportunidad', exact: true }).click();
        await expect(page.getByRole('dialog').getByRole('heading', { name: 'Áreas de oportunidad', exact: true })).toBeVisible();
        await page.keyboard.press('Escape');
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
    await check('Egresados conserva selección múltiple, reserva, guardado y recarga', async () => {
        const recipients = choice(page, '¿A quién atiende?'); await recipients.click();
        await expect(page.locator('[data-edit-block="identificacion:inicial"]')).toHaveAttribute('data-edit-state', 'owned');
        await page.getByRole('option', { name: 'Egresados', exact: true }).click();
        await expect(page.getByRole('option', { name: 'Docentes', exact: true })).toHaveAttribute('aria-selected', 'true');
        await expect(page.getByRole('option', { name: 'Egresados', exact: true })).toHaveAttribute('aria-selected', 'true');
        await page.keyboard.press('Escape'); await finish(page);
        assert.deepEqual(fixture.content.identificacion[0].usuario, ['Docentes', 'Egresados']);
        await page.reload(); await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        await expect(recipients).toHaveText('Docentes, Egresados');
    });
    await check('escritorio/móvil: Acciones en una línea, botón y foco íntegros con scroll horizontal', async () => {
        for (const width of [1920, 1366, 768, 360]) {
            await page.setViewportSize({ width, height: 900 }); await systems(page);
            await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width + 1);
            assert.equal(await row(page).locator('.MuiInputBase-root').first().evaluate(e => getComputedStyle(e).fontSize), '13px');
            await choice(page, 'Sistema o herramienta').focus(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
            await page.keyboard.press('Enter'); await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
            await expect(choice(page, 'Sistema o herramienta')).toBeFocused(); await finish(page);
            await page.evaluate(() => { document.activeElement?.blur(); window.scrollTo({ top: 0, behavior: 'instant' }); });
            await page.screenshot({ path: path.join(artifacts, `systems-${width}.png`), fullPage: true });
            if (width === 360) {
                await page.locator('.capture-scroll').evaluate(e => { e.scrollLeft = 280; });
                await page.screenshot({ path: path.join(artifacts, 'systems-360-tools.png'), fullPage: true });
            }
            const scroller = page.locator('.capture-row-scroll');
            await scroller.evaluate(e => { e.scrollLeft = e.scrollWidth; });
            const header = page.getByRole('columnheader', { name: 'Acciones', exact: true });
            const metrics = await header.evaluate(el => {
                const text = document.createRange(); text.selectNodeContents(el);
                return { width: el.getBoundingClientRect().width, lines: text.getClientRects().length, whiteSpace: getComputedStyle(el).whiteSpace };
            });
            assert.ok(metrics.width >= 100 && metrics.width <= 120); assert.equal(metrics.lines, 1); assert.equal(metrics.whiteSpace, 'nowrap');
            const button = row(page).getByRole('button', { name: 'Eliminar registro', exact: true });
            await button.focus(); await page.keyboard.press('Tab');
            await expect(page.locator('[data-edit-block^="sistemas:"]').nth(1)).toHaveAttribute('data-edit-state', 'owned');
            // La reserva confirmada incorpora el avatar accesible antes del primer campo.
            await page.keyboard.press('Shift+Tab');
            await expect(page.locator('[data-edit-block^="sistemas:"]').nth(1).locator('[data-participant-avatar]')).toBeFocused();
            await page.keyboard.press('Shift+Tab'); await expect(button).toBeFocused();
            const bounds = await button.boundingBox(), cell = await row(page).locator('.row-actions-cell').boundingBox(), head = await header.boundingBox();
            assert.ok(bounds.width >= 44 && bounds.height >= 44 && bounds.x >= cell.x + 4 && bounds.x + bounds.width <= cell.x + cell.width - 4);
            assert.ok(Math.abs((bounds.x + bounds.width / 2) - (head.x + head.width / 2)) < 1);
            const outline = await button.evaluate(e => getComputedStyle(e).outlineWidth); assert.equal(outline, '2px');
            const visible = await scroller.boundingBox(); assert.ok(bounds.x >= visible.x + 124 && bounds.x + bounds.width + 4 < visible.x + visible.width);
            await page.screenshot({ path: path.join(artifacts, `actions-${width}.png`), fullPage: true });
            await button.click(); await expect(page.getByRole('dialog')).toBeVisible(); await page.keyboard.press('Escape');
            await expect(page.getByRole('dialog')).toBeHidden();
        }
    });
    await check('dependientes agrupados: foco, Tab, nombres largos y errores locales en escritorio/móvil', async () => {
        const longModule = 'Administración y seguimiento de solicitudes académicas y servicios institucionales';
        fixture.catalog.modulos.push({ id: '77', descripcion: longModule }, { id: '78', descripcion: longModule + ' actualizado' });
        await page.reload(); await systems(page);
        const cases = [
            { primary: 'Sistema o herramienta', option: 'SIIv2', title: 'Módulos de SIIv2', field: 'modulosSii' },
            { primary: 'Sistema o herramienta', option: 'Otra (escríbela)', title: 'Nombre de la herramienta', field: 'sistemaOtro' },
            { primary: '¿Para qué se usa?', option: 'Otro (escríbelo)', title: 'Describe el uso', field: 'usoOtro' },
        ];
        for (const width of [1366, 360]) {
            await page.setViewportSize({ width, height: 900 });
            for (const item of cases) {
                // Volver a una opción sin detalle permite comprobar su aparición y el retorno de foco.
                await select(page, item.primary, item.field === 'usoOtro' ? 'Consultar información' : 'Hoja de cálculo (Excel)');
                const tableWidth = await page.locator('.systems-table').evaluate(el => el.getBoundingClientRect().width);
                await select(page, item.primary, item.option);
                const principal = choice(page, item.primary), detail = page.getByLabel(item.title + ' 1', { exact: true });
                await expect(principal).toBeFocused(); await page.keyboard.press('Tab'); await expect(detail).toBeFocused();
                await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
                const group = row(page).getByRole('group', { name: item.title, exact: true });
                await typography(group, true);
                const layout = await group.evaluate(el => {
                    const primary = el.previousElementSibling.getBoundingClientRect(), box = el.getBoundingClientRect();
                    const field = el.querySelector('.MuiFormControl-root').getBoundingClientRect(), style = getComputedStyle(el);
                    return { indent: field.x - primary.x, lostWidth: primary.width - field.width, gap: box.y - primary.bottom,
                        border: style.borderLeftWidth, color: style.borderLeftColor, sameCell: el.closest('td') === el.previousElementSibling.closest('td') };
                });
                assert.ok(layout.sameCell && layout.indent >= 10 && layout.indent <= 24 && layout.lostWidth <= 24);
                assert.ok(layout.gap >= 6 && layout.gap <= 12); assert.equal(layout.border, '2px');
                assert.match(layout.color, /^rgba\(36, 91, 182, 0\.28\)$/);
                assert.equal(await page.locator('.systems-table').evaluate(el => el.getBoundingClientRect().width), tableWidth);
                const releases = fixture.requests.filter(r => r.path.endsWith('/liberar')).length;
                const message = `Revisa ${item.title.toLowerCase()}: el valor no pudo confirmarse.`;
                const reject = route => route.fulfill({ status: 422, contentType: 'application/json', body: JSON.stringify({ message,
                    validation: [{ block: 'sistemas:inicial', field: item.field, message }] }) });
                await page.route('**/formatos/A/bloques', reject);
                try {
                    if (item.field === 'modulosSii') {
                        await group.locator('.MuiAutocomplete-clearIndicator').click(); await detail.fill('Administración'); await page.getByRole('option', { name: longModule, exact: true }).click();
                        await expect(group.locator('.sii-module-value')).toHaveText(longModule);
                    } else await detail.fill('Herramienta o actividad institucional con nombre largo y detalle completo\nSegunda línea sin recortar.');
                    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
                    await expect(detail).toHaveAttribute('aria-invalid', 'true');
                    await expect(group.getByText(message, { exact: true })).toBeVisible();
                    await typography(group, true);
                    const description = await detail.getAttribute('aria-describedby'); assert.ok(description);
                    await expect(detail).toHaveAccessibleDescription(message);
                    assert.equal(fixture.requests.filter(r => r.path.endsWith('/liberar')).length, releases);
                    await detail.focus();
                    // Mostrar el inicio del grupo junto a Edición durante la revisión visual del scroll.
                    await group.evaluate(el => {
                        const scroller = el.closest('.capture-row-scroll');
                        scroller.scrollLeft += el.getBoundingClientRect().left - scroller.getBoundingClientRect().left - 132;
                    });
                    await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' }));
                    await page.screenshot({ path: path.join(artifacts, `dependent-${item.field}-${width}.png`), fullPage: true });
                } finally { await page.unroute('**/formatos/A/bloques', reject); }
                if (item.field === 'modulosSii') {
                    await group.locator('.MuiAutocomplete-clearIndicator').click(); await detail.fill('actualizado'); await page.getByRole('option', { name: longModule + ' actualizado', exact: true }).click();
                } else await detail.fill('Detalle corregido que conserva el texto y sus saltos\nSegunda línea.');
                await finish(page); await expect(detail).not.toHaveAttribute('aria-invalid', 'true');
                const expected = item.field === 'modulosSii' ? '78' : 'Detalle corregido que conserva el texto y sus saltos\nSegunda línea.';
                assert.equal(item.field === 'modulosSii' ? fixture.content.sistemas[0].modulosSii[0].id : fixture.content.sistemas[0][item.field], expected);
            }
            await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width + 1);
        }
    });
    await check('tipografía uniforme vacía/completa; módulo multilínea, búsqueda y teclado sin recortar en escritorio/móvil', async () => {
        const longModule = 'Administración y seguimiento de solicitudes académicas y servicios institucionales actualizado';
        for (const width of [1366, 360]) {
            await page.setViewportSize({ width, height: 900 });
            const cases = [
                { primary: 'Sistema o herramienta', option: 'SIIv2', title: 'Módulos de SIIv2', module: true },
                { primary: 'Sistema o herramienta', option: 'Otra (escríbela)', title: 'Nombre de la herramienta' },
                { primary: '¿Para qué se usa?', option: 'Otro (escríbelo)', title: 'Describe el uso' },
            ];
            for (const item of cases) {
                await select(page, item.primary, item.option);
                const group = row(page).getByRole('group', { name: item.title, exact: true });
                const detail = page.getByLabel(item.title + ' 1', { exact: true }), principal = choice(page, item.primary);
                await detail.click(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
                if (item.module) {
                    await group.locator('.MuiAutocomplete-clearIndicator').click();
                    await page.keyboard.press('Escape');
                } else await detail.fill('');
                await principal.focus(); await typography(group, false);
                await page.keyboard.press('Tab'); await expect(detail).toBeFocused(); await typography(group, true);
                const tableWidth = await page.locator('.systems-table').evaluate(el => el.getBoundingClientRect().width);
                const releases = fixture.requests.filter(r => r.path.endsWith('/liberar')).length;
                if (item.module) {
                    await detail.fill('actualizado');
                    const option = page.getByRole('option', { name: longModule, exact: true });
                    await expect(option).toBeVisible();
                    const optionLayout = await option.evaluate(el => ({ height: el.clientHeight, content: el.scrollHeight,
                        size: getComputedStyle(el).fontSize, width: el.clientWidth, contentWidth: el.scrollWidth }));
                    assert.equal(optionLayout.size, '13px'); assert.ok(optionLayout.height > 40);
                    assert.ok(optionLayout.content <= optionLayout.height + 1 && optionLayout.contentWidth <= optionLayout.width + 1);
                    await detail.press('ArrowDown'); await detail.press('Enter');
                    await expect(group.locator('.sii-module-value')).toHaveText(longModule); await expect(detail).toBeFocused();
                    await expect(detail).toHaveAccessibleDescription('Selecciona uno o varios módulos utilizados para este procedimiento.');
                    const layout = await group.evaluate(el => {
                        const value = el.querySelector('.sii-module-value'), arrow = el.querySelector('.MuiAutocomplete-endAdornment');
                        const bounds = value.getBoundingClientRect(), control = el.querySelector('.MuiInputBase-root').getBoundingClientRect();
                        return { height: bounds.height, lineHeight: parseFloat(getComputedStyle(value).lineHeight), contentHeight: value.scrollHeight,
                            visibleHeight: value.clientHeight, valueRight: bounds.right, arrowLeft: arrow.getBoundingClientRect().left,
                            endGap: control.right - arrow.getBoundingClientRect().right,
                            contentWidth: value.scrollWidth, visibleWidth: value.clientWidth, controlBottom: control.bottom, valueBottom: bounds.bottom };
                    });
                    assert.ok(layout.height > layout.lineHeight * 2, JSON.stringify(layout));
                    assert.ok(layout.contentHeight <= layout.visibleHeight + 1 && layout.contentWidth <= layout.visibleWidth + 1);
                    assert.ok(layout.valueRight <= layout.arrowLeft && layout.valueBottom <= layout.controlBottom, JSON.stringify(layout));
                    assert.ok(layout.endGap >= 0 && layout.endGap <= 8, JSON.stringify(layout));
                    await detail.press('ArrowDown'); await expect(page.getByRole('listbox')).toBeVisible();
                    await detail.press('Escape'); await expect(group.locator('.sii-module-value')).toHaveText(longModule);
                } else await detail.fill('Detalle completo\nSegunda línea');
                await typography(group, true);
                await group.evaluate(el => {
                    const scroller = el.closest('.capture-row-scroll');
                    scroller.scrollLeft += el.getBoundingClientRect().left - scroller.getBoundingClientRect().left - 132;
                });
                await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
                assert.equal(fixture.requests.filter(r => r.path.endsWith('/liberar')).length, releases);
                assert.equal(await page.locator('.systems-table').evaluate(el => el.getBoundingClientRect().width), tableWidth);
                await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' }));
                await page.screenshot({ path: path.join(artifacts, `typography-${item.module ? 'module' : item.title}-${width}.png`), fullPage: true });
                await finish(page);
                if (item.module) {
                    assert.equal(fixture.content.sistemas[0].modulosSii[0].id, '78');
                    await page.reload(); await systems(page);
                    await expect(row(page).locator('.sii-module-value')).toHaveText(longModule);
                }
            }
            await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width + 1);
        }
        await select(page, 'Sistema o herramienta', 'SIIv2'); await finish(page);
        fixture.readOnly = true; await page.reload(); await systems(page);
        await expect(row(page).locator('.sii-module-value')).toHaveText(longModule);
        await expect(choice(page, 'Módulos de SIIv2')).toHaveJSProperty('readOnly', true);
        fixture.readOnly = false;
    });
    await check('históricos y enviados conservan valores y cierran la edición', async () => {
        fixture.content.sistemas[0].sistema = 'Herramienta histórica'; fixture.content.sistemas[0].uso = 'Uso histórico'; fixture.content.sistemas[0].estado = 'Parcial'; fixture.submitted = true; fixture.version++;
        await page.reload(); await systems(page); await expect(choice(page, 'Sistema o herramienta')).toHaveText('Herramienta histórica');
        await choice(page, 'Sistema o herramienta').click(); await expect(page.getByRole('listbox')).toHaveCount(0);
        await expect(page.getByLabel('Áreas de oportunidad 1', { exact: true })).toHaveJSProperty('readOnly', true);
        await expect(page.getByLabel('Áreas de oportunidad 1', { exact: true })).toHaveValue('En horas pico\nNo exporta');
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
