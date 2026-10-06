import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Requires isolated native host.');
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) {
            headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
        }
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const page = await context.newPage(), errors = [], checks = [];
page.on('pageerror', error => errors.push(error.message));
async function check(name, work) { await work(); checks.push({ name, passed: true }); console.log('BROWSER PASS ' + name); }
try {
    assert.equal((await context.request.post(origin + '/__fixture/scope-level3', { headers: { 'X-Fixture-Key': key } })).status(), 200);
    await page.goto(origin + '/connect');
    await page.getByRole('heading', { name: 'Procesos operativos', exact: true }).waitFor();
    await check('Nuevo rol: tipo N excluido, tipo 0 elegible en niveles 2 y 3; Mis áreas y Todas las áreas conservan alcance', async () => {
        await expect(page.getByRole('button', { name: 'Configuración', exact: true })).toHaveCount(0);
        await expect(page.getByText('Nodo auxiliar excluido')).toHaveCount(0);
        await page.getByRole('tab', { name: 'Mis áreas', exact: true }).click();
        await page.getByRole('button', { name: /100.*Área sintética A/ }).click();
        await expect(page.getByRole('button', { name: 'Abrir formato de Área sintética A3', exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: 'Abrir formato de Área sintética A', exact: true })).toHaveCount(0);
        await page.getByRole('tab', { name: 'Todas las áreas', exact: true }).click();
        await expect(page.getByText('3', { exact: true })).toBeVisible();
        await page.getByRole('button', { name: /100.*Área sintética A/ }).click();
        await expect(page.getByRole('button', { name: 'Abrir formato de Área sintética A', exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: 'Abrir formato de Área sintética A3', exact: true })).toBeVisible();
    });
    await check('Claves: busca 06000/6000, muestra 6000 y mantiene la ruta A3', async () => {
        const search = page.getByLabel('Buscar área o clave');
        for (const code of ['06000', '6000']) {
            await search.fill(code);
            await expect(page.getByRole('button', { name: 'Abrir formato de Área sintética A3', exact: true })).toBeVisible();
        }
        await expect(page.getByText('6000 · Sin iniciar', { exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'Abrir formato de Área sintética A3', exact: true }).click();
        await page.waitForURL(origin + '/formatos/A3');
        await expect(page.getByText('6000 · Área sintética A3', { exact: true })).toBeVisible();
    });
    await check('Nivel 3: botón visible, sólo opción local y alta sintética en su formato', async () => {
        await page.goto(origin + '/inicio');
        await page.getByRole('button', { name: 'Colaboradores', exact: true }).click();
        await expect(page.getByRole('radio', { name: /Colaborador de áreas dependientes/ })).toHaveCount(0);
        await expect(page.getByText(/únicamente el formato de esta área de nivel 3/)).toBeVisible();
        await page.getByLabel('Buscar persona', { exact: true }).fill('colaboradora');
        await page.getByRole('button', { name: /Colaboradora sintética.*colaboradora@uacj/ }).click();
        const added = page.waitForResponse(r => new URL(r.url()).pathname === '/colaboradores' && r.request().method() === 'POST');
        await page.getByRole('button', { name: 'Agregar colaborador', exact: true }).click();
        assert.equal((await added).status(), 201);
        await expect(page.getByRole('button', { name: 'Retirar a Colaboradora sintética' })).toBeVisible();
        await page.getByRole('button', { name: 'Retirar a Colaboradora sintética' }).click();
        await page.getByRole('dialog').getByRole('button', { name: 'Retirar colaboración', exact: true }).click();
        await expect(page.getByRole('dialog')).toHaveCount(0);
    });
    await check('Móvil y movimiento reducido: expansión, foco y menús sin desbordamiento', async () => {
        await page.setViewportSize({ width: 390, height: 844 });
        await page.emulateMedia({ reducedMotion: 'reduce' });
        await page.goto(origin + '/inicio');
        const trigger = page.getByRole('button', { name: /100.*Área sintética A/ });
        await trigger.click();
        await expect(trigger).toBeFocused();
        const motion = await trigger.evaluate(el => getComputedStyle(el).transitionDuration);
        assert.equal(motion, '0s');
        await expect(page.getByRole('button', { name: 'Abrir formato de Área sintética A3', exact: true })).toBeVisible();
        await page.getByLabel('Mostrar', { exact: true }).click();
        await page.getByRole('option', { name: 'Puedo llenar', exact: true }).click();
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
        await page.screenshot({ path: path.join(artifacts, 'consulta-institucional-movil.png'), fullPage: true });
    });
    assert.deepEqual(errors, []);
} catch (error) {
    console.error(error.stack); process.exitCode = 1;
    await page.screenshot({ path: path.join(artifacts, 'scope-failure.png') }).catch(() => {});
} finally {
    await writeFile(path.join(artifacts, 'browser-scope.json'), JSON.stringify({ checks, errors, failed: !!process.exitCode }, null, 2));
    await browser.close();
}
