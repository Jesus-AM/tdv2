// Razor exportado por TDV2.Verification; recursos públicos cacheados para comparar ambos pases.
// Sin autenticación, servicios externos de negocio, PostgreSQL ni migraciones.
import assert from 'node:assert/strict';
import { chromium } from '@playwright/test';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';
import { renderedContrast } from './rendered-contrast.mjs';
const phase = process.argv[2] || 'after';
assert.ok(['before', 'after'].includes(phase));
// La revisión de jerarquía autoriza cambios dentro del hero; el resto conserva su diseño.
const heroHierarchy = process.argv.includes('--hero-hierarchy');
const logoAlignment = process.argv.includes('--logo-alignment');
const surfaceFinish = process.argv.includes('--surface-finish');
assert.ok(!(heroHierarchy && logoAlignment), 'Comparar cada revisión con su referencia correspondiente');
const selectiveLayout = heroHierarchy || logoAlignment;
const output = path.resolve(process.env.TDV2_VISUAL_ARTIFACTS || '../.artifacts/visual-refresh');
await mkdir(path.join(output, 'resources'), { recursive: true });
let html = await readFile(path.join(output, `public-${phase}.html`), 'utf8');
if (phase === 'after') {
    // Primarios y un único complementario reservado al estado hover.
    const palette = new Set(surfaceFinish ? ['#213455', '#FFFFFF', '#DADADA', '#CFD6E8'] : ['#213455', '#FFFFFF', '#DADADA', '#174187']);
    for (const color of html.match(/#[\da-f]{3,8}\b/gi) || []) assert.ok(palette.has(color.toUpperCase()), `Color fuera de paleta: ${color}`);
}
if (surfaceFinish && phase === 'after') {
    const before = await readFile(path.join(output, 'public-before.html'), 'utf8');
    assert.equal(html.slice(html.indexOf('</style>')), before.slice(before.indexOf('</style>')), 'Contenido, marcado, enlaces y preguntas intactos');
}
const fixture = await systemFixture();
fixture.handleRequest = async (req, url, json) => {
    if (url.pathname !== '/') return false;
    // El servidor del fixture espera JSON; el HTML se entrega directamente desde Playwright.
    json({}); return true;
};
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ reducedMotion: 'reduce' });
const report = { phase, browser: browser.version(), resources: [], views: [], errors: [], limitations: [], states: [], palette: [] };
await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.origin === fixture.origin) {
        if (url.pathname === '/') return route.fulfill({ contentType: 'text/html; charset=utf-8', body: html });
        return route.continue();
    }
    if (!['fonts.googleapis.com', 'fonts.gstatic.com', 'images.unsplash.com'].includes(url.hostname)) return route.abort();
    const cache = path.join(output, 'resources', createHash('sha256').update(url.href).digest('hex'));
    try {
        let resource;
        try { resource = JSON.parse(await readFile(cache, 'utf8')); }
        catch {
            assert.equal(phase, 'before', 'El pase posterior usa los mismos recursos');
            try {
                const response = await route.fetch({ timeout: 15000 });
                resource = { status: response.status(), headers: response.headers(), body: (await response.body()).toString('base64') };
            } catch { resource = { blocked: true }; }
            await writeFile(cache, JSON.stringify(resource));
        }
        if (resource.blocked) { report.limitations.push(`${url.hostname}: recurso externo inaccesible en ambos pases`); return route.abort(); }
        report.resources.push({ url: url.hostname, status: resource.status });
        return route.fulfill({ status: resource.status, headers: resource.headers, body: Buffer.from(resource.body, 'base64') });
    } catch (error) { report.errors.push(String(error)); return route.abort(); }
});
const page = await context.newPage();
page.on('pageerror', error => report.errors.push(error.message));
try {
    const viewports = selectiveLayout
        ? [[1920, 1080], [1366, 900], [1024, 768], [768, 1024], [390, 844], [320, 800]]
        : surfaceFinish ? [[1366, 900], [768, 1024], [390, 844], [320, 800]] : [[1366, 900], [390, 844], [320, 800]];
    for (const [width, height] of viewports) {
        await page.setViewportSize({ width, height });
        await page.goto(fixture.origin, { waitUntil: 'networkidle' });
        await page.evaluate(() => document.fonts.ready);
        await page.mouse.move(0, 0);
        await page.screenshot({ path: path.join(output, `public-${phase}-${width}-viewport.png`), animations: 'disabled' });
        await page.screenshot({ path: path.join(output, `public-${phase}-${width}.png`), fullPage: true, animations: 'disabled' });
        if (selectiveLayout) {
            await page.locator('.hero').screenshot({ path: path.join(output, `hero-${phase}-${width}.png`), animations: 'disabled' });
            await page.evaluate(() => scrollTo({ top: 0, behavior: 'instant' }));
        }
        const layout = await page.evaluate(() => [...document.querySelectorAll('body *')].map(el => {
            const r = el.getBoundingClientRect(), s = getComputedStyle(el);
            return { tag: el.tagName, text: el.children.length ? null : el.textContent, src: el.getAttribute('src'), href: el.getAttribute('href'),
                inHero: !!el.closest('.hero'),
                rect: [r.x, r.y, r.width, r.height], font: [s.fontFamily, s.fontSize, s.fontWeight, s.lineHeight, s.letterSpacing],
                spacing: [s.padding, s.margin, s.gap], shape: [s.borderRadius, s.borderWidth], display: [s.display, s.position, s.gridTemplateColumns],
                motion: [s.transition, s.animation] };
        }));
        const layoutFile = path.join(output, `public-${phase}-${width}-layout.json`);
        await writeFile(layoutFile, JSON.stringify(layout, null, 2));
        if (phase === 'after') {
            const styles = await page.evaluate(() => {
                const hero = [...document.querySelectorAll('.hero h1, .hero .lema, .hero .lead, .hero .direccion-general')].map(el => {
                    const chain = []; for (let node = el; node; node = node.parentElement) chain.push(getComputedStyle(node));
                    return { text: el.textContent.trim(), color: getComputedStyle(el).color,
                        opacity: chain.reduce((opacity, s) => opacity * Number(s.opacity), 1),
                        filters: chain.filter(s => s.filter !== 'none' || s.mixBlendMode !== 'normal').length };
                });
                const projectBackgrounds = []; for (let node = document.querySelector('#que-es'); node; node = node.parentElement) projectBackgrounds.push(getComputedStyle(node).backgroundColor);
                const complementary = [...document.querySelectorAll('body *')].filter(el => getComputedStyle(el).color === 'rgb(23, 65, 135)');
                return { hero, projectBackgrounds, navigation: getComputedStyle(document.querySelector('.topbar')).backgroundColor,
                    normalComplementaryText: complementary.length, body: getComputedStyle(document.body).backgroundColor };
            });
            assert.equal(styles.hero.length, 4);
            for (const text of styles.hero) {
                assert.equal(text.color, 'rgb(255, 255, 255)', text.text); assert.equal(text.opacity, 1); assert.equal(text.filters, 0);
            }
            assert.equal(styles.navigation, 'rgb(255, 255, 255)'); assert.equal(styles.body, 'rgb(255, 255, 255)');
            assert.ok(styles.projectBackgrounds.every(c => c === 'rgb(255, 255, 255)' || c === 'rgba(0, 0, 0, 0)'));
            assert.equal(styles.normalComplementaryText, 0); report.palette.push({ width, ...styles });
            const contrast = await renderedContrast(page);
            await writeFile(path.join(output, `public-contrast-${width}.json`), JSON.stringify(contrast, null, 2));
            assert.deepEqual(contrast.filter(c => c.ratio < c.required), [], 'Contraste de textos visibles');
        }
        const hero = await page.evaluate(() => {
            const metrics = selector => {
                const el = document.querySelector(selector), r = el.getBoundingClientRect(), s = getComputedStyle(el);
                return { x: r.x, y: r.y, width: r.width, height: r.height, bottom: r.bottom, fontSize: parseFloat(s.fontSize), fontWeight: s.fontWeight, fontFamily: s.fontFamily,
                    color: s.color, background: s.background, opacity: s.opacity, textAlign: s.textAlign };
            };
            const logo = document.querySelector('.uacj-logo');
            const canvas = document.createElement('canvas'); canvas.width = logo.naturalWidth; canvas.height = logo.naturalHeight;
            const context = canvas.getContext('2d'); context.drawImage(logo, 0, 0);
            const pixels = context.getImageData(0, 0, canvas.width, canvas.height).data;
            let left = canvas.width, right = 0;
            for (let y = 0; y < canvas.height; y++) for (let x = 0; x < canvas.width; x++) if (pixels[(y * canvas.width + x) * 4 + 3] > 0) { left = Math.min(left, x); right = Math.max(right, x); }
            const logoRect = logo.getBoundingClientRect();
            return { section: metrics('.hero'), identity: metrics('.identidad-institucional'), logo: metrics('.uacj-logo'), direction: metrics('.direccion-general'),
                title: metrics('.hero h1'), subtitle: metrics('.hero .lema'), paragraph: metrics('.hero .lead'),
                visibleLogo: { sourceLeft: left, sourceRight: right, left: logoRect.left + left * logoRect.width / canvas.width, right: logoRect.left + (right + 1) * logoRect.width / canvas.width },
                image: { src: logo.getAttribute('src'), alt: logo.alt, naturalWidth: logo.naturalWidth, naturalHeight: logo.naturalHeight }, h1Count: document.querySelectorAll('h1').length };
        });
        if (phase === 'after') {
            const before = JSON.parse(await readFile(path.join(output, `public-before-${width}-layout.json`), 'utf8'));
            if (logoAlignment) {
                const previous = JSON.parse(await readFile(path.join(output, 'public-before-report.json'), 'utf8')).views.find(v => v.width === width).hero;
                assert.equal(layout.length, before.length);
                const expected = structuredClone(before);
                const logoIndex = expected.findIndex(el => el.src === hero.image.src);
                expected[logoIndex].rect[0] = layout[logoIndex].rect[0];
                assert.deepEqual(layout, expected, 'Sólo cambia la coordenada horizontal del logotipo');
                assert.deepEqual(hero.image, previous.image);
                assert.equal(hero.logo.width, previous.logo.width); assert.equal(hero.logo.height, previous.logo.height);
                assert.equal(hero.section.background, previous.section.background);
                assert.ok(previous.visibleLogo.left - previous.direction.x > 5, 'Espacio transparente comprobado antes');
                for (const text of [hero.direction, hero.title, hero.subtitle, hero.paragraph]) assert.ok(Math.abs(hero.visibleLogo.left - text.x) < 1, 'Borde visible alineado');
                assert.ok(hero.visibleLogo.left >= 0 && hero.visibleLogo.right <= width, 'Marca completa dentro del viewport');
            }
            else if (surfaceFinish) {
                assert.equal(layout.length, before.length);
                for (let i = 0; i < layout.length; i++) {
                    // Se retiran elevaciones/animaciones de hover, sin cambiar la geometría ni la tipografía.
                    assert.deepEqual({ ...layout[i], motion: [] }, { ...before[i], motion: [] }, `Contenido y geometría intactos ${width}px / ${i}`);
                }
            }
            else if (!heroHierarchy) assert.deepEqual(layout, before, `Diseño intacto a ${width}px`);
            else {
                const previous = JSON.parse(await readFile(path.join(output, 'public-before-report.json'), 'utf8')).views.find(v => v.width === width).hero;
                assert.equal(layout.length, before.length, 'Sin elementos añadidos o retirados');
                const shift = hero.section.height - previous.section.height;
                for (let i = 0; i < layout.length; i++) {
                    const current = layout[i], original = before[i];
                    // Texto, imágenes, enlaces, familias, formas y animaciones se conservan también dentro del bloque.
                    for (const key of ['tag', 'text', 'src', 'href', 'inHero', 'shape', 'motion']) assert.deepEqual(current[key], original[key], `${key} del elemento ${i}`);
                    assert.equal(current.font[0], original.font[0], `Familia tipográfica ${i}`);
                    if (current.inHero) continue;
                    const expected = structuredClone(original);
                    if (expected.rect[1] >= previous.section.bottom - .1) expected.rect[1] += shift;
                    // Sólo se permite el desplazamiento vertical causado por la nueva altura del bloque.
                    for (let j = 0; j < 4; j++) assert.ok(Math.abs(current.rect[j] - expected.rect[j]) < .1, `Geometría exterior ${i}/${j} a ${width}px`);
                    assert.deepEqual({ ...current, rect: [] }, { ...expected, rect: [] }, `Estilos exteriores ${i}`);
                }
                assert.deepEqual(hero.image, previous.image, 'Archivo, composición y texto alternativo originales');
                assert.ok(hero.image.naturalWidth > 0, 'Logotipo cargado');
                assert.ok(Math.abs(hero.logo.width / hero.logo.height - hero.image.naturalWidth / hero.image.naturalHeight) < .01, 'Proporciones del logotipo');
                assert.equal(hero.h1Count, 1, 'Conserva el único h1');
                assert.deepEqual(hero.section.background, previous.section.background, 'Fotografía y velo intactos');
                assert.ok(hero.title.fontSize < previous.title.fontSize, 'El proyecto deja de dominar');
                assert.ok(hero.direction.fontSize < hero.title.fontSize && Number(hero.direction.fontWeight) <= 600, 'Dependencia secundaria');
                const institutionGap = hero.direction.y - hero.logo.bottom;
                const projectGap = hero.title.y - hero.direction.bottom;
                assert.ok(institutionGap > 0 && projectGap > institutionGap * 1.5, 'Agrupación institución–dependencia');
                for (const element of [hero.logo, hero.direction, hero.title, hero.subtitle, hero.paragraph]) {
                    assert.equal(element.x, hero.logo.x, 'Alineación izquierda');
                    assert.ok(element.x >= 0 && element.x + element.width <= width && element.bottom <= hero.section.bottom, 'Sin recortes/desbordamientos');
                }
                assert.ok(shift <= 12, 'Sin incremento innecesario de altura');
                if (width >= 1024) {
                    assert.ok(hero.logo.width >= 520 && hero.logo.width <= 600 && hero.logo.width > previous.logo.width);
                    assert.ok(hero.title.fontSize >= 36 && hero.title.fontSize <= 40);
                }
            }
        }
        const pageWidth = await page.evaluate(() => document.documentElement.scrollWidth);
        if (!selectiveLayout) assert.equal(pageWidth > width, false);
        else if (phase === 'after') {
            const previous = JSON.parse(await readFile(path.join(output, 'public-before-report.json'), 'utf8')).views.find(v => v.width === width);
            assert.equal(pageWidth, previous.pageWidth, 'La revisión del bloque no introduce desbordamientos');
        }
        report.views.push({ width, height, elements: layout.length, identicalLayout: phase === 'after' && !selectiveLayout, onlyLogoShift: phase === 'after' && logoAlignment,
            unchangedOutsideHero: phase === 'after' && heroHierarchy, pageWidth, hero, fonts: await page.evaluate(() => [...document.fonts].map(f => ({ family: f.family, weight: f.weight, status: f.status }))) });
        if (surfaceFinish) {
            // Mismos encuadres documentales antes y después, independientes del margen de ancla.
            for (const selector of ['.hero', '#que-es', '#modelo', '#ejes', '#ruta', '#diagnostico', '#participa', '#beneficios', 'aside.side', '#preguntas']) {
                const target = page.locator(selector);
                await page.evaluate(() => scrollTo({ top: 0, behavior: 'instant' }));
                await target.evaluate(el => scrollTo({ top: el.getBoundingClientRect().top + scrollY - document.querySelector('.topbar').getBoundingClientRect().height - 18, behavior: 'instant' }));
                await page.screenshot({ path: path.join(output, `${phase}-${selector.replace(/[.#]/g, '')}-${width}.png`), animations: 'disabled' });
            }
            if (phase === 'after') await verifyFinish(page, report, width, output);
        }
        await page.getByText('¿Cómo me beneficia?', { exact: true }).click();
        assert.equal(await page.locator('details').nth(1).getAttribute('open'), '');
        if (surfaceFinish) await page.screenshot({ path: path.join(output, `${phase}-faq-multiple-${width}.png`), animations: 'disabled' });
        await page.keyboard.press('Tab');
        await page.locator('.brand').focus();
        assert.equal(await page.locator('.brand').evaluate(el => getComputedStyle(el).outlineStyle), 'solid');
        if (phase === 'after') {
            for (const selector of ['.topbar .btn-dark', '.nav a', '.contact-email', '.doclist a', '.faq summary']) {
                const target = page.locator(selector).first();
                if (!await target.isVisible()) continue;
                await target.hover();
                for (const state of ['hover', 'active', 'focus']) {
                    if (state === 'active') await page.mouse.down();
                    if (state === 'focus') {
                        await page.mouse.move(1, 1); await page.mouse.up(); await page.keyboard.press('Tab'); await target.focus();
                    }
                    const contrast = await renderedContrast(page, selector);
                    assert.ok(contrast.every(c => c.ratio >= c.required), `${selector} ${state}`);
                    report.states.push({ width, selector, state, minimum: Math.min(...contrast.map(c => c.ratio)) });
                }
            }
            report.states.push({ width, disabledControls: await page.locator(':disabled').count(), note: 'La portada no contiene controles deshabilitados ni un selector de estado propio' });
        }
    }
    if (surfaceFinish && phase === 'after') {
        html = await readFile(path.join(output, 'public-authenticated.html'), 'utf8');
        for (const [width, height] of [[1366, 900], [768, 1024], [390, 844], [320, 800]]) {
            await page.setViewportSize({ width, height });
            await page.goto('about:blank'); // Cambia de documento; una navegación al mismo hash no vuelve a pedir HTML.
            await page.goto(fixture.origin + '/#ejes', { waitUntil: 'networkidle' });
            assert.equal(await page.getByRole('link', { name: /Ir a mi espacio/ }).getAttribute('href'), '/inicio');
            assert.equal(await page.locator('.session-actions form').getAttribute('action'), '/logout');
            assert.equal(await page.locator('.session-actions form').getAttribute('method'), 'POST');
            assert.ok(await page.getByRole('button', { name: 'Cerrar sesión', exact: true }).isVisible());
            const metrics = await page.evaluate(() => ({ header: document.querySelector('.topbar').getBoundingClientRect().bottom,
                title: document.querySelector('#ejes h2').getBoundingClientRect().top, scrollWidth: document.documentElement.scrollWidth }));
            assert.ok(metrics.title >= metrics.header); assert.equal(metrics.scrollWidth, width);
            report.states.push({ width, authenticated: 'sintético', ...metrics });
            await page.screenshot({ path: path.join(output, `after-authenticated-ejes-${width}.png`) });
        }
    }
    assert.equal(report.errors.length, 0, report.errors.join('\n'));
    report.completed = true;
} finally {
    await writeFile(path.join(output, `public-${phase}-report.json`), JSON.stringify(report, null, 2));
    await context.close(); await browser.close(); await fixture.close();
}
console.log(JSON.stringify({ completed: report.completed, phase, views: report.views.map(v => v.width) }));

async function verifyFinish(page, report, width, output) {
    const blue = 'rgb(33, 52, 85)', white = 'rgb(255, 255, 255)', soft = 'rgb(207, 214, 232)';
    const color = (selector, property) => page.locator(selector).first().evaluate((el, key) => getComputedStyle(el)[key], property);
    assert.equal(await color('.m-cat', 'backgroundColor'), blue);
    assert.equal(await color('.m-cat', 'color'), white);
    assert.equal(await color('.m-card', 'backgroundColor'), white);
    assert.equal(await page.locator('#ejes .ico').count(), 6);
    for (const icon of await page.locator('#ejes .ico').all()) {
        assert.deepEqual(await icon.evaluate(el => [getComputedStyle(el).backgroundColor, getComputedStyle(el).color]), [blue, white]);
    }
    assert.equal(await page.locator('.step').count(), 8);
    assert.equal(await color('.steps', 'borderLeftColor'), blue);
    assert.equal(await color('footer', 'backgroundColor'), blue);
    for (const selector of ['.m-card', '#ejes .card']) {
        const card = page.locator(selector).first(); await card.hover();
        assert.equal(await card.evaluate(el => getComputedStyle(el).transform), 'none', 'Tarjeta informativa sin elevación');
        assert.equal(await card.locator('a, button, [tabindex]').count(), 0);
    }
    for (const selector of ['.topbar .btn-dark', '#open-microsoft-quiz', '.panel.qa a', '.doclist a', '.contact-email', '.faq summary', '.nav a']) {
        const control = page.locator(selector).first(); if (!await control.isVisible()) continue;
        await control.scrollIntoViewIfNeeded(); await page.mouse.move(0, 0);
        const before = await control.boundingBox();
        const layoutBefore = await control.evaluate(el => [el.offsetLeft, el.offsetTop, el.offsetWidth, el.offsetHeight]);
        await control.hover(); const hovered = await control.boundingBox();
        assert.deepEqual(hovered, before, `Sin movimiento al pasar el cursor: ${selector}`);
        const ratios = await renderedContrast(page, selector);
        assert.ok(ratios.every(c => c.ratio >= c.required), selector);
        await page.keyboard.press('Tab'); await control.focus();
        const focus = await control.evaluate(el => ({ style: getComputedStyle(el).outlineStyle, width: getComputedStyle(el).outlineWidth }));
        assert.equal(focus.style, 'solid'); assert.ok(parseFloat(focus.width) >= 2);
        assert.deepEqual(await control.evaluate(el => [el.offsetLeft, el.offsetTop, el.offsetWidth, el.offsetHeight]), layoutBefore, `Sin cambio de distribución al enfocar: ${selector}`);
        report.states.push({ width, selector, hoverMinimum: Math.min(...ratios.map(c => c.ratio)), focus });
    }
    assert.equal(await color('.faq details[open] summary', 'backgroundColor'), soft);
    assert.equal(await color('.faq details[open] p', 'backgroundColor'), white);
    const second = page.locator('.faq details').nth(1); await second.locator('summary').focus();
    await page.keyboard.press('Enter'); assert.equal(await page.locator('.faq details[open]').count(), 2, 'Acordeón permite varias abiertas');
    assert.equal(await second.locator('summary').evaluate(el => getComputedStyle(el).backgroundColor), soft);
    await page.keyboard.press('Enter'); assert.equal(await page.locator('.faq details[open]').count(), 1);

    const anchor = async id => {
        const metrics = await page.evaluate(id => {
            const target = document.getElementById(id);
            const heading = target.querySelector('h1,h2,h4') || target;
            return { id, header: document.querySelector('.topbar').getBoundingClientRect().bottom,
                titleTop: heading.getBoundingClientRect().top, titleBottom: heading.getBoundingClientRect().bottom,
                offset: parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--alto-cabecera')),
                headerHeight: document.querySelector('.topbar').getBoundingClientRect().height, hash: location.hash };
        }, id);
        assert.equal(metrics.hash, '#' + id); assert.ok(Math.abs(metrics.offset - metrics.headerHeight) < .1);
        assert.ok(metrics.titleTop >= metrics.header - .5, `Título bajo la barra: ${JSON.stringify(metrics)}`);
        assert.ok(metrics.titleBottom <= page.viewportSize().height, `Título visible: ${id}`);
        report.states.push({ width, anchor: metrics });
    };
    for (const link of await page.locator('.nav a, .panel.qa a, .topbar > .wrap > .btn').all()) {
        if (!await link.isVisible()) continue;
        const href = await link.getAttribute('href'); if (!href?.startsWith('#') || href === '#') continue;
        await link.click(); await anchor(href.slice(1));
    }
    // En móvil la navegación conserva su ocultación existente: se prueban también enlaces directos.
    const origin = new URL(page.url()).origin;
    for (const id of ['que-es', 'modelo', 'ejes', 'ruta', 'diagnostico', 'participa', 'beneficios', 'preguntas']) {
        await page.goto(`${origin}/#${id}`, { waitUntil: 'networkidle' }); await anchor(id);
    }
    await page.locator('.brand').click(); await anchor('inicio');
    await page.locator('.topbar > .wrap > .btn').click(); await anchor('participa');
    await page.goBack(); await anchor('inicio'); await page.goForward(); await anchor('participa');
    await page.goto(`${origin}/#ejes`, { waitUntil: 'networkidle' }); await anchor('ejes');
    await page.screenshot({ path: path.join(output, `after-anchor-ejes-${width}.png`) });
    if (width === 1366) {
        await page.emulateMedia({ reducedMotion: 'no-preference' });
        await page.evaluate(() => scrollTo({ top: 0, behavior: 'instant' }));
        await page.locator('.nav a[href="#ejes"]').click();
        await page.waitForFunction(() => Math.abs(document.getElementById('ejes').getBoundingClientRect().top
            - document.querySelector('.topbar').getBoundingClientRect().height - 18) < 1);
        await anchor('ejes'); report.states.push({ width, smoothAnchor: 'Ejes, movimiento normal' });
        await page.emulateMedia({ reducedMotion: 'reduce' });
    }
    const cdp = await page.context().newCDPSession(page); await cdp.send('DOM.enable'); await cdp.send('CSS.enable');
    const { root } = await cdp.send('DOM.getDocument');
    const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector: 'h1' });
    const actualFonts = (await cdp.send('CSS.getPlatformFontsForNode', { nodeId })).fonts;
    assert.ok(actualFonts.some(f => f.isCustomFont && f.familyName.includes('Inter'))); await cdp.detach();
    report.states.push({ width, actualFonts });
}
