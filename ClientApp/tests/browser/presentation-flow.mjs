import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, output = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !output) throw new Error('Sólo host sintético aislado.');
const phase = process.env.TDV2_UI_PHASE || 'after';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 900 } });
context.setDefaultTimeout(20000);
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            const target = new URL(headers.location);
            headers.location = origin + '/connect?code=synthetic-code&state=' + encodeURIComponent(target.searchParams.get('state'));
        }
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const page = await context.newPage(), report = { phase, synthetic: true, browser: browser.version(), completed: false, screenshots: [], checks: [], errors: [] };
page.on('pageerror', error => report.errors.push(error.message));
const pages = [
    ['inicio', '/inicio', 'Procesos operativos'],
    ['configuracion', '/configuracion', 'Configuración'],
    ['sincronizaciones', '/configuracion/sincronizaciones', 'Sincronizaciones'],
    ['pruebas', '/configuracion/pruebas-acceso', 'Pruebas de acceso'],
    ['procesos', '/configuracion/procesos', 'Configuración procesos'],
    ['rol-area', '/configuracion/pruebas-acceso/rol-area', 'Vista por rol y área'],
    ['representacion', '/configuracion/pruebas-acceso/actuar-como-usuario', 'Actuar como usuario'],
];
async function capture(name) {
    await expect(page.locator('h1')).toHaveCount(1);
    await page.screenshot({ path: path.join(output, name + '.png'), fullPage: true, animations: 'disabled' });
    const geometry = await page.evaluate(() => ({
        width: innerWidth, overflow: document.documentElement.scrollWidth > innerWidth + 1,
        h1Count: document.querySelectorAll('h1').length,
        barBottom: document.querySelector('header.MuiAppBar-root')?.getBoundingClientRect().bottom,
        titleTop: document.querySelector('h1')?.getBoundingClientRect().top,
    }));
    report.screenshots.push({ name, ...geometry });
    if (phase === 'after') { assert.equal(geometry.overflow, false, name + ': desbordamiento'); assert.equal(geometry.h1Count, 1); assert.ok(geometry.titleTop >= geometry.barBottom); }
}
try {
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    for (const width of [360, 768, 1366, 1920]) {
        await page.setViewportSize({ width, height: width === 1920 ? 1080 : 900 });
        for (const [name, url, title] of pages) {
            await page.goto(origin + url); await expect(page.getByRole('heading', { name: title, exact: true, level: 1 })).toBeVisible();
            await capture(phase + '-' + name + '-' + width);
        }
    }
    await page.setViewportSize({ width: 1366, height: 900 });
    for (const [name, url, title] of pages) {
        await page.goto(origin + url);
        await expect(page.getByRole('heading', { name: title, exact: true, level: 1 })).toBeVisible();
        // Emulación explícita de ampliación CSS; el reflujo estrecho se verifica además a 360/768 px.
        await page.evaluate(() => document.documentElement.style.zoom = '2');
        await capture(phase + '-' + name + '-zoom200');
    }
    await page.evaluate(() => document.documentElement.style.zoom = '1');
    if (phase === 'after') await interactions();
    assert.deepEqual(report.errors, []);
    report.checks.push('7 pantallas × 4 anchos + ampliación CSS 200%, sin errores JS');
    report.completed = true;
} finally {
    await writeFile(path.join(output, 'ui-presentation-' + phase + '.json'), JSON.stringify(report, null, 2));
    await browser.close();
}
console.log('PASS presentation ' + phase + ': ' + report.screenshots.length + ' capturas');

async function interactions() {
    const nav = () => page.getByRole('navigation', { name: 'Módulos', exact: true });
    const check = async (name, work) => { await work(); report.checks.push(name); console.log('UI PASS ' + name); };
    await check('Navegación, selección, teclado, ruta al padre y menú de cuenta sin desplazamiento', async () => {
        await page.goto(origin + '/configuracion/sincronizaciones');
        const button = nav().getByRole('button', { name: 'Configuración', exact: true });
        await expect(button).toBeVisible();
        const logo = await page.getByRole('img', { name: 'Transformación Digital' }).boundingBox();
        const navigation = await nav().boundingBox(); assert.equal(navigation.x - logo.x - logo.width, 32);
        await button.focus(); await page.keyboard.press('Enter');
        await expect(page.getByRole('menuitem', { name: 'Sincronizaciones', exact: true })).toHaveAttribute('aria-current', 'page');
        assert.deepEqual(await page.getByRole('menuitem').allTextContents(), ['Configuración', 'Configuración procesos', 'Sincronizaciones', 'Pruebas de acceso']);
        await page.keyboard.press('Escape'); await expect(button).toBeFocused();
        const before = await page.locator('h1').boundingBox();
        await page.getByRole('button', { name: 'Abrir menú de usuario' }).click();
        const after = await page.locator('h1').boundingBox(); assert.deepEqual(after, before);
        await expect(page.getByRole('menuitem', { name: 'Usar otra cuenta' })).toBeVisible();
        await capture('after-account-menu-1366'); await page.keyboard.press('Escape');
        await page.getByRole('navigation', { name: 'Ubicación' }).getByRole('link', { name: 'Configuración', exact: true }).click();
        await expect(page.getByRole('heading', { level: 1, name: 'Configuración', exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'Abrir configuración procesos' }).click();
        await expect(page.getByRole('heading', { level: 1, name: 'Configuración procesos' })).toBeVisible();
    });
    await check('Select, cambios pendientes, vista previa y Cancelar conservan configuración', async () => {
        const read = async () => (await (await context.request.get(origin + '/configuracion/procesos', { headers: { 'X-TDV2-Page': '1' } })).json()).props.settings;
        const before = await read();
        await page.getByRole('combobox', { name: 'Niveles participantes' }).click();
        await page.getByRole('option', { name: 'Nivel 4', exact: true }).click(); await page.keyboard.press('Escape');
        await expect(page.getByText('Cambios pendientes', { exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'Revisar cambios' }).click();
        await expect(page.getByRole('dialog')).toBeVisible(); await capture('after-process-confirmation-1366');
        await expect(page.getByRole('button', { name: 'Cancelar', exact: true })).toBeFocused();
        await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
        assert.deepEqual(await read(), before);
        await expect(page.getByRole('combobox', { name: 'Niveles participantes' })).toContainText('Nivel 4');
        await page.getByRole('combobox', { name: 'Niveles participantes' }).click();
        await page.getByRole('option', { name: 'Nivel 4', exact: true }).click(); await page.keyboard.press('Escape');
        await expect(page.getByRole('button', { name: 'Revisar cambios' })).toBeDisabled();
    });
    await check('Carga sin salto, error real conserva borrador y guardado de programación aislada', async () => {
        await page.goto(origin + '/configuracion/sincronizaciones');
        await page.getByRole('combobox', { name: 'Frecuencia' }).click();
        await page.getByRole('option', { name: 'Cada 3 horas', exact: true }).click();
        const refresh = page.getByRole('button', { name: 'Actualizar estado', exact: true });
        const source = page.getByRole('heading', { name: 'SII · Unidades responsables', exact: true });
        let release;
        const gate = new Promise(resolve => release = resolve);
        const pattern = origin + '/configuracion/sincronizaciones';
        await context.route(pattern, async route => { const response = await route.fetch(); await gate; await route.fulfill({ response }); });
        const before = await source.boundingBox();
        await refresh.click(); await expect(refresh).toBeDisabled();
        assert.deepEqual(await source.boundingBox(), before); await capture('after-sync-loading-1366');
        release(); await expect(refresh).toBeEnabled(); await context.unroute(pattern);
        await context.route(pattern, route => route.fulfill({ status: 503, contentType: 'application/json', body: JSON.stringify({ message: 'No se pudo actualizar el estado. Inténtalo de nuevo.' }) }));
        await refresh.click(); await expect(page.getByRole('alert').filter({ hasText: 'No se pudo actualizar' })).toBeVisible();
        assert.deepEqual(await source.boundingBox(), before);
        await expect(page.getByRole('combobox', { name: 'Frecuencia' })).toContainText('Cada 3 horas');
        await capture('after-sync-error-1366');
        await page.setViewportSize({ width: 360, height: 900 }); await capture('after-sync-error-360');
        await page.setViewportSize({ width: 1366, height: 900 }); await context.unroute(pattern);
        await refresh.click(); await expect(page.getByRole('alert').filter({ hasText: 'No se pudo actualizar' })).toHaveCount(0);
        await page.getByRole('button', { name: 'Guardar programación', exact: true }).click();
        await expect(page.getByText('Cambios pendientes', { exact: true })).toHaveCount(0);
        await expect(page.getByLabel('Activar sincronización automática', { exact: true })).not.toBeChecked();
    });
    await check('Representación conserva búsqueda y advertencia de escritura real', async () => {
        await page.goto(origin + '/configuracion/pruebas-acceso/actuar-como-usuario');
        await page.getByRole('textbox', { name: 'Nombre o correo institucional' }).fill('encargada');
        await page.getByRole('button', { name: 'Buscar', exact: true }).click();
        await page.getByRole('button', { name: /Encargada sintética/ }).click();
        await page.getByLabel('Permitir cambios reales con los permisos de esta persona').check();
        await expect(page.getByRole('alert').filter({ hasText: 'Los cambios se guardarán' })).toBeVisible();
        await capture('after-representation-warning-1366');
    });
    await check('Móvil, menú jerárquico, movimiento reducido y textos largos', async () => {
        await page.setViewportSize({ width: 360, height: 900 }); await page.emulateMedia({ reducedMotion: 'reduce' });
        await page.goto(origin + '/configuracion');
        await page.getByRole('button', { name: 'Abrir navegación' }).click();
        await nav().getByRole('button', { name: 'Configuración', exact: true }).click();
        await page.screenshot({ path: path.join(output, 'after-mobile-menu-360.png'), fullPage: true });
        await page.getByRole('menuitem', { name: 'Sincronizaciones', exact: true }).click();
        await expect(page.getByRole('heading', { level: 1, name: 'Sincronizaciones' })).toBeVisible();
        await expect(page.getByRole('button', { name: 'Cerrar navegación' })).toHaveCount(0);
        // Textos sintéticos de estrés visual; no sustituyen permisos ni escriben datos.
        await page.evaluate(() => {
            document.querySelector('h1').textContent += ' · Administración de catálogos e inventarios institucionales';
            document.querySelector('.page-description').textContent += ' Seguimiento de unidades responsables y dependencias con nombres largos.';
        });
        await capture('after-long-text-360');
        assert.equal(await page.locator('.page-header').evaluate(el => getComputedStyle(el).animationName), 'none');
        await page.goto(origin + '/configuracion/pruebas-acceso/rol-area');
        await page.getByRole('combobox', { name: 'Área de pertenencia o responsabilidad' }).click();
        await page.getByRole('option').first().click();
        await expect(page.getByRole('button', { name: 'Iniciar vista de prueba' })).toBeEnabled();
        await capture('after-area-selected-360');
    });
    await check('Sin permiso de submódulo: menú y acceso directo respetan Nexo sintético', async () => {
        assert.equal((await context.request.post(origin + '/__fixture/sync-access-off', { headers: { 'X-Fixture-Key': key } })).status(), 200);
        await page.goto(origin + '/configuracion');
        await expect(page.getByRole('button', { name: 'Abrir sincronizaciones' })).toHaveCount(0);
        await capture('after-permissions-360');
        assert.equal((await context.request.get(origin + '/configuracion/sincronizaciones', { headers: { 'X-TDV2-Page': '1' } })).status(), 403);
        assert.equal((await context.request.post(origin + '/__fixture/sync-access-on', { headers: { 'X-Fixture-Key': key } })).status(), 200);
    });
}
