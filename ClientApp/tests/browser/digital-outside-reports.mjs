// Proyecciones producidas por el caso HTTP/PostgreSQL «Fuera de funciones».
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';

const file = path.resolve(process.argv[2]);
const reports = JSON.parse(await readFile(file, 'utf8'));
const base = '/competencias-digitales';
const views = [
    ['avance-dependencia', 'Avance por dependencia', 'avance', 0],
    ['resultados-generales', 'Resultados generales', 'resultados', 1],
    ['mi-informe-ruta', 'Mi informe y ruta', 'informe', 3],
];
const fixture = await systemFixture();
const account = reports.informe.institution.name;
fixture.pages = Object.fromEntries(views.map(([slug, , key, index]) => {
    const url = base + '/' + slug;
    return [url, () => ({ component: 'CompetenciasDigitales', url, props: {
        auth: { user: { id: 1, name: account, email: 'persona@example.test' }, roles: [], modules: [] },
        csrfToken: 'synthetic', session: { lifetime_ms: 7200000 }, routes: { home: '/', inicio: base, logout: '/logout' },
        digital: { page: index, generation: reports.informe.generation, user: account, actor: account,
            notice: reports.informe.instrument.notice, content: reports[key],
            navigation: views.map(([s, name]) => ({ name, path: base + '/' + s })) },
    } })];
}));
fixture.handleRequest = async (_req, url, json) => {
    if (url.pathname === base + '/api/generacion') { json({ generation: reports.informe.generation }); return true; }
    if (url.pathname === '/user/photo') { json({ photo: null }); return true; }
    return false;
};
const browser = await chromium.launch({ channel: 'msedge', headless: true });
try {
    for (const width of [1440, 390]) {
        const page = await browser.newPage({ viewport: { width, height: 960 } });
        await page.route('**/*', r => new URL(r.request().url()).origin === fixture.origin ? r.continue() : r.abort());
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        for (const [slug, , key] of views) {
            await page.goto(fixture.origin + base + '/' + slug);
            await expect(page.getByText('Conocimientos digitales generales', { exact: false }).first()).toBeVisible();
            if (key === 'informe') {
                const row = width === 390 ? page.locator('.cd-tool-card').filter({ has: page.getByRole('heading', { name: 'Excel', exact: true }) })
                    : page.getByRole('row').filter({ has: page.getByRole('rowheader', { name: 'Excel', exact: true }) });
                await expect(row).toContainText('Fuera de las funciones del puesto');
                await expect(row).toContainText('Avanzado');
                await row.scrollIntoViewIfNeeded();
            } else {
                const section = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Respuestas fuera de las funciones del puesto' }) });
                await expect(section.locator('.cd-kpis strong')).toHaveText(['0', '1', '1']);
                await expect(section.locator('.cd-kpis div > span:first-child')).toHaveText(['Básico', 'Intermedio', 'Avanzado']);
                if (key === 'resultados') {
                    const table = page.getByRole('region', { name: 'Resultados por herramienta o tema' });
                    const excel = table.getByRole('row').filter({ has: page.getByRole('rowheader', { name: 'Excel', exact: true }) });
                    await expect(excel.getByRole('cell').nth(0)).toHaveText('Sin índice');
                    await expect(excel.getByRole('cell').nth(1)).toHaveText('100 %');
                    await expect(excel).toContainText('Básico: 0 · Intermedio: 0 · Avanzado: 1');
                }
                await section.scrollIntoViewIfNeeded();
            }
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
            await page.screenshot({ path: path.join(path.dirname(file), key + '-' + width + '.png') });
        }
        assert.deepEqual(errors, []);
        await page.close();
    }
    console.log('PASS tres informes en escritorio y móvil con proyecciones PostgreSQL sintéticas');
} finally {
    await browser.close();
    await fixture.close();
}
