import assert from 'node:assert/strict';
import { chromium, expect as baseExpect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const expect = baseExpect.configure({ timeout: 30000 });
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, output = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !output) throw new Error('Sólo host sintético aislado.');
const phase = process.env.TDV2_CAPTURE_PHASE || 'after';
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 768 } });
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            const target = new URL(headers.location);
            headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(target.searchParams.get('state'))}`;
        }
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const control = (action, method = 'POST') => context.request.fetch(origin + '/__fixture/' + action, { method, headers: { 'X-Fixture-Key': key } });
const report = { synthetic: true, phase, instrumented: true, cases: [], errors: [] };
try {
    const login = await context.newPage(); await login.goto(origin + '/connect'); await login.waitForURL(origin + '/inicio'); await login.close();
    for (const count of [20, 100, 200]) {
        assert.equal((await control(`performance-seed?rows=${count}&single=true`)).status(), 200);
        const page = await context.newPage(), requests = [];
        page.on('pageerror', e => report.errors.push(e.message));
        page.on('request', r => { const u = new URL(r.url()); if (u.origin === origin && (/^\/(formatos|user|form-events)/).test(u.pathname)) requests.push({ method: r.method(), path: u.pathname }); });
        await control('capture-metrics');
        const started = performance.now(); await page.goto(origin + '/formatos/A');
        await expect(page.getByRole('tab', { name: 'Contexto', exact: true })).toBeVisible();
        const mountMs = performance.now() - started, panels = await page.getByRole('tabpanel').count();
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click();
        const field = page.getByLabel('Trámite / servicio 1', { exact: true }); await field.focus();
        await expect(field).toHaveJSProperty('readOnly', false);
        await page.waitForTimeout(150); // Terminar confirmaciones de reserva antes de medir las teclas.
        assert.ok(await page.evaluate(() => window.__captureRenders));
        await page.evaluate(() => {
            window.__captureRenders = { editor: 0, rows: {} }; window.__captureFrames = [];
            document.addEventListener('input', () => { const at = performance.now(); requestAnimationFrame(() => window.__captureFrames.push(performance.now() - at)); }, true);
        });
        const beforeRequests = requests.length;
        const text = 'Texto sintético para medir respuesta, foco y autoguardado.';
        await field.pressSequentially(text, { delay: 25 });
        await expect(field).toHaveValue(text); await expect(field).toBeFocused();
        await page.waitForTimeout(80);
        const typing = await page.evaluate(() => ({ renders: window.__captureRenders, frames: window.__captureFrames }));
        const typingRequests = requests.slice(beforeRequests);
        const saved = page.waitForResponse(r => r.request().method() === 'PATCH' && new URL(r.url()).pathname.endsWith('/bloques'));
        await saved; await expect(field).toHaveValue(text);
        const efCommands = (await (await control('capture-metrics', 'GET')).json()).efCommands;
        const frames = typing.frames.slice().sort((a, b) => a - b);
        report.cases.push({ records: count, mountMs, panels, keystrokes: text.length, inputToFrame: { averageMs: frames.reduce((a,b)=>a+b,0)/frames.length, p95Ms: frames[Math.floor(frames.length*.95)], samples: frames.length },
            renders: typing.renders, typingRequests, requests: requests.slice(), efCommands });
        assert.equal(panels, 1); assert.equal(typingRequests.filter(r => r.method === 'PATCH').length, 0);
        if (phase !== 'before') assert.equal(Object.entries(typing.renders.rows).filter(([id, n]) => id !== 'identificacion:synthetic-0' && n > 0).length, 0);
        await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
        await page.getByRole('tab', { name: 'Identificación general', exact: true }).click(); await expect(field).toHaveValue(text);
        await page.goto(origin + '/inicio'); await page.close();
        console.log(`MATRIX ${phase}: ${count} registros, ${frames.length} muestras, ${typing.renders.editor} renders de Editor, ${efCommands} comandos EF.`);
    }
    assert.deepEqual(report.errors, []); report.passed = true;
} finally { await writeFile(path.join(output, `capture-matrix-${phase}.json`), JSON.stringify(report, null, 2)); await browser.close(); }
