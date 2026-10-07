import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
import { verifyParticipants } from './participant-flow.mjs';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Sólo host sintético aislado.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, hasTouch: true, viewport: { width: 1366, height: 768 } });
await context.route('**/*', async route => {
    const url = new URL(route.request().url()); if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/'))
            headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const report = { synthetic: true, touchscreenEmulated: true, checks: [], errors: [] }, page = await context.newPage();
page.on('pageerror', e => report.errors.push(e.message));
try {
    await page.goto(origin + '/connect'); await page.waitForURL(origin + '/inicio');
    assert.equal((await context.request.post(origin + '/__fixture/ready-to-submit', { headers: { 'X-Fixture-Key': key } })).status(), 200);
    await page.goto(origin + '/formatos/A');
    const finish = async () => {
        const button = page.getByRole('button', { name: 'Guardar borrador', exact: true });
        await expect(button).toBeEnabled(); await button.click(); await expect(button).toHaveAttribute('aria-busy', 'false');
        await expect(page.locator('.form-actions [role=status]')).toHaveText(/^(Guardado|Sin cambios pendientes)$/);
    };
    const check = async (name, run) => { await run(); report.checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name); };
    await verifyParticipants({ page, context, origin, check, finish, artifacts, touch: true });
    assert.deepEqual(report.errors, []); report.passed = true;
} finally { await writeFile(path.join(artifacts, 'participants-browser.json'), JSON.stringify(report, null, 2)); await browser.close(); }
