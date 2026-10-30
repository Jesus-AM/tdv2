import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
const origin = process.env.TDV2_BROWSER_ORIGIN, key = process.env.TDV2_BROWSER_KEY, artifacts = process.env.TDV2_BROWSER_ARTIFACTS;
if (!origin?.startsWith('https://127.0.0.1:') || !key || !artifacts) throw new Error('Sólo entorno aislado.');
const mode = process.env.TDV2_DIGITAL_VITE === '1' ? 'development' : process.env.TDV2_TEST_PUBLISHED_ROOT ? 'published' : 'compiled';
const base = '/competencias-digitales', report = { mode, syntheticSources: true, realPostgreSql: true, checks: [], colors: [], errors: [], developmentHmrUnavailable: 0 };
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
await context.routeWebSocket('**/*', socket => {
    if (new URL(socket.url()).host !== new URL(origin).host) socket.close();
    else socket.connectToServer();
});
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin !== origin) return route.abort();
    if (url.pathname === '/connect') {
        const response = await route.fetch({ maxRedirects: 0 }), headers = response.headers();
        if (headers.location?.startsWith('https://login.microsoftonline.com/')) headers.location = `${origin}/connect?code=synthetic-code&state=${encodeURIComponent(new URL(headers.location).searchParams.get('state'))}`;
        return route.fulfill({ response, headers });
    }
    return route.continue();
});
const page = await context.newPage(); page.setDefaultTimeout(15000); page.on('pageerror', e => {
    // Vite configura HMR para localhost:7136; el host sintético usa un puerto efímero
    // y bloquea otros orígenes. No se prueba HMR ni se cambia la configuración del desarrollador.
    if (mode === 'development' && e.message === 'WebSocket closed without opened.' && e.stack?.includes('@vite/client')) report.developmentHmrUnavailable++;
    else report.errors.push(e.message);
});
const control = async name => { const r = await context.request.post(origin + '/__digitalfixture/' + name, { headers: { 'X-Fixture-Key': key } }); assert.equal(r.status(), 200); return r.json(); };
const survey = async () => { const r = await context.request.get(origin + base + '/api/encuesta'); assert.equal(r.status(), 200); return r.json(); };
const api = async (method, route, data) => { const csrf = await (await context.request.get(origin + '/session/csrf')).json(); const r = await context.request.fetch(origin + base + route, { method, data, headers: { 'X-CSRF-TOKEN': csrf.token, 'X-TDV2-Context': 'own' } }); assert.equal(r.status(), 200); return r.json(); };
const check = async (name, work) => { await work(); report.checks.push(name); console.log('POPULATION UI PASS ' + name); };
const shot = async (name, fullPage = true) => { await page.evaluate(async () => { await document.fonts.ready; }); await page.screenshot({ path: path.join(artifacts, `${mode}-${name}.png`), fullPage }); };
const save = async () => { const done = page.waitForResponse(r => r.url().endsWith('/api/encuesta/guardar') && r.request().method() === 'POST'); await page.getByRole('button', { name: 'Guardar avance', exact: true }).click(); assert.equal((await done).status(), 200); await expect(page.locator('.cd-save-status')).toHaveText('Avance guardado'); };
try {
    await page.goto(origin + '/connect?returnUrl=' + encodeURIComponent(base + '/avance-dependencia'));
    // Arranque frío de Vite durante otras pruebas SQL: no es un tiempo de captura/autoguardado.
    await expect(page.locator('.cd-site')).toBeVisible({ timeout: 30000 });
    await check('población 10 = 6 + 2 + 2; indicadores y tabla, filtros sin nivel 1 ni N', async () => {
        const data = await (await context.request.get(origin + base + '/api/avance?unit=A&level=2')).json();
        assert.equal(data.summary.target, 10); assert.equal(data.summary.completed, 6); assert.equal(data.summary.inProgress, 2); assert.equal(data.summary.notStarted, 2); assert.equal(data.summary.participation, 60);
        assert(!data.levels.includes(1)); assert(!data.options.some(u => ['ROOT', 'A3'].includes(u.id)));
        const level3 = await (await context.request.get(origin + base + '/api/avance?unit=A&level=3')).json();
        assert.equal(level3.summary.target, 10); assert(level3.units.some(u => u.id === 'A3'));
        const historical3 = await (await context.request.get(origin + base + '/api/resultados?unit=A&level=3')).json();
        // Seis de la población actual más el histórico sintético sin acceso vigente.
        assert.equal(historical3.summary.completed, 7);
        await page.goto(origin + base + '/avance-dependencia?unit=A&level=2');
        await expect(page.locator('.cd-participation-table')).toContainText('60 %');
        await expect(page.locator('main')).not.toContainText(/Rama|directas|Sin meta completa/);
        for (const width of [1440, 900, 390]) {
            await page.setViewportSize({ width, height: 950 }); await shot(`avance-${width}`);
            assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        }
    });
    await check('colores calculados, dos filas, logotipo blanco, cuenta y foco con cinco y dos módulos', async () => {
        for (const role of ['admin', 'participant']) {
            await control(role); await page.goto(origin + base);
            const nav = page.getByRole('navigation', { name: 'Módulos de Competencias digitales' }); await expect(nav.getByRole('button')).toHaveCount(role === 'admin' ? 5 : 2);
            for (const width of [1440, 900, 390]) {
                await page.setViewportSize({ width, height: 950 });
                const styles = await page.evaluate(() => {
                    const nav = document.querySelector('.cd-nav'), header = document.querySelector('.cd-header'), footer = document.querySelector('.cd-footer');
                    const css = e => ({ background: getComputedStyle(e).backgroundColor, image: getComputedStyle(e).backgroundImage, opacity: getComputedStyle(e).opacity });
                    return { nav: css(nav), header: css(header), footer: css(footer), font: getComputedStyle(nav).fontFamily, overflow: document.documentElement.scrollWidth > innerWidth + 1, logo: document.querySelector('.cd-header img')?.getAttribute('src') };
                });
                for (const element of [styles.nav, styles.header, styles.footer]) { assert.equal(element.background, 'rgb(33, 52, 85)'); assert.equal(element.image, 'none'); assert.equal(element.opacity, '1'); }
                assert.match(styles.font, /Noto Sans/); assert.match(styles.logo, /transformacion-digital-blanco\.svg/); assert(!styles.overflow);
                await nav.getByRole('button').first().focus(); await page.keyboard.press('Tab'); await page.keyboard.press('Shift+Tab'); await expect(nav.getByRole('button').first()).toBeFocused();
                const focus = await nav.getByRole('button').first().evaluate(e => ({ style: getComputedStyle(e).outlineStyle, color: getComputedStyle(e).color })); assert.notEqual(focus.style, 'none'); assert.equal(focus.color, 'rgb(255, 255, 255)');
                report.colors.push({ role, width, ...styles, focus }); await shot(`nav-${role}-${width}`);
                await page.getByRole('button', { name: 'Abrir menú de usuario' }).click(); await expect(page.getByRole('menu')).toContainText('Persona sintética'); await page.keyboard.press('Escape');
                const footer = page.locator('.cd-footer'); await footer.scrollIntoViewIfNeeded();
                await page.screenshot({ path: path.join(artifacts, `${mode}-footer-${role}-${width}.png`) });
            }
        }
        await control('admin');
    });
    await check('fuente no disponible y conjunto sin población no muestran cero ni cien artificial', async () => {
        await control('population-unavailable'); await page.goto(origin + base + '/avance-dependencia'); await expect(page.getByText('La información de participación no está disponible.', { exact: false })).toBeVisible(); await shot('poblacion-no-disponible');
        await control('population-available'); await page.goto(origin + base + '/avance-dependencia?unit=B'); await expect(page.getByText('No hay personas que deban responder dentro del conjunto consultado.')).toBeVisible(); await shot('sin-poblacion');
    });
    await check('Inicial masivo sólo pendientes de categoría, preserva parcial/NA; guardado coordinado y recarga', async () => {
        let s = await survey(); await api('POST', '/api/aviso', { generation: s.generation, version: s.instrument.notice.version }); s = await survey();
        const profile = Object.fromEntries(s.instrument.profile.map(f => [f.id, f.multiple ? [f.options[0]] : f.options[0]]));
        s = await api('PUT', '/api/encuesta', { version: s.version, generation: s.generation, profile, answers: s.answers });
        await page.goto(origin + base + '/encuesta'); await expect(page.locator('.cd-block h2')).toHaveText('Microsoft 365');
        const block = s.instrument.modules[0], tools = block.tools.filter(t => !t.opt && !t.doc), category = tools[0].cat, grouped = tools.filter(t => t.cat === category);
        assert(grouped.length >= 3);
        await page.locator(`#${grouped[0].id} input[type=radio][value="3"]`).check(); await page.locator(`#${grouped[1].id} input[type=radio][value="na"]`).check();
        const section = page.getByRole('region', { name: category, exact: true }); await expect(section.locator('.cd-category-help')).toHaveCount(0);
        const requests = []; page.on('request', r => { if (r.method() === 'PUT' && r.url().endsWith('/api/encuesta')) requests.push(r); });
        const before = (await control('audit-count')).saves;
        await section.getByRole('button', { name: 'Marcar pendientes como Inicial' }).click(); await expect(section.getByRole('button', { name: 'Marcar pendientes como Inicial' })).toBeDisabled();
        await expect(page.locator('.cd-save-status')).toHaveText('Avance guardado');
        s = await survey(); assert.equal(s.answers[grouped[0].id].level, 3); assert.equal(s.answers[grouped[1].id].state, 'na'); assert(grouped.slice(2).every(t => s.answers[t.id].level === 0));
        assert(tools.filter(t => t.cat !== category).every(t => !s.answers[t.id])); assert.equal((await control('audit-count')).saves, before);
        assert(requests.length <= 2); await save(); assert.equal((await control('audit-count')).saves, before + 1);
        await page.reload(); await expect(page.locator(`#${grouped[0].id} input[value="3"]`)).toBeChecked();
        s = await survey(); for (const t of tools) s.answers[t.id] ||= { state: 'level', level: 0 };
        await api('PUT', '/api/encuesta', { version: s.version, generation: s.generation, profile: s.profile, answers: s.answers });
        await page.goto(origin + base + '/encuesta'); await expect(page.locator('.cd-block h2')).toHaveText('Ciberseguridad');
        s = await survey(); const cyber = s.instrument.modules[1].tools.filter(t => !t.opt && !t.doc), cat = cyber.find(t => cyber.filter(x => x.cat === t.cat).length >= 3).cat, group = cyber.filter(t => t.cat === cat);
        await page.locator('#' + group[0].id).getByRole('checkbox').first().check();
        await page.locator('#' + group[1].id).getByRole('button', { name: 'No aplica', exact: true }).click();
        const section2 = page.getByRole('region', { name: cat, exact: true }); await section2.getByRole('button', { name: 'Marcar pendientes como Inicial' }).click();
        await expect(page.locator('.cd-save-status')).toHaveText('Avance guardado'); s = await survey();
        assert.equal(s.answers[group[0].id].checks, '100000000000'); assert.equal(s.answers[group[1].id].state, 'na'); assert(group.slice(2).every(t => s.answers[t.id].checks === '000000000000'));
        assert(cyber.filter(t => t.cat !== cat).every(t => !s.answers[t.id]));
        await page.reload(); await expect(page.locator('#' + group[0].id).getByRole('checkbox').first()).toBeChecked();
        for (const width of [1440, 900, 390]) {
            await page.setViewportSize({ width, height: 950 }); await page.evaluate(() => scrollTo({ top: 0, behavior: 'instant' })); await shot(`instrucciones-${width}`, false);
            await section2.locator('.cd-category-row').evaluate(e => scrollTo({ top: scrollY + e.getBoundingClientRect().top - document.querySelector('.cd-header').getBoundingClientRect().height - document.querySelector('.cd-progress').getBoundingClientRect().height - 12, behavior: 'instant' }));
            await shot(`categoria-${width}`, false); assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        }
        await page.locator('#' + group[2].id).getByRole('checkbox').first().check(); await save(); assert.equal((await survey()).answers[group[2].id].checks, '100000000000');
    });
    await check('responsable N3 conserva contexto acumulativo sin ofrecerlo como filtro y vuelve al total', async () => {
        await control('population-n3'); await page.goto(origin + base + '/avance-dependencia');
        await expect(page.getByRole('combobox', { name: 'Nivel de UR' })).toHaveCount(0);
        await expect(page.getByRole('combobox', { name: 'Contexto de responsabilidad' })).toHaveCount(0);
        const area = page.getByRole('combobox', { name: 'Unidad dependiente' }); await area.click();
        await expect(page.getByRole('option')).toHaveCount(1); await expect(page.getByRole('option')).toContainText('110 - Área sintética A3');
        await page.getByRole('option').click(); await page.getByRole('button', { name: 'Aplicar filtros' }).click();
        await page.waitForURL(url => url.searchParams.get('unit') === 'A4'); await expect(area).toHaveValue('111 - Área sintética A4');
        await area.press('Escape'); await page.getByRole('button', { name: /Clear|Limpiar|Borrar/i }).click();
        await page.getByRole('button', { name: 'Aplicar filtros' }).click(); await page.waitForURL(url => !url.searchParams.has('unit'));
        await expect(page.locator('.cd-kpis .cd-card').first()).toContainText('10'); await shot('responsable-n3', false);
    });
    await check('Configuración conserva padre; reapertura individual, informe histórico y filtros adaptables', async () => {
        await control('admin'); let s = await survey();
        for (const block of s.instrument.modules) {
            for (const t of block.tools.filter(t => !t.opt && !t.doc)) s.answers[t.id] ||= ['ciber', 'datos', 'sii'].includes(block.id) ? { state: 'level', checks: '000000000000' } : { state: 'level', level: 0 };
            s = await api('PUT', '/api/encuesta', { generation: s.generation, version: s.version, profile: s.profile, answers: s.answers });
        }
        s = await api('POST', '/api/encuesta/finalizar', { generation: s.generation, version: s.version });
        assert(s.completedAt);
        const before = await (await context.request.get(origin + base + '/api/avance')).json();
        await page.goto(origin + base + '/configuracion'); await expect(page.locator('.cd-nav button')).toHaveCount(5);
        await expect(page.locator('.cd-config-nav button')).toHaveCount(2);
        await page.getByRole('button', { name: 'Gestión de encuestas', exact: true }).click();
        await page.waitForURL('**/configuracion/encuestas'); await expect(page.locator('.cd-nav [aria-current=page]')).toHaveText('Configuración');
        await page.getByRole('textbox', { name: 'Buscar encuesta por nombre o correo' }).fill('persona@uacj.mx');
        await page.getByRole('button', { name: 'Buscar encuesta', exact: true }).click();
        await expect(page.locator('.cd-survey-people article')).toHaveCount(1);
        for (const width of [1440, 900, 390]) {
            await page.setViewportSize({ width, height: 950 }); await shot(`gestion-${width}`); assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        }
        await page.getByRole('button', { name: 'Reabrir encuesta', exact: true }).click(); const dialog = page.getByRole('dialog');
        await expect(dialog).toContainText('Se conservarán todas las respuestas'); await shot('confirmar-reapertura-390', false);
        await dialog.getByRole('button', { name: 'Reabrir encuesta', exact: true }).focus(); await page.keyboard.press('Enter'); await expect(dialog).toContainText('Encuesta reabierta');
        await dialog.getByRole('button', { name: 'Cerrar', exact: true }).click();
        const after = await (await context.request.get(origin + base + '/api/avance')).json();
        assert.equal(after.summary.target, before.summary.target); assert.equal(after.summary.completed, before.summary.completed - 1); assert.equal(after.summary.inProgress, before.summary.inProgress + 1);
        await page.goto(origin + base + '/mi-informe-ruta'); await expect(page.getByText('Encuesta reabierta ·', { exact: false })).toBeVisible();
        for (const width of [1440, 900, 390]) { await page.setViewportSize({ width, height: 950 }); await shot(`informe-reabierto-${width}`, false); }
        await page.getByRole('button', { name: /^Envío 1 ·/ }).click(); await expect(page.getByText('Informe histórico del envío', { exact: false })).toBeVisible(); await shot('informe-historico-390', false);
        await page.getByRole('button', { name: 'Ver estado actual', exact: true }).click();
        await page.goto(origin + base + '/configuracion/actuar-como-usuario'); await expect(page.getByRole('textbox', { name: 'Buscar persona por nombre o correo' })).toBeVisible(); await shot('representacion-390', false);
        for (const reportPath of ['avance-dependencia', 'resultados-generales']) {
            await page.goto(origin + base + '/' + reportPath); const level = page.getByRole('combobox', { name: 'Nivel de UR', exact: true }); await expect(level).toHaveText('2');
            await level.click(); await expect(page.getByRole('option')).toHaveText(['2', '3', '4', '5']); await page.keyboard.press('Escape');
            await page.goto(origin + base + '/' + reportPath + '?level=3'); await expect(level).toHaveText('3');
            await page.getByRole('button', { name: 'Restablecer filtros' }).click(); await expect(level).toHaveText('2');
            for (const width of [1440, 900, 390]) { await page.setViewportSize({ width, height: 950 }); await shot(`${reportPath}-filtros-${width}`, false); if (width > 760) assert.equal(Math.round((await page.locator('.cd-level-filter').boundingBox()).width), 150); assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)); }
        }
    });
    assert.deepEqual(report.errors, []);
} catch (error) { report.failure = error.message; await page.screenshot({ path: path.join(artifacts, 'population-failure.png') }).catch(() => {}); throw error; }
finally { await writeFile(path.join(artifacts, `digital-population-${mode}.json`), JSON.stringify(report, null, 2)); await browser.close(); }
