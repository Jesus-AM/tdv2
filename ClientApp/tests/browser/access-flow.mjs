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
    if (url.origin !== origin) return route.abort();
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
    await target.getByRole('tab', { name: 'Identificación general', exact: true }).click();
    return target.getByLabel('Trámite / servicio 1', { exact: true });
}
async function savedAfter(work, status = 200, target = page) {
    const response = target.waitForResponse(r => new URL(r.url()).pathname === '/formatos/A/bloques' && r.request().method() === 'PATCH');
    await work();
    assert.equal((await response).status(), status);
}

async function report() { return (await control('access-report')).json(); }
async function request(route, method = 'GET', body) {
    return page.evaluate(async ({ route, method, body }) => {
        const p = await (await fetch('/inicio', { headers: { Accept: 'application/json' } })).json();
        const csrf = await (await fetch('/session/csrf')).json();
        const response = await fetch(route, { method, headers: {
            Accept: 'application/json', 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.token,
            'X-TDV2-Context': p.props?.contextoEdicion || p.contextoEdicion || 'own',
        }, ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
        return { status: response.status, body: await response.json() };
    }, { route, method, body });
}
async function searchCollaborator(query, status = 200) {
    const response = page.waitForResponse(r => new URL(r.url()).pathname === '/colaboradores/personas'
        && new URL(r.url()).searchParams.get('q') === query);
    await page.getByLabel('Buscar persona', { exact: true }).fill(query);
    assert.equal((await response).status(), status);
}
async function add(kind = 'local') {
    await searchCollaborator('colaboradora');
    await page.getByRole('button', { name: /Colaboradora sintética.*colaboradora@uacj/ }).click();
    if (kind === 'dependencias') await page.getByRole('radio', { name: /Colaborador de áreas dependientes/ }).check();
    const response = page.waitForResponse(r => new URL(r.url()).pathname === '/colaboradores' && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Agregar colaborador', exact: true }).click();
    assert.equal((await response).status(), 201);
    await page.getByRole('button', { name: 'Retirar a Colaboradora sintética', exact: true }).waitFor();
}
async function retire(status) {
    await page.getByRole('button', { name: 'Retirar a Colaboradora sintética', exact: true }).click();
    const response = page.waitForResponse(r => new URL(r.url()).pathname.startsWith('/colaboradores/') && r.request().method() === 'DELETE');
    await page.getByRole('dialog').getByRole('button', { name: 'Retirar colaboración', exact: true }).click();
    assert.equal((await response).status(), status);
}
async function startRepresentation(write = false) {
    await page.goto(origin + '/configuracion/pruebas-acceso');
    await page.getByRole('button', { name: 'Seleccionar usuario', exact: true }).click();
    await page.getByLabel('Nombre o correo institucional').fill('encargada');
    await page.getByRole('button', { name: 'Buscar', exact: true }).click();
    await page.getByRole('button', { name: /Encargada sintética.*encargada@uacj/ }).click();
    await page.getByLabel('Motivo de la representación').fill('Prueba sintética desde React');
    if (write) await page.getByRole('checkbox', { name: 'Permitir cambios reales con los permisos de esta persona' }).check();
    await page.getByRole('button', { name: 'Actuar como Encargada sintética', exact: true }).click();
    await page.waitForURL(origin + '/inicio');
    await page.getByText('Actuando como Encargada sintética', { exact: true }).waitFor();
}
async function exit() {
    await page.getByRole('button', { name: 'Volver a mi usuario', exact: true }).click();
    await page.waitForURL(origin + '/inicio');
    await page.getByRole('heading', { name: 'Procesos operativos', exact: true }).waitFor();
    const own = await request('/inicio');
    assert.equal(own.body.props.contextoEdicion, 'own');
    assert.equal(own.body.props.auth.user.email, 'persona@uacj.mx');
    assert.equal(own.body.props.representacion, null);
}
try {
    await check('login y Configuración conservan las dos herramientas separadas', async () => {
        await page.goto(origin);
        await page.getByRole('link', { name: /Registrar un procedimiento institucional/ }).click();
        await page.waitForURL(origin + '/inicio');
        await page.goto(origin + '/configuracion/pruebas-acceso');
        await page.getByRole('heading', { name: 'Actuar como usuario', exact: true }).waitFor();
        await page.getByRole('button', { name: 'Probar rol y área', exact: true }).waitFor();
    });
    await check('módulos principales y desplegable de Configuración respetan jerarquía, teclado y móvil', async () => {
        const navigation = page.getByRole('navigation', { name: 'Módulos', exact: true });
        await expect(navigation.getByRole('button')).toHaveCount(2);
        await expect(navigation.getByRole('button', { name: 'Sincronizaciones', exact: true })).toHaveCount(0);
        const config = navigation.getByRole('button', { name: 'Configuración', exact: true });
        await config.click();
        await expect(page.getByRole('menuitem', { name: 'Pruebas de acceso', exact: true })).toHaveAttribute('aria-current', 'page');
        await expect(page.getByRole('menuitem', { name: 'Sincronizaciones', exact: true })).toHaveCount(0);
        await page.getByRole('menuitem', { name: 'Configuración', exact: true }).focus();
        await page.keyboard.press('ArrowDown');
        await expect(page.getByRole('menuitem', { name: 'Pruebas de acceso', exact: true })).toBeFocused();
        await page.keyboard.press('Enter');
        await page.getByRole('heading', { name: 'Actuar como usuario', exact: true }).waitFor();
        await config.click(); await expect(page.getByRole('menuitem', { name: 'Pruebas de acceso', exact: true })).toHaveAttribute('aria-current', 'page');
        await page.keyboard.press('Escape'); await expect(config).toBeFocused();
        await page.setViewportSize({ width: 390, height: 844 }); await page.emulateMedia({ reducedMotion: 'reduce' });
        await page.getByRole('button', { name: 'Abrir navegación', exact: true }).click();
        await navigation.getByRole('button', { name: 'Configuración', exact: true }).click();
        await page.getByRole('menuitem', { name: 'Pruebas de acceso', exact: true }).waitFor();
        await page.screenshot({ path: path.join(artifacts, 'modulos-menu-movil.png') });
        await page.getByRole('menuitem', { name: 'Pruebas de acceso', exact: true }).click();
        await page.getByRole('heading', { name: 'Actuar como usuario', exact: true }).waitFor();
        await expect(navigation).toHaveCount(0);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await page.screenshot({ path: path.join(artifacts, 'modulos-movil.png'), fullPage: true });
        await page.setViewportSize({ width: 1440, height: 1000 }); await page.emulateMedia({ reducedMotion: 'no-preference' });
    });
    await check('búsqueda distingue resultado vacío, falta de delegación y conexión fallida', async () => {
        await page.goto(origin + '/colaboradores');
        await searchCollaborator('inexistente');
        await page.getByText('No encontramos personas disponibles.', { exact: true }).waitFor();
        await control('search-outage'); await searchCollaborator('fallida', 503);
        await page.getByText('No se pudo comprobar la operación con Nexo. Inténtalo más tarde.', { exact: true }).first().waitFor();
        assert.equal(await page.getByText('No encontramos personas disponibles.', { exact: true }).count(), 0);
        await control('search-restore'); await control('delegation-off'); await searchCollaborator('denegada', 403);
        await page.getByText('El área no está habilitada para delegar en Nexo y TDV2.', { exact: true }).first().waitFor();
        assert.equal(await page.getByText('No encontramos personas disponibles.', { exact: true }).count(), 0);
        await control('delegation-on');
    });
    await check('alta React de persona subordinada usa formato de UR nivel 3 sin crear otro', async () => {
        await add();
        const result = await report();
        assert.equal(result.links[0].scope, 'A3'); assert.equal(result.links[0].kind, 'local');
        assert.equal(result.forms, 0);
        assert.ok(result.audits.some(a => a.actor === 'persona@uacj.mx' && a.action === 'crear' && a.entity === 'colaboracion'));
        assert.equal((await request('/colaboradores', 'POST', { ur: 'B', email: 'ajena@uacj.mx', tipo: 'local', id_ur: 'B4' })).status, 403);
    });
    await check('retiro conserva revocación local ante error y permite confirmar reintento', async () => {
        await control('retire-outage'); await retire(202);
        await page.getByText('Retiro pendiente de confirmar en Nexo', { exact: true }).waitFor();
        assert.ok((await report()).links[0].revoked && (await report()).links[0].pending);
        await control('retire-restore'); await retire(200);
        assert.ok((await report()).links[0].revoked); assert.equal((await report()).links[0].pending, false);
        await page.getByRole('button', { name: 'Retirar a Colaboradora sintética', exact: true }).waitFor({ state: 'detached' });
    });
    await check('colaboración de áreas dependientes conserva alcance de la rama nivel 2', async () => {
        await add('dependencias');
        assert.ok((await report()).links.some(l => l.kind === 'dependencias' && l.scope === 'A' && !l.revoked));
    });
    await check('administrador sin capacidad Nexo no puede representar desde pantalla ni endpoint', async () => {
        await control('rep-deny'); await page.goto(origin + '/configuracion/pruebas-acceso');
        assert.ok(await page.getByRole('button', { name: 'Seleccionar usuario', exact: true }).isDisabled());
        assert.equal((await request('/actuar-como-usuario', 'POST', { email: 'encargada@uacj.mx', motivo: 'Prueba directa', escritura: true })).status, 403);
        await control('rep-allow');
    });
    await check('representación de consulta bloquea edición, UR ajena y permite volver al actor', async () => {
        await startRepresentation(false);
        await page.getByText('Solo lectura', { exact: true }).waitFor();
        const form = await request('/formatos/A');
        assert.equal(form.body.props.editable, false);
        assert.equal((await request('/formatos/A', 'PUT', { version: 0, contenido: form.body.props.contenido })).status, 409);
        assert.equal((await page.goto(origin + '/formatos/B')).status(), 403);
        await exit();
        assert.equal((await request('/formatos/B')).status, 200); // Actor's institutional consultation restored.
    });
    await check('representación con escritura guarda desde React y audita ambas identidades', async () => {
        await startRepresentation(true);
        await page.getByText('Cambios reales habilitados', { exact: true }).waitFor();
        await page.goto(origin + '/formatos/A');
        const field = await header(); await field.focus(); await expect(field).toHaveJSProperty('readOnly', false);
        await savedAfter(() => field.fill('Operación representada desde React'));
        assert.equal((await stored()).contenido.identificacion[0].tramite, 'Operación representada desde React');
        const form = await request('/formatos/A');
        assert.equal((await request('/formatos/B', 'PUT', { version: 0, contenido: form.body.props.contenido })).status, 403);
        assert.equal((await request('/configuracion')).status, 409);
        assert.ok((await report()).audits.some(a => a.action === 'guardar_bloques' && a.actor === 'persona@uacj.mx' && a.meta.representado_email === 'encargada@uacj.mx'));
        await page.screenshot({ path: path.join(artifacts, 'representacion-activa.png') });
        await exit();
    });
    await check('representación revocada o vencida no restaura permisos de administrador automáticamente', async () => {
        for (const action of ['rep-revoke', 'rep-expire']) {
            await startRepresentation(); await control(action);
            assert.equal((await page.goto(origin + '/inicio')).status(), 409);
            await page.getByRole('button', { name: 'Volver a mi usuario', exact: true }).waitFor();
            await exit();
        }
    });
    await check('fallo de Nexo bloquea representación y permite terminarla explícitamente', async () => {
        await startRepresentation(); await control('rep-outage');
        assert.equal((await page.goto(origin + '/inicio')).status(), 503);
        await exit(); await control('rep-restore');
    });
    await check('vista por rol y área es de consulta y rechaza elusión por endpoints', async () => {
        await page.goto(origin + '/configuracion/pruebas-acceso');
        await page.getByRole('button', { name: 'Probar rol y área', exact: true }).click();
        await page.getByRole('combobox', { name: 'Rol', exact: true }).click();
        await page.getByRole('option', { name: 'Colaborador local', exact: true }).click();
        await page.getByRole('combobox', { name: 'Área de pertenencia o responsabilidad', exact: true }).fill('Área sintética A4');
        await page.getByRole('option', { name: /Área sintética A4/ }).click();
        await page.getByRole('button', { name: 'Iniciar vista de prueba', exact: true }).click();
        await page.waitForURL(origin + '/inicio');
        const form = await request('/formatos/A3'); assert.equal(form.body.props.editable, false);
        assert.equal((await request('/formatos/A3', 'PUT', { version: 0, contenido: form.body.props.contenido })).status, 403);
        assert.equal((await request('/colaboradores/personas?ur=A&q=colaboradora')).status, 403);
        assert.equal((await request('/configuracion')).status, 409);
        await page.screenshot({ path: path.join(artifacts, 'vista-rol-area.png') });
        await page.getByRole('button', { name: 'Salir de la prueba', exact: true }).click();
        await page.getByRole('button', { name: 'Salir de la prueba', exact: true }).waitFor({ state: 'detached' });
        assert.equal((await request('/inicio')).body.props.contextoEdicion, 'own');
    });
    await check('PostgreSQL confirma auditoría sin tokens y todos los contextos finalizados', async () => {
        const result = await report(); assert.equal(result.activeContexts, 0); assert.equal(result.secretLeaks, 0);
        assert.ok(result.audits.some(a => a.meta.resultado === 'rechazado'));
        assert.ok(result.audits.some(a => a.action === 'finalizar' && a.entity === 'representacion'));
        assert.equal(errors.length, 0);
    });
} catch (error) {
    await page.screenshot({ path: path.join(artifacts, 'access-failure.png') }).catch(() => {});
    throw error;
} finally {
    await writeFile(path.join(artifacts, 'browser-access.json'), JSON.stringify({
        utc: new Date().toISOString(), browser: browser.version(), realPostgreSql: true,
        microsoft: 'simulated local redirects and HTTP peer', nexo: 'synthetic PostgreSQL views and functions',
        externalRequestsBlocked: true, checks, pageErrors: errors,
    }, null, 2));
    await browser.close();
}
