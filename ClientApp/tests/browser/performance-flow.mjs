import assert from 'node:assert/strict';
import { chromium, expect as baseExpect } from '@playwright/test';
const expect = baseExpect.configure({ timeout: 60000 });
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, output = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !output) throw new Error('Sólo host sintético aislado.');
const phase = process.env.TDV2_PERFORMANCE_PHASE || 'after';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 768 } });
context.setDefaultTimeout(45000);
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            const target = new URL(headers.location);
            headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(target.searchParams.get('state'))}`;
            return route.fulfill({ response, headers });
        }
        return route.fulfill({ response });
    }
    return route.continue();
});
const page = await context.newPage(), errors = [], report = { phase, synthetic: true, rows: 800, checks: [], errors };
page.on('pageerror', e => errors.push(e.message));
try {
    assert.equal((await context.request.post(origin + '/__fixture/performance-seed', { headers: { 'X-Fixture-Key': key } })).status(), 200);
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    const start = performance.now(); await page.goto(origin + '/formatos/A');
    await expect(page.getByRole('tab', { name: 'Contexto', exact: true })).toBeVisible();
    report.mountMs = performance.now() - start;
    report.contextControls = await page.locator('input,textarea,[role=combobox]').count();
    report.mountedPanels = await page.locator('[role=tabpanel]').count();
    console.log(`Medición ${phase}: ${report.mountedPanels} paneles, ${report.contextControls} controles iniciales.`);
    await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
    const input = page.getByLabel('Trámite / servicio 1', { exact: true }); await input.focus();
    await expect(input).toHaveJSProperty('readOnly', false);
    await page.evaluate(() => {
        window.captureTimes = [];
        document.addEventListener('input', () => { const start = performance.now(); requestAnimationFrame(() => window.captureTimes.push(performance.now() - start)); }, true);
    });
    await input.pressSequentially('Medición sintética de escritura', { delay: 25 });
    await page.waitForTimeout(100);
    const times = await page.evaluate(() => window.captureTimes.sort((a, b) => a - b));
    report.inputToFrame = { samples: times.length, averageMs: times.reduce((a, b) => a + b, 0) / times.length, p95Ms: times[Math.floor(times.length * .95)] };
    await expect(input).toHaveValue('Medición sintética de escritura');
    await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Guardar borrador', exact: true })).toHaveAttribute('aria-busy', 'false');
    report.checks.push('800 filas, reserva automática, primera escritura y guardado confirmado.');
    if (phase === 'after') {
        assert.equal(report.mountedPanels, 1); assert.equal(report.contextControls, 0);
        const other = await context.newPage(); await other.goto(origin + '/formatos/A');
        await other.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        await input.focus(); await expect(input).toHaveJSProperty('readOnly', false);
        await expect(other.getByText('Estás editando este registro en otra pestaña', { exact: true })).toBeVisible();
        for (const [width, height] of [[1920,1080],[1366,768],[768,1024],[390,844]]) {
            await other.setViewportSize({ width, height });
            assert.equal(await other.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
            await other.screenshot({ path: path.join(output, `presencia-${width}.png`), fullPage: false });
        }
        const observed = other.locator('[data-edit-state=occupied]').first();
        await expect(observed).toHaveCSS('outline-width', '2px');
        await expect(observed.locator('.MuiAvatar-root')).toBeVisible();
        await other.emulateMedia({ reducedMotion: 'reduce' });
        assert.equal(await observed.evaluate(el => getComputedStyle(el).transitionDuration), '0s');
        await page.getByRole('tab', { name: 'Contexto', exact: true }).click();
        await expect(other.locator('[data-edit-state=occupied]')).toHaveCount(0);
        await expect(other.getByRole('button', { name: 'Eliminar registro', exact: true }).nth(0)).toBeEnabled();
        await other.close(); report.checks.push('Otra pestaña bloqueada con avatar; liberación automática, eliminación directa, móvil y movimiento reducido.');
        assert.equal((await context.request.post(origin + '/__fixture/configuration-admin', { headers: { 'X-Fixture-Key': key } })).status(), 200);
        await page.goto(origin + '/configuracion');
        await page.getByRole('button', { name: 'Abrir configuración procesos', exact: true }).click();
        await expect(page.getByRole('heading', { name: 'Participación de áreas', exact: true })).toBeVisible();
        for (const [width, height] of [[1920,1080],[1366,768],[768,1024],[390,844]]) {
            await page.setViewportSize({ width, height });
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
            await page.screenshot({ path: path.join(output, `configuracion-procesos-${width}.png`), fullPage: true });
        }
        await page.getByRole('combobox', { name: 'Niveles participantes', exact: true }).click();
        await page.getByRole('option', { name: 'Nivel 3', exact: true }).click(); await page.keyboard.press('Escape');
        await page.getByRole('button', { name: 'Revisar cambios', exact: true }).click();
        await expect(page.getByRole('dialog')).toContainText('Saldrán: 1');
        await expect(page.getByRole('button', { name: 'Cancelar', exact: true })).toBeFocused();
        await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
        const unchanged = await context.request.get(origin + '/configuracion/procesos', { headers: { 'X-TDV2-Page': 'true' } });
        assert.equal((await unchanged.json()).props.settings.version, 1);
        await page.getByRole('button', { name: 'Revisar cambios', exact: true }).click();
        await page.getByRole('button', { name: 'Confirmar y guardar', exact: true }).click();
        await expect(page.getByText('Participación guardada.', { exact: true })).toBeVisible();
        assert.equal((await context.request.get(origin + '/formatos/A3')).status(), 403);
        report.checks.push('Configuración adaptable, vista previa, Cancelar sin escritura y confirmación que retira acceso al formato excluido.');
    }
    assert.deepEqual(errors, []); report.passed = true;
} finally { await writeFile(path.join(output, `browser-performance-${phase}.json`), JSON.stringify(report, null, 2)); await browser.close(); }
console.log(JSON.stringify(report));
