import assert from 'node:assert/strict';
import { chromium, expect as baseExpect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw Error('Only isolated synthetic host');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const expect = baseExpect.configure({ timeout: 20000 });
const report = { realPostgreSql: true, realAspNet: true, realSignalR: true, identities: ['persona@uacj.mx', 'colaboradora@uacj.mx'], mockedEditingTransport: false, checks: [], errors: [] };
const check = async (name, work) => { await work(); report.checks.push(name); console.log('DELIVERY PASS ' + name); };
let first, second;
async function control(action) { assert.equal((await first.context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } })).status(), 200); }
async function session(who) {
    if (first) await control('delivery-identity?who=' + who);
    const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', hasTouch: true });
    await context.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.origin !== origin && !(url.protocol === 'wss:' && url.host === new URL(origin).host)) return route.abort();
        if (url.pathname === '/connect') {
            const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
            if (headers.location?.startsWith('https://login.microsoftonline.com/')) headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
            return route.fulfill({ response, headers });
        }
        return route.continue();
    });
    const page = await context.newPage(); page.setDefaultTimeout(20000); page.on('pageerror', e => report.errors.push(e.message));
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio'); return { context, page };
}
async function tab(page, name) { await page.getByRole('tab', { name, exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); }
const row = (page, id) => page.locator(`[data-edit-block="${id}"]`);
const input = (page, label) => page.getByLabel(label, { exact: true });
async function finish(page) { await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); }
async function stored() { return (await (await first.context.request.get(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } })).json()).contenido; }
async function request(page, url, method = 'GET', body) {
    return page.evaluate(async ({ url, method, body }) => {
        const csrf = await (await fetch('/session/csrf')).json();
        const res = await fetch(url, { method, headers: { 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own', 'Content-Type': 'application/json' }, body: body && JSON.stringify(body) });
        return { status: res.status, body: await res.json() };
    }, { url, method, body });
}
const live = async page => (await request(page, '/formatos/A/estado?tab=' + crypto.randomUUID())).body;
const mutation = (block, version, value, leaseId) => ({ tabId: crypto.randomUUID(), operationId: crypto.randomUUID(), section: 'sistemas', release: false, blocks: [{ key: block, version, value, leaseId }] });
try {
    first = await session('owner'); await control('delivery-seed'); second = await session('helper');
    const a = first.page, b = second.page;
    for (const p of [a, b]) { await p.goto(origin + '/formatos/A'); await tab(p, 'Identificación general'); }
    const later = (c) => Object.fromEntries(['datos', 'evaluaciones', 'preguntas', 'acuerdos'].map(s => [s, c[s]]));
    const history = later(await stored());
    await check('dos identidades autorizadas: carrera real de reserva acepta exactamente una', async () => {
        assert.notEqual((await first.context.cookies()).find(c => c.name === 'tdv2.aspnet.session')?.value, (await second.context.cookies()).find(c => c.name === 'tdv2.aspnet.session')?.value);
        const statuses = []; let start, arrived = 0; const gate = new Promise(r => start = r);
        for (const p of [a, b]) await p.route('**/formatos/A/reservas', async route => { arrived++; await gate; const response = await route.fetch(); statuses.push(response.status()); await route.fulfill({ response }); });
        try {
            await Promise.all([a, b].map(p => input(p, 'Trámite o servicio 1').focus())); await expect.poll(() => arrived).toBe(2); start();
            await expect.poll(() => statuses.length).toBe(2); assert.deepEqual([...statuses].sort(), [200, 409]);
            const winner = await input(a, 'Trámite o servicio 1').evaluate(e => !e.readOnly) ? a : b, loser = winner === a ? b : a;
            await expect(row(loser, 'identificacion:inicial')).toHaveAttribute('data-edit-state', 'occupied');
            const presence = row(loser, 'identificacion:inicial').locator('td').first();
            await expect(presence.getByText('Ocupado', { exact: true })).toBeVisible();
            await expect(presence.getByRole('img', { name: 'Participante: ' + (winner === a ? 'Persona sintética' : 'Colaboradora sintética'), exact: true })).toBeVisible();
            await expect(row(loser, 'identificacion:inicial').getByRole('button', { name: 'Eliminar registro' })).toBeDisabled();
            await input(winner, 'Trámite o servicio 1').fill('Procedimiento compartido');
            await winner.getByRole('tab', { name: 'Identificación general', exact: true }).focus();
            await expect(row(loser, 'identificacion:inicial')).toHaveAttribute('data-edit-state', 'idle'); await expect(input(loser, 'Trámite o servicio 1')).toHaveValue('Procedimiento compartido');
        } finally { start?.(); for (const p of [a, b]) await p.unroute('**/formatos/A/reservas'); }
    });
    await check('filas distintas, fotografías, presencia fija y un único foco MUI', async () => {
        const before = await row(a, 'identificacion:inicial').locator('td').first().boundingBox();
        await input(a, 'Trámite o servicio 1').click(); await input(b, 'Trámite o servicio 2').click();
        await expect(row(a, 'identificacion:inicial')).toHaveAttribute('data-edit-state', 'owned'); await expect(row(b, 'identificacion:segundo')).toHaveAttribute('data-edit-state', 'owned');
        await expect(row(a, 'identificacion:segundo').getByRole('img', { name: 'Participante: Colaboradora sintética', exact: true })).toBeVisible();
        const after = await row(a, 'identificacion:inicial').locator('td').first().boundingBox(); assert.equal(before.width, after.width); assert.equal(before.height, after.height);
        const focus = await input(a, 'Trámite o servicio 1').evaluate(el => ({ outline: getComputedStyle(el).outlineStyle, border: getComputedStyle(el.closest('.MuiOutlinedInput-root').querySelector('fieldset')).borderWidth, size: getComputedStyle(el).fontSize, row: getComputedStyle(el.closest('tr')).outlineStyle }));
        assert.equal(focus.outline, 'none'); assert.equal(focus.border, '2px'); assert.equal(focus.row, 'solid'); assert.notEqual(focus.size, '16px');
        await a.screenshot({ path: path.join(artifacts, 'delivery-desktop-focus.png'), fullPage: true });
        await finish(a); await finish(b);
    });
    await check('primera tecla, pegado y Tab se conservan al confirmar la reserva', async () => {
        const f = input(a, 'Trámite o servicio 1'); await f.focus(); await a.keyboard.press('End'); await a.keyboard.type(' teclado'); await a.keyboard.press('Tab');
        await expect.poll(async () => (await stored()).identificacion[0].tramite).toContain('teclado'); await finish(a);
        await first.context.grantPermissions(['clipboard-read', 'clipboard-write']);
        await a.evaluate(() => navigator.clipboard.writeText(' pegado sintético'));
        const text = input(a, '¿Qué entrega? 1'); await text.focus(); await a.keyboard.press('Control+V');
        await expect(text).toHaveValue(/pegado sintético/); await finish(a);
        assert.match((await stored()).identificacion[0].resultado, /pegado sintético/);
    });
    await check('incompletos excluidos; completar e invalidar actualiza otra identidad por SignalR', async () => {
        await tab(b, 'Sistemas y herramientas'); const select = b.getByRole('combobox', { name: 'Procedimiento 1', exact: true });
        await select.click(); await expect(b.getByRole('option', { name: /PO-02/ })).toHaveCount(0); await b.keyboard.press('Escape'); await finish(b);
        await input(a, '¿Qué entrega? 2').click(); await expect(row(a, 'identificacion:segundo')).toHaveAttribute('data-edit-state', 'owned'); await input(a, '¿Qué entrega? 2').fill('Resultado completo'); await finish(a);
        await select.click(); await expect(b.getByRole('option', { name: /PO-02/ })).toBeVisible(); await b.getByRole('option', { name: /PO-02/ }).click(); await finish(b);
        await input(a, '¿Qué entrega? 2').click(); await expect(row(a, 'identificacion:segundo')).toHaveAttribute('data-edit-state', 'owned'); await input(a, '¿Qué entrega? 2').fill(''); await finish(a);
        await expect(row(b, 'sistemas:inicial').getByText('El procedimiento relacionado está pendiente de completar o validar.')).toBeVisible();
        assert.equal((await stored()).sistemas[0].proceso, 'PO-02'); assert.equal((await live(b)).revisionEnvio.listo, false);
        const invalid = (await stored()).sistemas[1]; invalid.proceso = 'PO-02';
        const state = await live(b), block = state.bloques.find(r => r.key === 'sistemas:segunda');
        const req = mutation('sistemas:segunda', block?.version || 0, invalid);
        const reserved = await request(b, '/formatos/A/reservas', 'POST', req); assert.equal(reserved.status, 200);
        req.blocks[0].leaseId = reserved.body.bloques[0].reserva.id;
        assert.equal((await request(b, '/formatos/A/bloques', 'PATCH', req)).status, 422);
        await request(b, '/formatos/A/reservas/liberar', 'POST', req);
        await input(a, '¿Qué entrega? 2').click(); await expect(row(a, 'identificacion:segundo')).toHaveAttribute('data-edit-state', 'owned'); await input(a, '¿Qué entrega? 2').fill('Restaurado'); await finish(a);
        await expect(row(b, 'sistemas:inicial').getByText('El procedimiento relacionado está pendiente de completar o validar.')).toHaveCount(0);
    });
    await check('eliminación directa libre y actualización de opciones en la otra sesión', async () => {
        await row(a, 'identificacion:tercero').getByRole('button', { name: 'Eliminar registro' }).click();
        await a.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click(); await expect(row(a, 'identificacion:tercero')).toHaveCount(0);
        await b.getByRole('combobox', { name: 'Procedimiento 1', exact: true }).click(); await expect(b.getByRole('option', { name: /PO-03/ })).toHaveCount(0); await b.keyboard.press('Escape'); await finish(b);
    });
    await check('Medios es independiente, menús y ayudas mantienen reserva de herramientas', async () => {
        await tab(a, 'Sistemas y herramientas'); await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'idle');
        await input(a, 'Fallas o comentarios 1').click(); await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'owned');
        const media = row(b, 'medios'); await media.getByRole('checkbox').first().click(); await expect(media).toHaveAttribute('data-edit-state', 'owned');
        await expect(row(b, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'occupied'); await expect(media.locator('xpath=ancestor::table')).toHaveCount(0);
        await input(b, 'Otro medio').fill('Medio independiente'); await finish(b);
        assert.equal((await stored()).medioOtro, 'Medio independiente');
        await a.getByRole('combobox', { name: '¿Cómo funciona? 1', exact: true }).click(); await a.getByRole('option', { name: 'Funciona con fallas', exact: true }).click();
        await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'owned');
        const help = a.getByRole('button', { name: 'Ayuda: ¿Cómo funciona?', exact: true }); await help.click(); await a.keyboard.press('Escape'); await expect(help).toBeFocused();
        await help.click(); await a.getByRole('button', { name: 'Cerrar ayuda', exact: true }).click(); await expect(help).toBeFocused();
        await help.click(); await a.getByRole('dialog').getByRole('button', { name: 'Cerrar', exact: true }).click(); await expect(help).toBeFocused();
        await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'owned'); await finish(a);
    });
    await check('otra pestaña se distingue de otra persona y no adquiere la fila ocupada', async () => {
        const otherTab = await first.context.newPage(); await otherTab.goto(origin + '/formatos/A'); await tab(otherTab, 'Sistemas y herramientas');
        await input(a, 'Fallas o comentarios 1').click(); await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'owned');
        await expect(row(otherTab, 'sistemas:inicial').getByText('Otra pestaña', { exact: true })).toBeVisible();
        await expect(row(b, 'sistemas:inicial').getByText('Ocupado', { exact: true })).toBeVisible(); await finish(a); await otherTab.close();
    });
    await check('backend rechaza eliminación ajena y renovación tardía tras retiro', async () => {
        await input(a, 'Fallas o comentarios 2').click(); await expect(row(a, 'sistemas:segunda')).toHaveAttribute('data-edit-state', 'owned');
        const state = await live(b), v = state.bloques.find(r => r.key === 'sistemas:segunda')?.version || 0;
        const req = mutation('sistemas:segunda', v, null); req.removal = { section: 'sistemas', id: 'segunda' };
        assert.equal((await request(b, '/formatos/A/reservas', 'POST', req)).status, 409);
        assert.equal((await request(b, '/formatos/A/bloques', 'PATCH', req)).status, 409); await finish(a);
        const reserved = await request(b, '/formatos/A/reservas', 'POST', req); assert.equal(reserved.status, 200); req.blocks[0].leaseId = reserved.body.bloques[0].reserva.id;
        const [save, renew] = await Promise.all([request(b, '/formatos/A/bloques', 'PATCH', { ...req, release: true }), request(b, '/formatos/A/reservas/actividad', 'POST', req)]);
        assert.equal(save.status, 200); assert.ok([200, 409].includes(renew.status));
        const late = await request(b, '/formatos/A/reservas/actividad', 'POST', req); assert.equal(late.status, 409); assert.equal(late.body.code, 'registro_eliminado');
        await expect(row(a, 'sistemas:segunda')).toHaveCount(0);
    });
    await check('desconexión conserva propuesta; reconexión y vencimiento exigen reserva vigente', async () => {
        const comments = input(a, 'Fallas o comentarios 1'); await comments.click(); await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'owned');
        await comments.fill('Pendiente de red'); await first.context.setOffline(true);
        await expect.poll(async () => await comments.inputValue()).toBe('Pendiente de red');
        await first.context.setOffline(false); await finish(a); assert.equal((await stored()).sistemas[0].fallas, 'Pendiente de red');
        const state = await live(a), v = state.bloques.find(r => r.key === 'sistemas:inicial').version;
        const req = mutation('sistemas:inicial', v, (await stored()).sistemas[0]);
        const reserved = await request(a, '/formatos/A/reservas', 'POST', req); assert.equal(reserved.status, 200); req.blocks[0].leaseId = reserved.body.bloques[0].reserva.id;
        await control('expire-editing'); req.blocks[0].value.fallas = 'No autorizado';
        assert.equal((await request(a, '/formatos/A/bloques', 'PATCH', req)).status, 409); assert.equal((await stored()).sistemas[0].fallas, 'Pendiente de red');
        await comments.click(); await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'owned'); await comments.fill('Después de renovar'); await finish(a);
    });
    await check('móvil, teclado, tacto y posición compartida de presencia', async () => {
        await a.setViewportSize({ width: 360, height: 800 }); await b.setViewportSize({ width: 360, height: 800 });
        await input(a, 'Fallas o comentarios 1').focus(); await expect(row(a, 'sistemas:inicial')).toHaveAttribute('data-edit-state', 'owned');
        await expect(row(a, 'sistemas:inicial').locator('td').first().getByText('Estás editando')).toBeVisible();
        const avatar = row(b, 'sistemas:inicial').getByRole('img', { name: 'Participante: Persona sintética', exact: true });
        await avatar.focus(); await expect(b.getByRole('tooltip')).toHaveText('Persona sintética');
        await avatar.tap(); await expect(b.getByRole('tooltip')).toHaveText('Persona sintética');
        assert.ok(await a.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        await row(a, 'sistemas:inicial').locator('.row-editing-cell').scrollIntoViewIfNeeded();
        await a.evaluate(() => { const scroll = document.querySelector('.systems-table').closest('.capture-scroll'); scroll.scrollLeft = scroll.scrollWidth; window.scrollTo({ top: 0, behavior: 'instant' }); });
        await a.screenshot({ path: path.join(artifacts, 'delivery-mobile-presence.png'), fullPage: true }); await finish(a);
    });
    await check('primera etapa se entrega sin posteriores, conserva sus datos y bloquea enviados', async () => {
        assert.deepEqual(later(await stored()), history);
        assert.equal((await request(b, '/formatos/A/enviar', 'POST', { tabId: crypto.randomUUID(), operationId: crypto.randomUUID(), version: (await live(b)).version })).status, 403);
        await tab(a, 'Revisar y enviar');
        await expect(a.getByRole('heading', { name: 'Revisar y enviar', exact: true })).toBeVisible();
        await expect(a.getByRole('button', { name: 'Enviar primera etapa', exact: true })).toBeVisible(); await a.getByRole('button', { name: 'Enviar primera etapa', exact: true }).click();
        await a.getByRole('dialog').getByRole('button', { name: 'Confirmar envío', exact: true }).click();
        await expect(input(b, 'Fallas o comentarios 1')).toHaveJSProperty('readOnly', true);
        assert.deepEqual(later(await stored()), history);
        const state = await live(a), req = mutation('sistemas:inicial', state.bloques.find(r => r.key === 'sistemas:inicial').version, state.contenido.sistemas[0], crypto.randomUUID());
        assert.equal((await request(a, '/formatos/A/reservas', 'POST', req)).status, 409); assert.equal((await request(a, '/formatos/A/bloques', 'PATCH', req)).status, 409);
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (e) { report.failure = e.message; if (first) await first.page.screenshot({ path: path.join(artifacts, 'delivery-failure.png'), fullPage: true }).catch(() => {}); throw e; }
finally { await writeFile(path.join(artifacts, 'delivery-browser.json'), JSON.stringify(report, null, 2)); await browser.close(); }
