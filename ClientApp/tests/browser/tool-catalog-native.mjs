// Navegador real, ASP.NET y PostgreSQL desechable. Sólo identidad/fuentes sintéticas.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw Error('Sólo entorno aislado');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 900 }, hasTouch: true });
await context.route('**/*', async route => {
    const url = new URL(route.request().url()); if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const page = await context.newPage(); page.setDefaultTimeout(20000);
const report = { realBackend: true, realPostgreSQL: true, syntheticIdentityAndSources: true, checks: [], errors: [] };
page.on('pageerror', e => report.errors.push(e.message));
const check = async (name, run) => { await run(); report.checks.push(name); console.log('NATIVE UI PASS ' + name); };
const control = async name => assert.equal((await context.request.post(origin + '/__fixture/' + name, { headers: { 'X-Fixture-Key': key } })).status(), 200);
const stored = async () => (await (await context.request.get(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } })).json()).contenido;
const tab = name => page.getByRole('tab', { name, exact: true }).click();
const finish = async () => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); };
const row = id => page.locator(`[data-edit-block="sistemas:${id}"]`);
const field = (id, name) => row(id).locator(`[data-field="${name}"]`).filter({ visible: true });
const choose = async (id, name, value) => { await field(id, name).click(); await expect(page.getByRole('listbox')).toBeVisible(); await page.getByRole('option', { name: value, exact: true }).click(); };
const add = async procedure => {
    const count = await page.locator('[data-edit-block^="sistemas:"]').count();
    await page.locator(`[data-system-group-heading="procedure:${procedure}"]`).getByRole('button', { name: 'Agregar sistema o herramienta', exact: true }).click();
    await expect(page.locator('[data-edit-block^="sistemas:"]')).toHaveCount(count + 1);
    await finish(); return (await stored()).sistemas.at(-1).id;
};
const complete = async id => { await choose(id, 'uso', 'Consultar información'); await choose(id, 'estado', 'Funciona bien'); await finish(); };
let a, b, c;
try {
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    await control('tool-catalog-seed'); await control('systems-sync'); await control('applications-names');
    await page.goto(origin + '/formatos/A'); await tab('Revisar y enviar');
    await check('procedimientos sin herramientas listos y Medios ausente', async () => {
        await expect(page.locator('[data-pending-block]')).toHaveCount(0);
        await expect(page.getByText(/Medios registrados|Medios utilizados|Otro medio/)).toHaveCount(0);
        await tab('Sistemas y herramientas'); await expect(page.locator('[data-edit-block^="sistemas:"]')).toHaveCount(0);
        assert.ok(!('medios' in await stored()));
    });
    a = await add('inicial');
    await check('catálogo agrupado, encabezados no seleccionables y lista exacta', async () => {
        await field(a, 'sistema').click(); await expect(page.getByRole('listbox')).toBeVisible();
        assert.deepEqual(await page.getByRole('option').allTextContents(), ['Sistema Integral de Información', 'Aplicaciones web', 'Aplicaciones móviles', 'Microsoft 365', 'LimeSurvey', 'Power BI', 'Tableau', 'Adobe Creative Cloud', 'Otra (escríbela)']);
        await expect(page.getByRole('listbox').getByText('Sistemas institucionales', { exact: true })).toBeVisible();
        await expect(page.getByRole('option', { name: 'Sistemas institucionales', exact: true })).toHaveCount(0);
        await field(a, 'sistema').fill('Microsoft'); await expect(page.getByRole('option')).toHaveCount(1);
        await page.keyboard.press('ArrowDown'); await page.keyboard.press('Enter');
        await expect(field(a, 'aplicacionesMicrosoft365')).toBeVisible();
    });
    await check('Microsoft: cero, nombre único, cantidad, búsqueda, casillas y menú abierto', async () => {
        await expect(field(a, 'aplicacionesMicrosoft365')).toHaveAttribute('placeholder', 'Selecciona aplicaciones');
        await choose(a, 'aplicacionesMicrosoft365', 'Excel'); await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
        await expect(row(a).locator('.record-selected-value').filter({ hasText: /^Excel$/ })).toBeVisible();
        await field(a, 'aplicacionesMicrosoft365').click(); await page.getByRole('option', { name: 'Forms — formularios', exact: true }).click();
        await page.getByRole('option', { name: 'Outlook — correo institucional', exact: true }).click();
        await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
        await expect(row(a).getByText('3 aplicaciones seleccionadas', { exact: true })).toBeVisible();
        await complete(a); await page.reload(); await tab('Sistemas y herramientas');
        assert.deepEqual((await stored()).sistemas[0].aplicacionesMicrosoft365, ['excel', 'forms', 'outlook']);
    });
    await check('Adobe exacto y alfabético; cambiar familia conserva ambas colecciones', async () => {
        await choose(a, 'sistema', 'Adobe Creative Cloud'); await field(a, 'aplicacionesAdobe').click();
        const names = await page.getByRole('option').allTextContents(); assert.equal(names.length, 20);
        assert.deepEqual(names, ['Acrobat Pro', 'Acrobat Reader', 'Adobe Express', 'Adobe Scan', 'After Effects', 'Animate', 'Audition', 'Bridge', 'Character Animator', 'Dreamweaver', 'Fresco', 'Illustrator', 'InCopy', 'InDesign', 'Lightroom', 'Lightroom Classic', 'Media Encoder', 'Photoshop', 'Photoshop Express', 'Premiere']);
        await page.getByRole('option', { name: 'Acrobat Reader', exact: true }).click();
        await field(a, 'aplicacionesAdobe').fill('Photoshop'); await page.getByRole('option', { name: 'Photoshop', exact: true }).click();
        await page.keyboard.press('Escape'); await finish();
        for (const name of ['LimeSurvey', 'Power BI', 'Tableau']) { await choose(a, 'sistema', name); await expect(row(a).locator('.system-dependent-field')).toHaveCount(0); }
        await choose(a, 'sistema', 'Microsoft 365'); await finish(); await page.reload(); await tab('Sistemas y herramientas');
        const saved = (await stored()).sistemas[0]; assert.deepEqual(saved.aplicacionesAdobe, ['acrobat_reader', 'photoshop']); assert.equal(saved.aplicacionesMicrosoft365.length, 3);
    });
    await check('duplicado deshabilitado, primera apertura en otra fila y reasignación protegida', async () => {
        b = await add('inicial'); await field(b, 'sistema').click(); await expect(page.getByRole('listbox')).toBeVisible();
        await expect(page.getByRole('option', { name: /Microsoft 365/ })).toHaveAttribute('aria-disabled', 'true');
        await expect(page.getByRole('option', { name: /Microsoft 365/ })).toContainText('Ya agregado a este procedimiento');
        await page.getByRole('option', { name: 'Adobe Creative Cloud', exact: true }).click(); await choose(b, 'aplicacionesAdobe', 'Premiere'); await page.keyboard.press('Escape'); await complete(b);
        c = await add('segundo'); await choose(c, 'sistema', 'Microsoft 365'); await choose(c, 'aplicacionesMicrosoft365', 'Teams'); await page.keyboard.press('Escape'); await complete(c);
        await field(c, 'proceso').click(); await expect(page.getByRole('option', { name: /PO-01/ })).toHaveAttribute('aria-disabled', 'true'); await page.keyboard.press('Escape');
        assert.equal((await stored()).sistemas.find(r => r.id === c).proceso, 'PO-02');
    });
    await check('módulos y aplicaciones institucionales conservan catálogos locales y selecciones diferenciadas', async () => {
        await choose(b, 'sistema', 'Sistema Integral de Información'); await field(b, 'modulosSii').click();
        await page.getByRole('option').first().click(); await page.getByRole('option').nth(1).click(); await page.keyboard.press('Escape');
        await expect(row(b).getByText('2 módulos seleccionados', { exact: true })).toBeVisible(); await finish();
        for (const [name, prop] of [['Aplicaciones web', 'aplicacionesWeb'], ['Aplicaciones móviles', 'aplicacionesMoviles']]) {
            await choose(b, 'sistema', name); await field(b, prop).click(); await expect(page.getByRole('option').first()).toBeVisible();
            await page.getByRole('option').first().click(); await page.getByRole('option').nth(1).click(); await page.keyboard.press('Escape'); await finish();
        }
        const saved = (await stored()).sistemas.find(r => r.id === b); assert.equal(saved.modulosSii.length, 2); assert.equal(saved.aplicacionesWeb.length, 2); assert.equal(saved.aplicacionesMoviles.length, 2); assert.deepEqual(saved.aplicacionesAdobe, ['premiere']);
    });
    await check('autoguardado conserva nodo, foco, selección y scroll', async () => {
        const input = field(a, 'fallas'); await input.click(); await expect(input).not.toHaveAttribute('readonly');
        await input.fill('Texto completo\nSegunda línea\nTercera línea\nCuarta línea'); await input.evaluate(e => e.setSelectionRange(2, 8));
        const region = page.getByRole('region', { name: 'Tabla de Sistemas y herramientas', exact: true });
        await region.evaluate(e => { e.scrollLeft = e.scrollWidth; }); const position = await region.evaluate(e => [e.scrollLeft, e.scrollTop]); const handle = await input.elementHandle();
        await expect.poll(async () => (await stored()).sistemas.find(r => r.id === a).fallas).toContain('Cuarta línea');
        assert.ok(await input.evaluate((e, old) => e === old, handle)); await expect(input).toBeFocused();
        assert.deepEqual(await input.evaluate(e => [e.selectionStart, e.selectionEnd]), [2, 8]); assert.deepEqual(await region.evaluate(e => [e.scrollLeft, e.scrollTop]), position);
        await finish();
    });
    await check('escritorio y móvil legibles, sin chips ni desbordamiento de página', async () => {
        for (const width of [1366, 390, 320]) {
            await page.setViewportSize({ width, height: 900 }); await field(a, 'sistema').scrollIntoViewIfNeeded();
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            await expect(row(a).locator('.MuiChip-root')).toHaveCount(0);
            await page.screenshot({ path: path.join(artifacts, `tool-catalog-${width}.png`), fullPage: true });
        }
    });
    assert.deepEqual(report.errors, []);
} catch (error) { report.failure = String(error.stack || error); await page.screenshot({ path: path.join(artifacts, 'tool-catalog-failure.png'), fullPage: true }); throw error; }
finally { await writeFile(path.join(artifacts, 'tool-catalog-browser.json'), JSON.stringify(report, null, 2)); await browser.close(); }
