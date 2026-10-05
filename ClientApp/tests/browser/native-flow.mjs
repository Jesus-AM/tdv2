import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';

const origin = process.env.TDV2_BROWSER_ORIGIN;
const fixtureKey = process.env.TDV2_BROWSER_KEY;
const artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !fixtureKey || !artifacts) throw new Error('Requires the isolated native test host.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
// Microsoft's browser and HTTPS peer are simulated. React, ASP.NET, cookies, CSRF and PostgreSQL are real.
// Intercept the local redirect itself: Playwright route handlers do not intercept subsequent redirects.
// No external request is allowed, including fonts. Never send even synthetic OAuth data to a real service.
let microsoftLogoutTarget;
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin && !(url.protocol === 'wss:' && url.host === new URL(origin).host)) return route.abort();
    if (url.pathname === '/connect' || url.pathname === '/session/microsoft-logout') {
        const response = await route.fetch({ maxRedirects: 0 });
        const headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            const target = new URL(headers.location);
            if (target.pathname.endsWith('/authorize')) {
                const state = target.searchParams.get('state');
                headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(state)}`;
                return route.fulfill({ response, headers });
            }
            assert.ok(target.pathname.endsWith('/logout'));
            microsoftLogoutTarget = target;
            delete headers.location;
            return route.fulfill({ status: 200, headers, body: 'Synthetic Microsoft sign-out' });
        }
        return route.fulfill({ response });
    }
    return route.continue();
});
const page = await context.newPage();
const errors = [];
page.on('pageerror', error => errors.push(error.message));
const checks = [];
async function check(name, work) { await work(); checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name); }
async function control(action) {
    const response = await context.request.post(`${origin}/__fixture/${action}`, { headers: { 'X-Fixture-Key': fixtureKey } });
    assert.equal(response.status(), 200); return response;
}
async function stored() { return (await control('stored')).json(); }
async function header(target = page) {
    await target.getByRole('tab', { name: 'Contexto', exact: true }).click();
    return target.getByLabel('Responsable del llenado', { exact: true });
}
async function savedAfter(work, status = 200, target = page) {
    const [response] = await Promise.all([target.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/bloques' && r.request().method() === 'PATCH'), work()]);
    assert.equal(response.status(), status);
}
async function finish(target = page) {
    await Promise.all([target.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/reservas/liberar'),
        target.getByRole('button', { name: 'Guardar borrador', exact: true }).click()]);
}
try {
    await check('portada y login Microsoft simulado crean sesión segura', async () => {
        assert.equal((await page.goto(origin)).status(), 200);
        await page.getByRole('link', { name: /Registrar un proceso operativo/ }).click();
        await page.waitForURL(`${origin}/inicio`);
        await page.getByRole('heading', { name: 'Procesos operativos', exact: true }).waitFor();
        const cookies = await context.cookies();
        const session = cookies.find(cookie => cookie.name === 'tdv2.aspnet.session');
        assert.ok(session?.secure && session.httpOnly && session.sameSite === 'Lax');
        assert.ok(!session.value.includes('synthetic-access'));
    });
    await check('captura React persiste respuestas y avance en PostgreSQL', async () => {
        await page.goto(`${origin}/formatos/A`);
        const input = await header();
        await savedAfter(() => input.fill('Respuesta desde React áéíóú'));
        const row = await stored();
        assert.equal(row.contenido.encabezado.responsable, 'Respuesta desde React áéíóú');
        assert.equal(row.porcentaje, 5);
        await finish();
        await expect(page.getByRole('tab', { name: 'Encabezado', exact: true })).toHaveCount(0);
        await page.reload();
        assert.equal(await (await header()).inputValue(), 'Respuesta desde React áéíóú');
    });
    await check('SignalR rechaza suscripción a UR ajena sin entregar contenido', async () => {
        const result = await page.evaluate(async () => {
            const csrf = await (await fetch('/session/csrf')).json();
            const response = await fetch('/form-events/negotiate?negotiateVersion=1', { method: 'POST',
                headers: { 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own' } });
            if (!response.ok) throw new Error('SignalR negotiation HTTP ' + response.status);
            const negotiation = await response.json();
            if (!negotiation.connectionToken) throw new Error('Missing SignalR connection token');
            return await new Promise((resolve, reject) => {
                const socket = new WebSocket(location.origin.replace('https:', 'wss:') + '/form-events?id=' + encodeURIComponent(negotiation.connectionToken));
                const timer = setTimeout(() => { socket.close(); reject(new Error('SignalR timeout')); }, 15000);
                let started = false, items = 0;
                socket.onopen = () => socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + '\u001e');
                socket.onmessage = event => {
                    for (const part of event.data.split('\u001e').filter(Boolean)) {
                        const message = JSON.parse(part);
                        if (!started) {
                            started = true;
                            socket.send(JSON.stringify({ type: 4, invocationId: '1', target: 'Watch', arguments: ['B', 'own', crypto.randomUUID()] }) + '\u001e');
                        } else if (message.type === 2) items++;
                        else if (message.type === 3) { clearTimeout(timer); socket.close(); resolve({ items, error: !!message.error }); }
                    }
                };
                socket.onerror = () => { clearTimeout(timer); reject(new Error('SignalR connection failed')); };
            });
        });
        assert.deepEqual(result, { items: 0, error: true });
    });
    await check('dos pestañas: SignalR muestra titular, bloquea la misma reserva y permite otro bloque', async () => {
        const other = await context.newPage();
        await other.goto(`${origin}/formatos/A`); const otherInput = await header(other);
        await savedAfter(() => page.getByLabel('Responsable del llenado', { exact: true }).fill('Ganadora React'));
        await expect(otherInput).toBeDisabled();
        await expect(other.getByText('Editando: Persona sintética', { exact: true })).toBeVisible();
        await other.getByRole('tab', { name: 'Preguntas', exact: true }).click();
        const answer = other.locator('#panel-preguntas textarea').first();
        await savedAfter(() => answer.fill('Respuesta simultánea desde otra pestaña'), 200, other);
        assert.equal((await stored()).contenido.encabezado.responsable, 'Ganadora React');
        await finish(other);
        await other.close();
    });
    await check('prioridad accesible sin defecto, Contexto inicial y navegación superior móvil', async () => {
        await finish();
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const priorities = page.getByRole('radiogroup', { name: 'Prioridad 1', exact: true });
        await expect(priorities.locator('input:checked')).toHaveCount(0);
        await savedAfter(() => priorities.getByRole('radio', { name: '5: Extremadamente prioritario', exact: true }).check());
        await priorities.getByRole('radio', { name: '5: Extremadamente prioritario', exact: true }).focus();
        await savedAfter(() => page.keyboard.press('ArrowDown'));
        await expect(priorities.getByRole('radio', { name: '4: Muy prioritario', exact: true })).toBeChecked();
        await finish();
        await page.reload();
        await expect(page.getByRole('tab', { name: 'Identificación general', exact: true })).toHaveAttribute('aria-selected', 'true');
        await page.setViewportSize({ width: 390, height: 844 }); await page.emulateMedia({ reducedMotion: 'reduce' });
        await page.getByRole('button', { name: 'Abrir navegación', exact: true }).click();
        await expect(page.getByRole('navigation', { name: 'Módulos' })).toBeVisible();
        await page.getByRole('button', { name: 'Cerrar navegación', exact: true }).click();
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await priorities.scrollIntoViewIfNeeded();
        await page.screenshot({ path: path.join(artifacts, 'prioridad-movil.png'), fullPage: true });
        await page.setViewportSize({ width: 1440, height: 1000 }); await page.emulateMedia({ reducedMotion: 'no-preference' });
        await header();
    });
    await check('error PostgreSQL conserva borrador; reintento recupera guardado', async () => {
        await control('fail-save');
        const before = await stored();
        await savedAfter(() => page.getByLabel('Responsable del llenado', { exact: true }).fill('Recuperación React'), 503);
        assert.equal((await stored()).version, before.version);
        assert.equal((await stored()).contenido.encabezado.responsable, 'Ganadora React');
        await control('recover-save');
        await savedAfter(() => page.getByRole('button', { name: 'Reintentar guardado' }).click());
        assert.equal((await stored()).contenido.encabezado.responsable, 'Recuperación React');
    });
    await check('UR ajena deniega documento HTML y muestra pantalla conservada', async () => {
        await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
        assert.equal((await page.goto(`${origin}/formatos/B`)).status(), 403);
        await page.getByText('No tienes acceso al formato de esta UR.', { exact: true }).waitFor();
        await page.screenshot({ path: path.join(artifacts, 'ur-denegada.png') });
    });
    await check('módulo no autorizado se aplica en React y servidor', async () => {
        await control('module-off');
        assert.equal((await page.goto(`${origin}/inicio`)).status(), 403);
        await page.getByText('No tienes acceso a este módulo de Transformación Digital.', { exact: true }).waitFor();
        await control('module-on');
    });
    await check('usuario sin aplicación publicada no conserva acceso de su sesión', async () => {
        await control('user-off');
        assert.equal((await page.goto(`${origin}/inicio`)).status(), 403);
        await page.getByText('Tu cuenta no tiene un acceso y un rol vigentes para Transformación Digital en Nexo.', { exact: true }).waitFor();
        await control('user-on');
        await control('app-off');
        assert.equal((await page.goto(`${origin}/inicio`)).status(), 403);
        await control('app-on');
    });
    await check('Nexo no disponible no concede permisos y permite recuperar sesión', async () => {
        await control('outage');
        assert.equal((await page.goto(`${origin}/inicio`)).status(), 503);
        await page.getByText('No fue posible comprobar el acceso con Nexo. Inténtalo más tarde.', { exact: true }).waitFor();
        await control('restore');
        assert.equal((await page.goto(`${origin}/formatos/A`)).status(), 200);
        assert.equal(await (await header()).inputValue(), 'Recuperación React');
        await page.screenshot({ path: path.join(artifacts, 'formato-postgresql.png') });
    });
    await check('Enviar confirma respuestas guardadas y rechaza requisitos incompletos sin bloquear el borrador', async () => {
        await page.getByRole('button', { name: 'Enviar', exact: true }).click();
        await expect(page.getByRole('dialog', { name: 'Enviar formato' })).toBeVisible();
        const [response] = await Promise.all([page.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/enviar'),
            page.getByRole('button', { name: 'Confirmar envío', exact: true }).click()]);
        assert.equal(response.status(), 422);
        await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
        await expect(page.getByText('No se pudo guardar', { exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toBeEnabled();
        await expect(page.getByLabel('Responsable del llenado', { exact: true })).toHaveValue('Recuperación React');
    });
    await check('Enviar guarda la propuesta pendiente, confirma y bloquea captura e impresión estable', async () => {
        await control('ready-to-submit'); await page.reload();
        const input = await header();
        await input.fill('Responsable del envío sintético');
        await page.getByRole('button', { name: 'Enviar', exact: true }).click();
        await expect(page.getByRole('dialog', { name: 'Enviar formato' })).toBeVisible();
        assert.equal((await stored()).contenido.encabezado.responsable, 'Responsable del envío sintético');
        const [response] = await Promise.all([page.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/enviar'),
            page.getByRole('button', { name: 'Confirmar envío', exact: true }).click()]);
        assert.equal(response.status(), 200);
        await expect(page.getByRole('dialog')).toHaveCount(0);
        await expect(input).toBeDisabled();
        await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toBeDisabled();
        await page.reload(); await header();
        await expect(page.getByRole('button', { name: 'Enviar', exact: true })).toHaveCount(0);
        await expect(page.getByRole('button', { name: 'Imprimir', exact: true })).toBeEnabled();
        await page.screenshot({ path: path.join(artifacts, 'formato-enviado.png'), fullPage: true });
        await page.evaluate(() => { window.print = () => {}; });
        await page.getByRole('button', { name: 'Imprimir', exact: true }).click();
        await page.locator('#panel-acuerdos').waitFor({ state: 'visible' });
        await page.emulateMedia({ media: 'print' });
        assert.equal(await page.getByRole('tabpanel').count(), 7);
        await page.pdf({ path: path.join(artifacts, 'formato-enviado.pdf'), format: 'A4', printBackground: true });
        await page.emulateMedia({ media: 'screen' });
        await page.evaluate(() => window.dispatchEvent(new Event('afterprint')));
    });
    await check('logout desde React revoca sesión y redirige a Microsoft', async () => {
        await page.getByRole('button', { name: 'Abrir menú de usuario' }).click();
        const logout = page.waitForResponse(r => new URL(r.url()).pathname === '/logout' && r.request().method() === 'POST');
        await page.getByRole('menuitem', { name: 'Cerrar sesión' }).click();
        const result = await logout;
        assert.equal(result.status(), 200);
        await page.waitForURL(`${origin}/session/microsoft-logout`);
        assert.equal(microsoftLogoutTarget?.searchParams.get('post_logout_redirect_uri'), origin + '/');
        assert.equal((await page.goto(`${origin}/inicio`)).status(), 401);
    });
    assert.deepEqual(errors, []);
    await writeFile(path.join(artifacts, 'browser.json'), JSON.stringify({ browser: browser.version(), checks, pageErrors: errors, externalFontsBlocked: true }, null, 2));
} catch (error) {
    console.error('BROWSER FAIL', error.message);
    await page.screenshot({ path: path.join(artifacts, 'browser-failure.png') }).catch(() => {});
    await writeFile(path.join(artifacts, 'browser.json'), JSON.stringify({ checks, failed: true, error: error.message, pageErrors: errors }, null, 2));
    process.exitCode = 1;
} finally { await browser.close(); }
