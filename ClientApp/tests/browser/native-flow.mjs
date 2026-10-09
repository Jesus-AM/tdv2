import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
import { watchStream, streamState, cancelStream } from './watch-stream.mjs';
import { verifyDirectDeletion } from './deletion-flow.mjs';
import { verifyCentralCollaboration } from './central-collaboration-flow.mjs';
import { verifyParticipants } from './participant-flow.mjs';

const origin = process.env.TDV2_BROWSER_ORIGIN;
const fixtureKey = process.env.TDV2_BROWSER_KEY;
const artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !fixtureKey || !artifacts) throw new Error('Requires the isolated native test host.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
context.setDefaultTimeout(15000);
// Microsoft's browser and HTTPS peer are simulated. React, ASP.NET, cookies, CSRF and PostgreSQL are real.
// Intercept the local redirect itself: Playwright route handlers do not intercept subsequent redirects.
// No external request is allowed, including fonts. Never send even synthetic OAuth data to a real service.
let microsoftLogoutTarget;
async function isolateMicrosoft(targetContext) { await targetContext.route('**/*', async route => {
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
}); }
await isolateMicrosoft(context);
const page = await context.newPage();
const errors = [];
const editingRequests = [];
page.on('response', async response => {
    const url = new URL(response.url());
    if (url.pathname.startsWith('/formatos/A/') && ['POST', 'PATCH'].includes(response.request().method())) {
        const body = response.request().postDataJSON();
        const result = await response.json().catch(() => ({}));
        editingRequests.push({ path: url.pathname, status: response.status(), tab: body?.tabId,
            release: body?.release,
            blocks: body?.blocks?.map(block => ({ key: block.key, version: block.version, lease: !!block.leaseId })),
            confirmed: result.bloques?.map(block => ({ key: block.key, version: block.version, lease: block.reserva })) });
    }
});
page.on('pageerror', error => errors.push(error.message));
context.on('page', target => target.on('pageerror', error => errors.push(error.message)));
const checks = [];
async function check(name, work) {
    await work(); checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name);
    await writeFile(path.join(artifacts, 'browser.json'), JSON.stringify({ checks, inProgress: true, pageErrors: errors }, null, 2));
}
async function control(action) {
    const response = await context.request.post(`${origin}/__fixture/${action}`, { headers: { 'X-Fixture-Key': fixtureKey } });
    assert.equal(response.status(), 200); return response;
}
async function stored() { return (await control('stored')).json(); }
async function header(target = page) {
    await target.getByRole('tab', { name: 'Identificación general', exact: true }).click();
    return target.getByLabel('Trámite / servicio 1', { exact: true });
}
async function activate(input) {
    await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
}
async function savedAfter(work, status = 200, target = page) {
    const [response] = await Promise.all([target.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/bloques' && r.request().method() === 'PATCH'), work()]);
    assert.equal(response.status(), status);
}
async function finish(target = page) {
    await target.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
    await expect(target.getByRole('button', { name: 'Guardar borrador', exact: true })).toHaveAttribute('aria-busy', 'false');
    await expect(target.locator('.form-actions [role="status"]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/);
    // El indicador espera también la liberación. networkidle ya ocurrido no espera peticiones posteriores.
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
        await expect(page.getByRole('tab', { name: 'Contexto', exact: true })).toHaveAttribute('aria-selected', 'true');
        await expect(page.getByText('Datos de la sesión', { exact: true })).toHaveCount(0);
        await expect(page.getByLabel('Fecha de sesión', { exact: true })).toHaveCount(0);
        await expect(page.getByRole('button', { name: 'Imprimir', exact: true })).toHaveCount(0);
        const input = await header();
        await activate(input);
        await savedAfter(() => input.fill('Respuesta desde React áéíóú'));
        const row = await stored();
        assert.equal(row.contenido.identificacion[0].tramite, 'Respuesta desde React áéíóú');
        assert.equal(row.porcentaje, 4);
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
    await check('reserva pendiente mantiene lectura; cambiar campos del registro conserva el mismo titular', async () => {
        await finish(); const input = await header();
        const row = input.locator('xpath=ancestor::tr');
        const reservations = editingRequests.filter(r => r.path.endsWith('/reservas')).length;
        await row.getByText('Por validar', { exact: true }).click(); await page.waitForTimeout(100);
        await expect(row).toHaveAttribute('data-edit-state', 'idle');
        assert.equal(editingRequests.filter(r => r.path.endsWith('/reservas')).length, reservations);
        let grant; const gate = new Promise(resolve => { grant = resolve; });
        await page.route('**/formatos/A/reservas', async route => { await gate; await route.continue(); });
        try {
            const value = await input.inputValue(); await input.focus();
            const caret = await input.evaluate(el => el.selectionStart || 0);
            await expect(page.getByText('Preparando edición…', { exact: true })).toBeVisible();
            await expect(input).toHaveJSProperty('readOnly', true); await input.press('X');
            await input.evaluate(el => { const data = new DataTransfer(); data.setData('text/plain',' pegado'); el.dispatchEvent(new ClipboardEvent('paste',{bubbles:true,cancelable:true,clipboardData:data})); });
            assert.equal(await input.inputValue(), value); await input.press('Tab');
            grant(); await expect(input).toHaveJSProperty('readOnly', false);
            await expect(input).toHaveValue(value.slice(0, caret) + 'X pegado' + value.slice(caret));
            const releases = editingRequests.filter(r => r.path.endsWith('/liberar')).length;
            await page.getByRole('combobox', { name: 'Usuarios que atiende 1', exact: true }).focus();
            await expect(input.locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'owned');
            await page.waitForTimeout(150);
            assert.equal(editingRequests.filter(r => r.path.endsWith('/liberar')).length, releases);
        } finally { grant(); await page.unroute('**/formatos/A/reservas'); }
    });
    await check('dos pestañas: SignalR muestra titular, bloquea la misma reserva y permite otro bloque', async () => {
        const other = await context.newPage();
        await other.goto(`${origin}/formatos/A`); const otherInput = await header(other);
        await activate(page.getByLabel('Trámite / servicio 1', { exact: true }));
        await savedAfter(() => page.getByLabel('Trámite / servicio 1', { exact: true }).fill('Ganadora React'));
        await expect(otherInput).toHaveJSProperty('readOnly', true);
        await expect(other.getByText('Estás editando este registro en otra pestaña', { exact: true })).toBeVisible();
        await expect(otherInput).toHaveValue('Ganadora React');
        await expect(other.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeDisabled();
        const occupied = otherInput.locator('xpath=ancestor::tr');
        await expect(occupied).toHaveCSS('outline-width', '2px');
        await expect(occupied.locator('.MuiAvatar-root')).toBeVisible();
        await expect(occupied.locator('.MuiAvatar-root')).toHaveCSS('width', '26px');
        assert.match(await occupied.evaluate(el => getComputedStyle(el).getPropertyValue('--participant-color')), /^#[0-9a-f]{6}$/i);
        await other.setViewportSize({ width: 390, height: 844 }); await other.emulateMedia({ reducedMotion: 'reduce' });
        await expect(occupied).toHaveCSS('transition-duration', '0s');
        await other.screenshot({ path: path.join(artifacts, 'reserva-otra-pestana-movil.png'), fullPage: true });
        await other.setViewportSize({ width: 1440, height: 1000 });
        await other.getByRole('tab', { name: 'Preguntas', exact: true }).click();
        const answer = other.locator('#panel-preguntas textarea').first();
        await activate(answer);
        await savedAfter(() => answer.fill('Respuesta simultánea desde otra pestaña'), 200, other);
        assert.equal((await stored()).contenido.identificacion[0].tramite, 'Ganadora React');
        await finish(other);
        await other.close();
    });
    await check('dos sesiones compiten al enfocar; salir guarda y libera, la otra edita directamente sin recargar', async () => {
        await finish();
        const independent = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
        independent.setDefaultTimeout(15000); await isolateMicrosoft(independent);
        const other = await independent.newPage();
        other.on('pageerror', error => errors.push(error.message));
        try {
            await other.goto(origin + '/connect'); await other.waitForURL(origin + '/inicio');
            const session = cookies => cookies.find(cookie => cookie.name === 'tdv2.aspnet.session')?.value;
            assert.notEqual(session(await context.cookies()), session(await independent.cookies()));
            await other.goto(origin + '/formatos/A');
            const inputs = [await header(page), await header(other)], pages = [page, other];
            let start; const gate = new Promise(resolve => { start = resolve; });
            let arrived = 0; const statuses = [];
            for (const target of pages) await target.route('**/formatos/A/reservas', async route => {
                arrived++; await gate; const response = await route.fetch(); statuses.push(response.status()); await route.fulfill({ response });
            });
            try {
                await Promise.all(inputs.map(input => input.focus()));
                await expect.poll(() => arrived).toBe(2); start();
                await expect.poll(() => statuses.length).toBe(2);
                assert.deepEqual([...statuses].sort(), [200, 409]);
                await expect.poll(async () => (await Promise.all(inputs.map(input => input.evaluate(element => !element.readOnly)))).filter(Boolean).length).toBe(1);
                const winner = await inputs[0].evaluate(input => !input.readOnly) ? 0 : 1, loser = 1 - winner;
                await expect(inputs[winner]).toHaveJSProperty('readOnly', false);
                await expect(inputs[loser].locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'occupied');
                await expect(pages[loser].getByText(/.+ está editando$/, { exact: false })).toBeVisible();
                await expect(pages[loser].getByText('Estás editando este registro en otra pestaña', { exact: true })).toHaveCount(0);
                for (const target of pages) await expect(target.getByRole('button', { name: 'Editar registro', exact: true })).toHaveCount(0);
                await savedAfter(async () => {
                    await inputs[winner].fill('Guardado al salir de la fila');
                    await pages[winner].getByRole('tab', { name: 'Identificación general', exact: true }).focus();
                }, 200, pages[winner]);
                await expect(inputs[loser]).toHaveValue('Guardado al salir de la fila');
                await expect(inputs[loser].locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'idle');
                await inputs[loser].click();
                await expect(inputs[loser]).toHaveJSProperty('readOnly', false);
                await savedAfter(async () => {
                    await inputs[loser].fill('Ganadora React');
                    await pages[loser].getByRole('tab', { name: 'Identificación general', exact: true }).focus();
                }, 200, pages[loser]);
                await expect(inputs[winner]).toHaveValue('Ganadora React');
                await expect(inputs[winner].locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'idle');
                assert.equal((await stored()).contenido.identificacion[0].tramite, 'Ganadora React');
            } finally { start(); for (const target of pages) await target.unroute('**/formatos/A/reservas'); }
        } finally { await independent.close(); }
    });
    await check('prioridad accesible sin defecto, Contexto inicial y navegación superior móvil', async () => {
        await finish();
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const priorities = page.getByRole('combobox', { name: 'Prioridad 1', exact: true });
        await expect(priorities).toHaveText('Selecciona una prioridad');
        // El desplegable es el primer campo visitado: adquiere y abre sin botón adicional.
        await priorities.click();
        await expect(page.getByLabel('Trámite / servicio 1', { exact: true })).toHaveJSProperty('readOnly', false);
        const releases = editingRequests.filter(r => r.path.endsWith('/liberar')).length;
        await page.waitForTimeout(150);
        assert.equal(editingRequests.filter(r => r.path.endsWith('/liberar')).length, releases);
        await savedAfter(() => page.getByRole('option', { name: '5: Extremadamente prioritario', exact: true }).click());
        await priorities.focus(); await page.keyboard.press('Enter'); await page.keyboard.press('ArrowDown');
        await savedAfter(() => page.keyboard.press('Enter'));
        await expect(priorities).toHaveText('4: Muy prioritario');
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
    await check('casillas reservan automáticamente y no cambian antes de confirmar; teclado y salida guardan y liberan', async () => {
        await finish(); await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
        const block = page.locator('[data-edit-block="medios"]'), checkbox = block.getByRole('checkbox').first();
        const before = await stored(), initial = await checkbox.isChecked();
        let grant; const gate = new Promise(resolve => { grant = resolve; });
        await page.route('**/formatos/A/reservas', async route => { await gate; await route.continue(); });
        try {
            await checkbox.click();
            await expect(block.getByText('Preparando edición…', { exact: true })).toBeVisible();
            assert.equal(await checkbox.isChecked(), initial);
            assert.deepEqual((await stored()).contenido.medios, before.contenido.medios);
            await savedAfter(async () => { grant(); });
            await expect(checkbox).toBeChecked({ checked: !initial });
            const releases = editingRequests.filter(r => r.path.endsWith('/liberar')).length;
            await block.getByLabel('Otro medio', { exact: true }).focus(); await page.waitForTimeout(100);
            assert.equal(editingRequests.filter(r => r.path.endsWith('/liberar')).length, releases);
            await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).focus();
            await expect(block).toHaveAttribute('data-edit-state', 'idle');
            await checkbox.focus(); await expect(block).toHaveAttribute('data-edit-state', 'owned');
            await savedAfter(() => checkbox.press('Space'));
            await expect(checkbox).toBeChecked({ checked: initial });
            await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).focus();
            await expect(block).toHaveAttribute('data-edit-state', 'idle');
        } finally { grant(); await page.unroute('**/formatos/A/reservas'); }
        await header();
    });
    await check('error PostgreSQL conserva borrador; reintento recupera guardado', async () => {
        await control('fail-save');
        const before = await stored();
        await activate(page.getByLabel('Trámite / servicio 1', { exact: true }));
        await savedAfter(() => page.getByLabel('Trámite / servicio 1', { exact: true }).fill('Recuperación React'), 503);
        assert.equal((await stored()).version, before.version);
        assert.equal((await stored()).contenido.identificacion[0].tramite, 'Ganadora React');
        await control('recover-save');
        await expect.poll(async () => (await stored()).contenido.identificacion[0].tramite, { timeout: 15000 }).toBe('Recuperación React');
        await expect(page.getByRole('dialog')).toHaveCount(0);
        assert.equal((await stored()).contenido.identificacion[0].tramite, 'Recuperación React');
    });
    await check('respuesta perdida confirma el mismo recibo sin duplicar ni abrir resolución manual', async () => {
        await finish(); const input = await header(); await activate(input); const before = await stored();
        const operations=[]; let first=true;
        await page.route('**/formatos/A/bloques', async route => {
            operations.push(route.request().postDataJSON().operationId);
            if(first) { first=false; await route.fetch(); await route.abort('failed'); }
            else await route.continue();
        });
        try {
            await input.fill('Guardado con respuesta perdida');
            await expect.poll(()=>operations.length,{timeout:15000}).toBe(2);
            await expect.poll(async()=>(await stored()).contenido.identificacion[0].tramite).toBe('Guardado con respuesta perdida');
            assert.equal(operations[0],operations[1]); assert.equal((await stored()).version,before.version+1);
            await expect(page.getByRole('button',{name:/Resolver|Confirmar propuesta|Conservar respuesta guardada/})).toHaveCount(0);
            await expect(page.getByRole('dialog')).toHaveCount(0);
            await page.screenshot({path:path.join(artifacts,'captura-sin-resolucion-manual.png'),fullPage:true});
        } finally { await page.unroute('**/formatos/A/bloques'); }
        await finish();
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
        assert.equal(await (await header()).inputValue(), 'Guardado con respuesta perdida');
        await page.screenshot({ path: path.join(artifacts, 'formato-postgresql.png') });
    });
    await check('el resumen del servidor enlaza pendientes y no ofrece envío de formato incompleto', async () => {
        await finish();
        await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
        await expect(page.getByRole('heading', { name: 'Revisión y envío de la primera etapa' })).toBeVisible();
        await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toHaveCount(0);
        await page.getByRole('button', { name: /Identificación general · Guardado con respuesta perdida:/ }).click();
        await expect(page.getByRole('tab', { name: 'Identificación general', exact: true })).toHaveAttribute('aria-selected', 'true');
        await expect(page.getByRole('combobox', { name: 'Usuarios que atiende 1', exact: true })).toBeFocused();
        assert.equal((await stored()).contenido.identificacion[0].tramite, 'Guardado con respuesta perdida');
        await finish(); // El enlace enfocó y reservó el campo; la petición de prueba usa otra pestaña.
        // Aislar requisitos de llenado de las reservas de las pestañas cerradas en los casos anteriores.
        await control('expire-editing');
        const response = await page.evaluate(async () => {
            const csrf = await (await fetch('/session/csrf')).json();
            const live = await (await fetch('/formatos/A/estado')).json();
            return (await fetch('/formatos/A/enviar', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own' },
                body: JSON.stringify({ tabId: crypto.randomUUID(), operationId: crypto.randomUUID(), version: live.version }) })).status;
        });
        assert.equal(response, 422);
    });
    await check('Cancelar tiene foco y no elimina filas en las cuatro tablas; borrar disponible es rojo', async () => {
        await finish(); await control('ready-to-submit'); await page.reload();
        for (const [tab, label] of [['Identificación general', 'Eliminar registro'], ['Sistemas y herramientas', 'Eliminar registro'], ['Datos', 'Eliminar registro'], ['Acuerdos', 'Eliminar registro']]) {
            await page.getByRole('tab', { name: tab, exact: true }).click();
            const before = (await stored()).contenido;
            const button = page.getByRole('button', { name: label, exact: true }).first();
            await expect(button.locator('xpath=ancestor::tr')).toHaveAttribute('data-edit-state', 'idle');
            const reservations = editingRequests.filter(r => r.path.endsWith('/reservas')).length;
            await expect(button).toBeEnabled();
            await expect(button).toHaveCSS('color', 'rgb(211, 47, 47)');
            await button.click();
            const dialog = page.getByRole('dialog', { name: 'Eliminar registro' });
            await expect(dialog).toBeVisible(); await expect(dialog.getByRole('button', { name: 'Cancelar', exact: true })).toBeFocused();
            await expect(dialog.getByText(/Respuesta sintética/)).toBeVisible();
            if (tab === 'Identificación general') await expect(dialog.getByText(/su evaluación/)).toBeVisible();
            await dialog.getByRole('button', { name: 'Cancelar', exact: true }).click();
            assert.deepEqual((await stored()).contenido, before);
            assert.equal(editingRequests.filter(r => r.path.endsWith('/reservas')).length, reservations);
        }
    });
    await verifyParticipants({ check, page, context, origin, finish, artifacts });
    await verifyDirectDeletion({ check, page, context, browser, origin, control, stored, finish, savedAfter, isolateMicrosoft, errors, artifacts });
    await check('criterios individuales mantienen reservas independientes y guardan sin sobrescribirse', async () => {
        await finish(); await page.getByRole('tab', { name: 'Evaluación', exact: true }).click();
        const first = page.locator('[data-edit-block="evaluaciones:PO-01:0"]');
        await savedAfter(() => first.getByRole('radio', { name: '4', exact: true }).click());
        await expect(first).toHaveAttribute('data-edit-state', 'owned');
        const other = await context.newPage(); await other.goto(origin + '/formatos/A');
        await other.getByRole('tab', { name: 'Evaluación', exact: true }).click();
        await expect(other.locator('[data-edit-block="evaluaciones:PO-01:0"]').getByRole('radio').first()).toBeDisabled();
        const second = other.locator('[data-edit-block="evaluaciones:PO-01:1"]');
        await savedAfter(() => second.getByRole('radio', { name: '3', exact: true }).click(), 200, other);
        await expect(second).toHaveAttribute('data-edit-state', 'owned');
        const values = (await stored()).contenido.evaluaciones['PO-01'];
        assert.equal(values[0].valor, '4'); assert.equal(values[1].valor, '3');
        await finish(other); await other.close(); await finish();
    });
    await check('el diálogo conserva el ID elegido cuando otra pestaña elimina una fila anterior', async () => {
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        for (const [number, label] of [[2, 'Proceso elegido'], [3, 'Proceso que permanece']]) {
            await page.getByRole('button', { name: 'Agregar trámite o servicio', exact: true }).click();
            await savedAfter(() => page.getByLabel('Trámite / servicio ' + number, { exact: true }).fill(label));
            await finish();
        }
        await page.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(1).click();
        const other = await context.newPage(); await other.goto(origin + '/formatos/A'); await header(other);
        await other.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0).click();
        await savedAfter(() => other.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click(), 200, other);
        await expect(page.getByLabel('Trámite / servicio 1', { exact: true })).toHaveValue('Proceso elegido');
        await savedAfter(() => page.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click());
        const content = (await stored()).contenido;
        assert.deepEqual(content.identificacion.map(row => row.tramite), ['Proceso que permanece']);
        assert.deepEqual(content.evaluaciones, {}); assert.equal(content.sistemas[0].proceso, ''); assert.equal(content.datos[0].proceso, '');
        await other.close(); await finish();
        for (const [tab, section] of [['Sistemas y herramientas', 'sistemas'], ['Datos', 'datos'], ['Acuerdos', 'acuerdos']]) {
            await page.getByRole('tab', { name: tab, exact: true }).click();
            await page.getByRole('button', { name: 'Eliminar registro', exact: true }).first().click();
            await savedAfter(() => page.getByRole('dialog').getByRole('button', { name: 'Eliminar', exact: true }).click());
            assert.equal((await stored()).contenido[section].length, 0);
            await finish();
        }
    });
    await check('desconexión conserva texto; reconectar confirma permisos y guarda sin resolución manual', async () => {
        await page.getByRole('tab', { name: 'Preguntas', exact: true }).click();
        const input = page.locator('#panel-preguntas textarea').first(); await activate(input);
        await input.fill('Propuesta durante desconexión'); await context.setOffline(true);
        await expect(input).toHaveJSProperty('readOnly', true);
        await expect(input).toHaveValue('Propuesta durante desconexión');
        await expect(page.getByRole('button', { name: /Resolver|Confirmar propuesta|Conservar respuesta guardada/ })).toHaveCount(0);
        await context.setOffline(false);
        await expect.poll(async () => (await stored()).contenido.preguntas.some(q => q.respuesta === 'Propuesta durante desconexión'), { timeout: 15000 }).toBe(true);
        await expect(page.getByRole('dialog')).toHaveCount(0);
        assert.ok((await stored()).contenido.preguntas.some(q => q.respuesta === 'Propuesta durante desconexión'));
        await finish();
    });
    await check('SignalR inactivo revalida durante tres intervalos, vence reserva sin actividad y cancela limpiamente', async () => {
        await page.getByRole('tab', { name: 'Preguntas', exact: true }).click();
        const input = page.locator('#panel-preguntas textarea').first(); await activate(input);
        const id = await watchStream(page);
        await page.waitForFunction(id => window.syntheticStreams[id].items.length >= 4, id, { timeout: 53000 });
        const stream = await streamState(page, id);
        assert.equal(stream.error, ''); assert.equal(new Set(stream.items.map(i => i.version)).size, 1);
        assert.ok(stream.items[3].at - stream.items[0].at >= 40000);
        await expect(input).toHaveJSProperty('readOnly', true);
        await expect(page.getByRole('button', { name: 'Resolver', exact: true })).toHaveCount(0);
        await cancelStream(page, id); assert.equal((await streamState(page, id)).error, '');
        const again = await watchStream(page); await cancelStream(page, again);
    });
    await check('Enviar formato aparece sólo en Acuerdos; confirma sin datos de sesión y bloquea la captura', async () => {
        await control('ready-to-submit'); await page.reload();
        await page.getByRole('tab', { name: 'Contexto', exact: true }).click();
        await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toHaveCount(0);
        const input = await header();
        await activate(input);
        await savedAfter(() => input.fill('Proceso del envío sintético'));
        await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
        await page.getByRole('button', { name: 'Enviar formato', exact: true }).click();
        const dialog = page.getByRole('dialog', { name: 'Enviar formato', exact: true });
        await expect(dialog.getByText('100 · Área sintética A', { exact: true })).toBeVisible();
        await expect(dialog.getByText(/Después de enviar, este formato quedará bloqueado para edición/)).toBeVisible();
        await expect(dialog.getByRole('button', { name: 'Cancelar', exact: true })).toBeFocused();
        await dialog.getByRole('button', { name: 'Cancelar', exact: true }).click();
        await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toBeEnabled();
        await page.getByRole('button', { name: 'Enviar formato', exact: true }).click();
        const [response] = await Promise.all([page.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/enviar'),
            dialog.getByRole('button', { name: 'Confirmar envío', exact: true }).click()]);
        assert.equal(response.status(), 200);
        await expect(page.getByRole('dialog')).toHaveCount(0);
        await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toBeDisabled();
        await expect(page.getByText(/Enviado el .* por persona@uacj.mx/)).toBeVisible();
        await page.reload(); await header();
        await expect(input).toHaveJSProperty('readOnly', true);
        await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toHaveCount(0);
        await expect(page.getByRole('button', { name: 'Imprimir', exact: true })).toHaveCount(0);
        await expect(page.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeDisabled();
        const content = (await stored()).contenido;
        assert.deepEqual(content.encabezado, { fecha: '', area: '', responsable: '' });
        await page.screenshot({ path: path.join(artifacts, 'formato-enviado.png'), fullPage: true });
    });
    await verifyCentralCollaboration({ page, context, origin, control, check });
    await check('logout desde React revoca sesión y redirige a Microsoft', async () => {
        const observer = await context.newPage(); await observer.goto(origin + '/formatos/A');
        const stream = await watchStream(observer);
        await page.getByRole('button', { name: 'Abrir menú de usuario' }).click();
        const logout = page.waitForResponse(r => new URL(r.url()).pathname === '/logout' && r.request().method() === 'POST');
        await page.getByRole('menuitem', { name: 'Cerrar sesión' }).click();
        const result = await logout;
        assert.equal(result.status(), 200);
        await page.waitForURL(`${origin}/session/microsoft-logout`);
        assert.equal(microsoftLogoutTarget?.searchParams.get('post_logout_redirect_uri'), origin + '/');
        assert.equal((await page.goto(`${origin}/inicio`)).status(), 401);
        await expect.poll(async () => (await streamState(observer, stream)).completed, { timeout: 20000 }).toBe(true);
        assert.ok((await streamState(observer, stream)).error);
        await observer.close();
    });
    assert.deepEqual(errors, []);
    await writeFile(path.join(artifacts, 'browser.json'), JSON.stringify({ browser: browser.version(), checks, pageErrors: errors, externalFontsBlocked: true }, null, 2));
} catch (error) {
    console.error('BROWSER FAIL', error.message, error.stack);
    await page.screenshot({ path: path.join(artifacts, 'browser-failure.png') }).catch(() => {});
    await writeFile(path.join(artifacts, 'browser.json'), JSON.stringify({ checks, failed: true, error: error.message, pageErrors: errors, editingRequests }, null, 2));
    process.exitCode = 1;
} finally { await browser.close(); }
