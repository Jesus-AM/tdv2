import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';

const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Sólo host sintético aislado.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const report = { synthetic: true, checks: [], errors: [] }, requests = [];
async function session() {
    const context = await browser.newContext({ ignoreHTTPSErrors: true, hasTouch: true, viewport: { width: 1366, height: 768 } });
    await context.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.origin !== origin && !(url.protocol === 'wss:' && url.host === new URL(origin).host)) return route.abort();
        if (url.pathname === '/connect') {
            const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
            if (headers.location?.startsWith('https://login.microsoftonline.com/'))
                headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
            return route.fulfill({ response, headers });
        }
        return route.continue();
    });
    const page = await context.newPage();
    page.on('pageerror', e => report.errors.push(e.message));
    page.on('request', r => { if (r.method() === 'POST' && r.url().endsWith('/reservas/liberar')) requests.push(r.url()); });
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    await page.goto(origin + '/formatos/A'); await header(page);
    return { context, page };
}
async function header(page) {
    await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
    // El HTML inicial precede a la revalidación de sesión/reservas; no interactuar durante esa carga.
    await expect(page.getByRole('combobox', { name: 'Usuarios que atiende 1', exact: true })).not.toHaveAttribute('aria-readonly', 'true');
}
const users = page => page.getByRole('combobox', { name: 'Usuarios que atiende 1', exact: true });
const priority = page => page.getByRole('combobox', { name: 'Prioridad 1', exact: true });
const row = page => page.locator('[data-edit-block="identificacion:inicial"]');
const option = (page, name) => page.getByRole('option', { name, exact: true });
async function finish(page) {
    await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toHaveAttribute('aria-busy', 'false');
    await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/);
    await expect(row(page)).toHaveAttribute('data-edit-state', 'idle');
}
const check = async (name, work) => { await work(); report.checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name); };
const first = await session(), page = first.page;
async function stored() {
    const response = await first.context.request.post(origin + '/__fixture/stored', { headers: { 'X-Fixture-Key': key } });
    assert.equal(response.status(), 200); return (await response.json()).contenido.identificacion[0];
}
try {
    await check('valores iniciales vacíos; menú de usuarios espera reserva confirmada', async () => {
        await expect(users(page)).toHaveText('Selecciona los usuarios');
        await expect(priority(page)).toHaveText('Selecciona una prioridad');
        await expect(users(page)).not.toHaveAttribute('aria-readonly', 'true');
        let grant;
        const gate = new Promise(resolve => { grant = resolve; });
        await page.route('**/formatos/A/reservas', async route => { await gate; await route.continue(); });
        try {
            await users(page).click();
            await expect(page.getByText('Preparando edición…', { exact: true })).toBeVisible();
            await expect(page.getByRole('listbox')).toHaveCount(0);
            await expect(page.getByLabel('Trámite / servicio 1', { exact: true })).toHaveJSProperty('readOnly', true);
            grant(); await expect(page.getByRole('listbox')).toBeVisible();
            await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        } finally { grant(); await page.unroute('**/formatos/A/reservas'); }
    });
    await check('marcar/desmarcar, scroll y autoguardado conservan menú, foco y reserva', async () => {
        const releases = requests.length;
        await expect(page.getByRole('option')).toHaveCount(5);
        await option(page, 'Docentes').click(); await expect(option(page, 'Docentes')).toHaveAttribute('aria-selected', 'true');
        await option(page, 'Estudiantes').click(); await expect(option(page, 'Estudiantes')).toHaveAttribute('aria-selected', 'true');
        await option(page, 'Docentes').click(); await expect(option(page, 'Docentes')).toHaveAttribute('aria-selected', 'false');
        await option(page, 'Docentes').click();
        await page.getByRole('listbox').hover(); await page.mouse.wheel(0, 180);
        await expect.poll(async () => (await stored()).usuario).toEqual(['Docentes', 'Estudiantes']);
        await expect(page.getByRole('listbox')).toBeVisible(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        assert.equal(requests.length, releases);
        await page.screenshot({ path: path.join(artifacts, 'usuarios-desktop.png'), fullPage: true });
        await page.keyboard.press('Escape'); await expect(users(page)).toBeFocused();
        await page.keyboard.press('Tab'); await expect(page.getByLabel('Resultado o documento 1', { exact: true })).toBeFocused();
        await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        await finish(page); await page.reload(); await header(page);
        await expect(users(page)).toHaveText('Docentes, Estudiantes');
        assert.equal((await stored()).prioridad, '');
    });
    await check('prioridad indicativa sólo en campo vacío; cinco opciones reales y persistencia', async () => {
        await priority(page).click(); await expect(page.getByRole('option')).toHaveCount(5);
        await expect(option(page, 'Selecciona una prioridad')).toHaveCount(0);
        await option(page, '4: Muy prioritario').click(); await finish(page);
        await page.reload(); await header(page); await expect(priority(page)).toHaveText('4: Muy prioritario');
        assert.deepEqual((await stored()).usuario, ['Docentes', 'Estudiantes']);
        await priority(page).click(); await expect(option(page, 'Selecciona una prioridad')).toHaveCount(0);
        await page.keyboard.press('Escape'); await finish(page);
    });
    await check('dos sesiones: ocupación bloquea menús y eliminación; liberación habilita sin recargar', async () => {
        const other = await session();
        try {
            await users(page).click(); await option(page, 'Otro').click();
            await expect(row(other.page)).toHaveAttribute('data-edit-state', 'occupied');
            await users(other.page).click(); await expect(other.page.getByRole('listbox')).toHaveCount(0);
            await priority(other.page).click(); await expect(other.page.getByRole('listbox')).toHaveCount(0);
            await expect(row(other.page).getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeDisabled();
            await page.keyboard.press('Escape'); await finish(page);
            await expect(row(other.page)).toHaveAttribute('data-edit-state', 'idle');
            await expect(users(other.page)).toHaveText('Docentes, Estudiantes, Otro');
            await users(other.page).click(); await expect(row(other.page)).toHaveAttribute('data-edit-state', 'owned');
            await option(other.page, 'Otro').click(); await other.page.keyboard.press('Escape'); await finish(other.page);
            await expect(users(page)).toHaveText('Docentes, Estudiantes');
            // El cambio confirmado y la liberación son notificaciones distintas: recibir texto no concede la fila.
            await expect(row(page)).toHaveAttribute('data-edit-state', 'idle');
        } finally { await other.context.close(); }
    });
    await check('teclado: selección múltiple sin cerrar, Escape restaura foco y vacío queda pendiente', async () => {
        await users(page).focus(); await page.keyboard.press('Enter');
        await option(page, 'Docentes').focus(); await page.keyboard.press('Space');
        await expect(option(page, 'Docentes')).toHaveAttribute('aria-selected', 'false');
        await page.keyboard.press('ArrowDown'); await expect(option(page, 'Estudiantes')).toBeFocused();
        await page.keyboard.press('Enter'); await expect(option(page, 'Estudiantes')).toHaveAttribute('aria-selected', 'false');
        await expect(page.getByRole('listbox')).toBeVisible(); await page.keyboard.press('Escape');
        await expect(users(page)).toBeFocused(); await finish(page);
        assert.deepEqual((await stored()).usuario, []); assert.equal((await stored()).prioridad, '4');
        await page.reload(); await header(page); await expect(users(page)).toHaveText('Selecciona los usuarios');
    });
    await check('móvil táctil y movimiento reducido: casillas, textos y menú sin desbordamiento', async () => {
        await page.setViewportSize({ width: 360, height: 800 }); await page.emulateMedia({ reducedMotion: 'reduce' });
        await expect(users(page)).not.toHaveAttribute('aria-readonly', 'true');
        await users(page).tap(); await option(page, 'Comunidad universitaria').tap(); await option(page, 'Personal administrativo').tap();
        await expect(page.getByRole('listbox')).toBeVisible(); await expect(row(page)).toHaveAttribute('data-edit-state', 'owned');
        const bounds = await page.getByRole('listbox').boundingBox();
        assert.ok(bounds.x >= 0 && bounds.x + bounds.width <= 360);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await page.screenshot({ path: path.join(artifacts, 'usuarios-mobile.png'), fullPage: true });
        await page.keyboard.press('Escape'); await expect(users(page)).toBeFocused(); await finish(page);
        await page.reload(); await header(page); await expect(users(page)).toHaveText('Comunidad universitaria, Personal administrativo');
        assert.equal((await stored()).prioridad, '4');
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally {
    await writeFile(path.join(artifacts, 'users-served-browser.json'), JSON.stringify(report, null, 2));
    await writeFile(path.join(artifacts, 'users-served-reservations.json'), JSON.stringify({ releases: requests.length }, null, 2));
    await browser.close();
}
