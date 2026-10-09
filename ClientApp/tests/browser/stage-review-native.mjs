import assert from 'node:assert/strict';
import { chromium, expect as baseExpect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw Error('Only isolated synthetic host');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const expect = baseExpect.configure({ timeout: 20000 });
const report = { realPostgreSql: true, realAspNet: true, realSignalR: true, identities: ['persona@uacj.mx', 'colaboradora@uacj.mx'], checks: [], errors: [] };
let first, second;
async function control(action) { assert.equal((await first.context.request.post(origin + '/__fixture/' + action, { headers: { 'X-Fixture-Key': key } })).status(), 200); }
async function session(helper = false) {
    if (helper) await control('delivery-identity?who=helper');
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
    const page = await context.newPage(); page.setDefaultTimeout(20000); page.on('pageerror', error => report.errors.push(error.message));
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio'); return { page, context };
}
const check = async (name, work) => { await work(); report.checks.push(name); console.log('STAGE PASS ' + name); };
async function tab(page, name = 'Revisar y enviar') { await page.getByRole('tab', { name, exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); }
const send = page => page.getByRole('button', { name: 'Enviar primera etapa', exact: true });
const row = (page, id) => page.locator(`[data-edit-block="identificacion:${id}"]`);
async function finish(page) { await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/); }
async function request(page, url, method = 'GET', body) {
    return page.evaluate(async ({ url, method, body }) => {
        const csrf = await (await fetch('/session/csrf')).json();
        const response = await fetch(url, { method, headers: { 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own', 'Content-Type': 'application/json' }, body: body && JSON.stringify(body) });
        return { status: response.status, body: await response.json() };
    }, { url, method, body });
}
const live = async page => (await request(page, '/formatos/A/estado?tab=' + crypto.randomUUID())).body;
try {
    first = await session(); const a = first.page;
    await check('formato nuevo inicia en Contexto; revisión accesible con pendientes y sin avance adicional', async () => {
        await a.goto(origin + '/formatos/A'); await expect(a.getByRole('tab', { name: 'Contexto', exact: true })).toHaveAttribute('aria-selected', 'true');
        await expect(a.getByRole('tab')).toHaveText(['Contexto', 'Identificación general', 'Sistemas y herramientas', 'Revisar y enviar']);
        const before = await live(a); await tab(a); await expect(a.getByRole('heading', { name: 'Revisar y enviar', exact: true })).toBeVisible();
        await expect(send(a)).toBeDisabled(); assert.equal((await live(a)).porcentajeEtapa, before.porcentajeEtapa);
        await a.goto(origin + '/inicio'); await control('delivery-seed'); second = await session(true);
        for (const p of [a, second.page]) await p.goto(origin + '/formatos/A');
    });
    const b = second.page;
    await check('pendiente identifica registro y campo; enlace enfoca la corrección y restaura última ubicación', async () => {
        await tab(a);
        await expect(a.getByText('Primera etapa', { exact: true })).toBeVisible(); await expect(a.getByText(/Área sintética A · Ejercicio 2026/)).toBeVisible();
        await a.getByRole('button', { name: /Registro 2.*Procedimiento pendiente: completa ¿Qué entrega/ }).click();
        const field = a.getByLabel('¿Qué entrega? 2', { exact: true }); await expect(field).toBeFocused();
        await expect(row(a, 'segundo')).toHaveAttribute('data-edit-state', 'owned'); await finish(a);
        await tab(a); await expect.poll(async () => (await live(a)).seccion).toBe('revision'); await a.reload();
        await expect(a.getByRole('tab', { name: 'Revisar y enviar', exact: true })).toHaveAttribute('aria-selected', 'true');
    });
    await check('colaborador consulta sin permiso de envío; otra sesión actualiza resumen y reservas en vivo', async () => {
        await tab(b); await expect(send(b)).toHaveCount(0);
        assert.equal((await request(b, '/formatos/A/enviar', 'POST', { tabId: crypto.randomUUID(), operationId: crypto.randomUUID(), version: (await live(b)).version, stage: 1 })).status, 403);
        await tab(b, 'Identificación general'); const field = b.getByLabel('¿Qué entrega? 2', { exact: true });
        await field.click(); await expect(row(b, 'segundo')).toHaveAttribute('data-edit-state', 'owned'); await field.fill('Entrega completa');
        await expect(a.getByText('Otra sesión está editando. Espera a que guarde y termine antes de enviar.')).toBeVisible(); await expect(send(a)).toBeDisabled();
        await finish(b); await expect(send(a)).toBeEnabled();
        assert.equal((await live(a)).entrega.revision.listo, true); assert.equal((await live(a)).porcentajeEtapa, 100);
    });
    await check('autoguardado sin confirmar deshabilita envío y conserva los pendientes reales del servidor', async () => {
        await tab(a, 'Identificación general'); const field = a.getByLabel('¿Qué entrega? 1', { exact: true });
        await field.click(); await expect(row(a, 'inicial')).toHaveAttribute('data-edit-state', 'owned');
        let release; const gate = new Promise(resolve => release = resolve); let arrived = false;
        await a.route('**/formatos/A/bloques', async route => { arrived = true; await gate; await route.continue(); });
        try {
            await field.fill(''); await expect.poll(() => arrived).toBe(true); await tab(a);
            await expect(a.getByText('Autoguardado: Guardando…', { exact: true })).toBeVisible(); await expect(send(a)).toBeDisabled(); release();
            await expect(a.getByRole('button', { name: /Registro 1.*completa ¿Qué entrega/ })).toBeVisible();
        } finally { release(); await a.unroute('**/formatos/A/bloques'); }
        await a.getByRole('button', { name: /Registro 1.*completa ¿Qué entrega/ }).click(); await expect(field).toBeFocused();
        await expect(row(a, 'inicial')).toHaveAttribute('data-edit-state', 'owned'); await field.fill('Entrega restablecida'); await finish(a); await tab(a); await expect(send(a)).toBeEnabled();
    });
    await check('cambio remoto durante confirmación invalida envío; servidor rechaza la versión revisada', async () => {
        const old = await live(a); await send(a).click(); const dialog = a.getByRole('dialog');
        await expect(dialog.getByText('Al enviar la primera etapa, sus respuestas quedarán disponibles para consulta y ya no podrán editarse.')).toBeVisible();
        const field = b.getByLabel('¿Qué entrega? 2', { exact: true }); await field.click(); await expect(row(b, 'segundo')).toHaveAttribute('data-edit-state', 'owned');
        await field.fill(''); await finish(b);
        await expect(dialog.getByRole('button', { name: 'Confirmar envío', exact: true })).toBeDisabled();
        await expect(dialog.getByText(/Las respuestas o la etapa cambiaron/)).toBeVisible();
        assert.equal((await request(a, '/formatos/A/enviar', 'POST', { tabId: crypto.randomUUID(), operationId: crypto.randomUUID(), version: old.version, stage: 1 })).status, 409);
        await dialog.getByRole('button', { name: 'Cancelar', exact: true }).click(); await expect(send(a)).toBeDisabled();
        await field.click(); await expect(row(b, 'segundo')).toHaveAttribute('data-edit-state', 'owned'); await field.fill('Entrega definitiva'); await finish(b); await expect(send(a)).toBeEnabled();
    });
    await check('revisión compacta en escritorio, móvil y navegación por teclado', async () => {
        await a.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).focus(); await a.keyboard.press('ArrowRight'); await a.keyboard.press('Enter');
        await expect(a.getByRole('tab', { name: 'Revisar y enviar', exact: true })).toHaveAttribute('aria-selected', 'true');
        for (const width of [1440, 390, 320]) {
            await a.setViewportSize({ width, height: 900 }); await a.getByRole('heading', { name: 'Revisar y enviar', exact: true }).scrollIntoViewIfNeeded();
            assert.ok(await a.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            await a.screenshot({ path: path.join(artifacts, `stage-review-${width}.png`), fullPage: true });
        }
    });
    await check('doble clic no duplica; reintento confirma envío y recarga conserva autor, fecha y bloqueo', async () => {
        await send(a).click(); const dialog = a.getByRole('dialog'); let count = 0, body, release;
        const gate = new Promise(resolve => release = resolve);
        await a.route('**/formatos/A/enviar', async route => { count++; body = route.request().postDataJSON(); const response = await route.fetch(); await gate; await route.fulfill({ response }); });
        try {
            const confirm = dialog.getByRole('button', { name: /^(Confirmar envío|Enviando…)$/ }); await confirm.click();
            await expect(confirm).toBeDisabled(); await confirm.dispatchEvent('click'); await expect.poll(() => count).toBe(1); release();
            await expect(dialog).toHaveCount(0); await expect(a.getByText('Primera etapa enviada', { exact: true })).toBeVisible();
        } finally { release(); await a.unroute('**/formatos/A/enviar'); }
        assert.equal((await request(a, '/formatos/A/enviar', 'POST', body)).status, 200);
        const state = await live(a); assert.equal(state.entrega.etapa.enviadoNombre, 'Persona sintética'); assert.ok(state.entrega.etapa.enviadoEn);
        await a.reload(); await expect(a.getByText('Primera etapa enviada', { exact: true })).toBeVisible(); await expect(send(a)).toHaveCount(0);
        await expect(a.getByRole('button', { name: 'Guardar borrador', exact: true })).toBeDisabled();
        await expect(row(b, 'segundo').getByLabel('¿Qué entrega? 2', { exact: true })).toHaveJSProperty('readOnly', true);
        await a.screenshot({ path: path.join(artifacts, 'stage-submitted-mobile.png'), fullPage: true });
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (error) { report.failure = error.message; if (first) await first.page.screenshot({ path: path.join(artifacts, 'stage-review-failure.png'), fullPage: true }).catch(() => {}); throw error; }
finally { await writeFile(path.join(artifacts, 'stage-review-browser.json'), JSON.stringify(report, null, 2)); await browser.close(); }
