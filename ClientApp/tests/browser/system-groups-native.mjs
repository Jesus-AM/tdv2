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
const systems = async () => { await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click(); await expect(page.locator('[data-add-row="sistemas"]')).toBeEnabled(); };
const review = () => page.getByRole('tab', { name: 'Revisar y enviar', exact: true }).click();
const finish = async () => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); };
const select = async (label, value) => { await page.getByRole('combobox', { name: label, exact: true }).click(); await page.getByRole('option', { name: value, exact: true }).click(); };
const row = id => page.locator(`[data-edit-block="sistemas:${id}"]`);
const heading = id => page.locator(`[data-system-group-heading="procedure:${id}"]`);
const groupRows = id => page.locator(`[data-system-group="${id}"]`);
const region = () => page.getByRole('region', { name: 'Tabla de Sistemas y herramientas', exact: true });
const ids = () => page.locator('.systems-table [data-edit-block]').evaluateAll(nodes => nodes.map(n => n.dataset.editBlock));
let addedId;
try {
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    await control('system-groups-seed'); await page.goto(origin + '/formatos/A'); await systems();
    await expect(heading('p8').getByRole('button', { name: 'Agregar sistema o herramienta', exact: true })).toBeEnabled();
    await check('filas intercaladas se agrupan por relación estable sin reordenar el JSON', async () => {
        await expect(page.locator('[data-system-group-heading]')).toHaveCount(4);
        assert.deepEqual(await ids(), ['sistemas:a1', 'sistemas:b1', 'sistemas:b2', 'sistemas:orphan']);
        assert.deepEqual((await stored()).sistemas.map(r => r.id), ['b1', 'a1', 'b2', 'orphan']);
        await expect(heading('p10')).toContainText('PO-10'); await expect(groupRows('procedure:p10')).toHaveCount(0);
        await expect(page.locator('[data-system-group-heading="unassigned"]')).toContainText('Sin procedimiento asignado');
        await expect(page.getByText('Procedimientos y captura de herramientas', { exact: true })).toHaveCount(0);
    });
    await check('reasignar con el primer clic mueve la misma fila y conserva su control, respuestas y foco', async () => {
        const field = row('b1').getByRole('combobox', { name: 'Procedimiento 1', exact: true });
        const handle = await field.elementHandle(), rowHandle = await row('b1').elementHandle();
        await field.click(); await expect(page.getByRole('listbox')).toBeVisible();
        await page.getByRole('option', { name: /PO-08/ }).click();
        await expect(row('b1')).toHaveAttribute('data-system-group', 'procedure:p8');
        assert.ok(await field.evaluate((node, old) => node === old, handle));
        assert.ok(await row('b1').evaluate((node, old) => node === old, rowHandle));
        await expect(field).toBeFocused();
        await expect(row('b1').getByRole('textbox', { name: 'Áreas de oportunidad 1', exact: true })).toHaveValue('Respuesta histórica conservada: b1');
        await finish();
        assert.deepEqual((await stored()).sistemas.map(r => r.id), ['b1', 'a1', 'b2', 'orphan']);
        assert.deepEqual(await ids(), ['sistemas:b1', 'sistemas:a1', 'sistemas:b2', 'sistemas:orphan']);
    });
    await check('alta explícita desde un grupo vacío asigna procedimiento y conserva el orden almacenado', async () => {
        const action = heading('p10').getByRole('button', { name: 'Agregar sistema o herramienta', exact: true });
        await action.focus(); await page.keyboard.press('Enter');
        await expect(groupRows('procedure:p10')).toHaveCount(1); await finish();
        const content = await stored(); addedId = content.sistemas.at(-1).id;
        assert.equal(content.sistemas.at(-1).proceso, 'PO-10');
        assert.deepEqual(content.sistemas.slice(0, 4).map(r => r.id), ['b1', 'a1', 'b2', 'orphan']);
        await review();
        await expect(page.locator(`[data-pending-block="sistemas:${addedId}"]`)).toHaveCount(3);
        await page.locator(`[data-pending-block="sistemas:${addedId}"][data-pending-field="sistema"]`).getByRole('button', { name: 'Ir al campo' }).click();
        await expect(row(addedId).getByRole('combobox', { name: 'Sistema o herramienta 5', exact: true })).toBeFocused();
        await page.keyboard.press('ArrowDown');
        await page.getByRole('option', { name: 'Otra (escríbela)', exact: true }).click();
        await row(addedId).getByRole('textbox', { name: 'Nombre de la herramienta 5', exact: true }).fill('Herramienta sintética de captura con nombre largo y acentos');
        await select('¿Para qué se usa? 5', 'Otro (escríbelo)');
        await row(addedId).getByRole('textbox', { name: 'Describe el uso 5', exact: true }).fill('Atender solicitudes y conservar el seguimiento');
        await select('¿Cómo funciona? 5', 'Funciona con fallas'); await finish();
        await page.reload(); await systems();
        await expect(groupRows('procedure:p10')).toHaveCount(1);
        await expect(row(addedId).getByRole('textbox', { name: 'Nombre de la herramienta 5', exact: true })).toHaveValue('Herramienta sintética de captura con nombre largo y acentos');
    });
    await check('autoguardado mantiene scroll, selección, texto y reserva; otra pestaña observa la fila ocupada', async () => {
        const field = row('a1').getByRole('textbox', { name: 'Áreas de oportunidad 2', exact: true });
        await field.click(); await expect(field).not.toHaveAttribute('readonly');
        await field.fill('Texto completo con acentos\nSegunda línea\nTercera línea\nCuarta línea\nQuinta línea que puede recorrerse');
        await field.evaluate(el => el.setSelectionRange(6, 14));
        await region().evaluate(el => { el.scrollLeft = el.scrollWidth; });
        const before = await region().evaluate(el => ({ top: el.scrollTop, left: el.scrollLeft }));
        const rowHandle = await row('a1').elementHandle();
        await expect.poll(async () => (await stored()).sistemas.find(r => r.id === 'a1').fallas).toContain('Quinta línea');
        assert.deepEqual(await region().evaluate(el => ({ top: el.scrollTop, left: el.scrollLeft })), before);
        assert.deepEqual(await field.evaluate(el => [el.selectionStart, el.selectionEnd]), [6, 14]);
        assert.ok(await row('a1').evaluate((el, old) => el === old, rowHandle));
        const other = await context.newPage(); await other.goto(origin + '/formatos/A');
        await other.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
        await expect(other.locator('[data-edit-block="sistemas:a1"]').getByRole('button', { name: 'Eliminar registro', exact: true })).toBeDisabled();
        await other.close(); await finish();
    });
    await check('renombrar un procedimiento actualiza el encabezado y mantiene todas sus asociaciones', async () => {
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const name = page.getByRole('textbox', { name: 'Procedimiento 1', exact: true });
        await name.click(); await expect(name).not.toHaveAttribute('readonly');
        await name.fill('Nombre completo actualizado: revisión y seguimiento de solicitudes para estudiantes y egresados'); await finish();
        await systems(); await expect(heading('p8')).toContainText('PO-08 · Nombre completo actualizado');
        await expect(groupRows('procedure:p8')).toHaveCount(2);
        assert.ok((await stored()).sistemas.filter(r => ['a1', 'b1'].includes(r.id)).every(r => r.proceso === 'PO-08'));
    });
    await check('eliminar procedimiento conserva respuestas, las agrupa sin asociación y permite Ir al campo', async () => {
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        await page.locator('[data-edit-block="identificacion:p8"]').getByRole('button', { name: 'Eliminar registro', exact: true }).click();
        await expect(page.getByRole('dialog')).toContainText('2 registros de sistemas/herramientas o datos perderán su asociación');
        await page.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click();
        await expect(page.locator('[data-edit-block="identificacion:p8"]')).toHaveCount(0);
        await systems(); await expect(groupRows('unassigned')).toHaveCount(3); await expect(heading('p8')).toHaveCount(0);
        assert.equal((await stored()).sistemas.find(r => r.id === 'b1').fallas, 'Respuesta histórica conservada: b1');
        await review();
        await page.locator('[data-pending-block="sistemas:b1"][data-pending-field="proceso"]').getByRole('button', { name: 'Ir al campo' }).click();
        await expect(row('b1').getByRole('combobox', { name: 'Procedimiento 1', exact: true })).toBeFocused();
        await expect(page.getByRole('option', { name: /PO-09/ })).toHaveAttribute('aria-disabled', 'true');
        await page.getByRole('option', { name: /PO-10/ }).click(); await finish();
        await expect(groupRows('procedure:p10')).toHaveCount(2);
    });
    await check('grupos, campos condicionales y encabezados completos en escritorio y móvil con desplazamiento', async () => {
        for (const width of [1366, 390, 320]) {
            await page.setViewportSize({ width, height: 900 });
            await region().evaluate(el => { el.scrollTop = 0; el.scrollLeft = 0; });
            await region().scrollIntoViewIfNeeded();
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            const group = heading('p9').locator('h3');
            await expect(group).toContainText('comprobar la distribución en dispositivos móviles');
            assert.ok(await group.evaluate(el => el.scrollHeight <= el.clientHeight + 1));
            const action = heading('p9').getByRole('button', { name: 'Agregar sistema o herramienta', exact: true });
            assert.ok(await action.evaluate(el => el.getBoundingClientRect().right <= el.closest('[data-edit-scroll]').getBoundingClientRect().right + 1));
            await page.screenshot({ path: path.join(artifacts, `system-groups-${width}.png`), fullPage: true });
            await region().screenshot({ path: path.join(artifacts, `system-groups-table-${width}.png`) });
            await region().evaluate(el => { el.scrollLeft = el.scrollWidth; el.scrollTop = el.scrollHeight; });
            const head = page.locator('.systems-table thead');
            assert.ok(await head.evaluate(el => {
                const r = el.querySelector('th').getBoundingClientRect(), container = el.closest('[data-edit-scroll]').getBoundingClientRect();
                return Math.abs(r.top - container.top) <= 2;
            }));
            await row('a1').getByRole('button', { name: 'Eliminar registro', exact: true }).scrollIntoViewIfNeeded();
            await expect(row('a1').getByRole('button', { name: 'Eliminar registro', exact: true })).toBeVisible();
            await page.screenshot({ path: path.join(artifacts, `system-groups-actions-${width}.png`), fullPage: true });
        }
    });
    await check('el toque abre el selector al primer intento y Escape permite continuar por teclado', async () => {
        const input = row('b2').getByRole('combobox', { name: 'Procedimiento 3', exact: true });
        await input.tap(); await expect(page.getByRole('listbox')).toBeVisible();
        await expect(page.getByRole('option', { name: /PO-10/ })).toContainText('dispositivos móviles');
        await page.keyboard.press('Escape'); await expect(input).toBeFocused();
        await page.keyboard.press('Tab');
        await expect(row('b2').getByRole('combobox', { name: 'Sistema o herramienta 3', exact: true })).toBeFocused();
        await finish(); await page.reload(); await systems();
        assert.deepEqual((await stored()).sistemas.map(r => r.id), ['b1', 'a1', 'b2', 'orphan', addedId]);
    });
    await check('textos de todas las secciones y opciones históricas: escritorio, móvil y claves intactas', async () => {
        await control('configuration-admin'); await page.reload();
        await page.getByRole('switch', { name: 'Mostrar secciones posteriores', exact: true }).check();
        const original = await stored();
        for (const width of [1366, 390]) {
            await page.setViewportSize({ width, height: 900 });
            for (const tab of ['Contexto', 'Identificación general', 'Sistemas y herramientas', 'Datos', 'Evaluación', 'Preguntas', 'Acuerdos', 'Revisar y enviar']) {
                await page.getByRole('tab', { name: tab, exact: true }).click();
                await expect(page.getByRole('tabpanel')).toBeVisible();
                assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
                if (tab === 'Preguntas') {
                    await expect(page.getByRole('tabpanel')).toContainText('¿Qué pasos consideras innecesarios');
                    await expect(page.getByRole('tabpanel')).toContainText('¿Qué parte del procedimiento conviene automatizar o digitalizar primero y por qué?');
                    await page.getByRole('combobox', { name: 'Respuesta 1', exact: true }).click();
                    await expect(page.getByRole('option', { name: 'Ninguno o muy pocos', exact: true })).toBeVisible();
                    await page.keyboard.press('Escape');
                    await expect(page.getByRole('listbox')).toHaveCount(0);
                }
                await page.screenshot({ path: path.join(artifacts, `form-texts-${tab.replaceAll(' ', '-')}-${width}.png`), fullPage: true });
            }
        }
        const after = await stored();
        for (const field of ['preguntas', 'medios', 'datos', 'evaluaciones', 'acuerdos']) assert.deepEqual(after[field], original[field]);
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally {
    await page.screenshot({ path: path.join(artifacts, 'system-groups-final.png'), fullPage: true }).catch(() => {});
    await writeFile(path.join(artifacts, 'system-groups-native-browser.json'), JSON.stringify(report, null, 2));
    await context.close(); await browser.close();
}
