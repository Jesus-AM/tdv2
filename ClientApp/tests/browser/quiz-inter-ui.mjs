// Razor exportado del host ASP.NET de pruebas; UI interna con transporte sintético.
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';
import { systemFixture } from './system-ui-fixture.mjs';
import { runInNewContext } from 'node:vm';
import { verifyFooterFlow } from './page-flow.mjs';
import { verifySignatures } from './identity-checks.mjs';
import { renderedContrast } from './rendered-contrast.mjs';
import { verifyFaqMotion } from './faq-motion.mjs';

const phase = process.argv[2] || 'after';
const output = path.resolve(process.env.TDV2_VISUAL_ARTIFACTS || '../.artifacts/quiz-inter-20261016');
await mkdir(output, { recursive: true });
const root = path.resolve(process.env.TDV2_PUBLIC_ROOT || '../tdv2/wwwroot');
const html = await readFile(path.join(output, `public-${phase}.html`), 'utf8');
const original = await readFile('tests/fixtures/microsoft-365-original.html', 'utf8');
const originalScript = original.match(/<script>([\s\S]*?)<\/script>/)[1];
const questions = runInNewContext(originalScript.split('let index=')[0] + ';questions');
if (phase === 'after') {
    assert.equal((await readFile(path.join(root, 'quiz/microsoft-365/quiz.js'), 'utf8')).trimEnd(), originalScript.split('function showHow(){')[0].trimEnd(), 'Preguntas, respuestas y puntuación originales; únicamente se retira la alerta de ayuda');
    assert.ok((await readFile(path.join(root, 'quiz/microsoft-365/quiz.css'), 'utf8')).startsWith(original.match(/<style>([\s\S]*?)<\/style>/)[1]), 'Diseño original íntegro');
}
const report = { phase, checks: [], fonts: [], errors: [], externalBlocked: [], requests: [] };
const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.woff2': 'font/woff2', '.ttf': 'font/ttf', '.pdf': 'application/pdf', '.jpg': 'image/jpeg', '.png': 'image/png', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = createServer(async (req, res) => {
    const url = new URL(req.url, 'http://localhost');
    res.setHeader('X-Frame-Options', 'DENY'); res.setHeader('X-Content-Type-Options', 'nosniff');
    if (url.pathname === '/') { res.setHeader('content-type', types['.html']); return res.end(html); }
    if (url.pathname === '/original-quiz.html') { res.setHeader('content-type', types['.html']); return res.end(original); }
    const file = path.resolve(root, '.' + url.pathname);
    if (!file.startsWith(root + path.sep)) { res.writeHead(404); return res.end(); }
    try { const bytes = await readFile(file); res.setHeader('content-type', types[path.extname(file)] || 'application/octet-stream'); res.end(bytes); }
    catch { res.writeHead(404); res.end(); }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const origin = `http://127.0.0.1:${server.address().port}`;
const browser = await chromium.launch({ channel: 'msedge', headless: true });
async function fonts(page, selector, label, requireFont = false, family = 'Noto Sans') {
    await page.evaluate(() => document.fonts.ready);
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('DOM.enable'); await cdp.send('CSS.enable');
    const { root } = await cdp.send('DOM.getDocument', { depth: -1, pierce: true });
    let rootId = root.nodeId;
    if (selector.includes(' >>> ')) {
        const [frameSelector, childSelector] = selector.split(' >>> ');
        const frameNode = await cdp.send('DOM.querySelector', { nodeId: rootId, selector: frameSelector });
        rootId = (await cdp.send('DOM.describeNode', { nodeId: frameNode.nodeId, depth: 1 })).node.contentDocument.nodeId;
        selector = childSelector;
    }
    const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: rootId, selector });
    const actual = (await cdp.send('CSS.getPlatformFontsForNode', { nodeId })).fonts;
    report.fonts.push({ label, actual });
    if (requireFont) assert.ok(actual.some(f => f.isCustomFont && f.familyName.includes(family)), JSON.stringify(actual));
    await cdp.detach();
}
async function context(width, height) {
    const ctx = await browser.newContext({ viewport: { width, height }, reducedMotion: 'reduce', hasTouch: width < 500 });
    await ctx.route('**/*', route => {
        const url = new URL(route.request().url());
        if (url.hostname === '127.0.0.1') return route.continue();
        report.externalBlocked.push(url.hostname); return route.abort();
    });
    const page = await ctx.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => report.errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error' && /Content Security Policy|Refused to/.test(message.text())) report.errors.push(message.text()); });
    page.on('request', request => report.requests.push({ url: request.url(), method: request.method() }));
    return { ctx, page };
}
try {
    for (const [width, height] of [[1366, 900], [1024, 900], [768, 1024], [390, 844], [320, 800]]) {
        const { ctx, page } = await context(width, height);
        await page.goto(origin); await page.evaluate(() => document.fonts.ready);
        await fonts(page, '.hero h1', `portal-${width}`, phase === 'after', 'Noto Sans');
        await page.screenshot({ path: path.join(output, `public-${phase}-${width}.png`), fullPage: true });
        const contrast=await renderedContrast(page,'.hero'); assert(contrast.every(c=>c.ratio>=c.required),JSON.stringify(contrast));
        report.checks.push({width,heroContrast:contrast});
        await page.locator('.hero').screenshot({ path: path.join(output, `hero-${phase}-${width}.png`) });
        // La captura aislada del bloque puede superar la ventana. Las pruebas de anclas usan la cabecera visible.
        await page.locator('#diagnostico').screenshot({ path: path.join(output, `diagnosis-${phase}-${width}.png`), style: '.topbar { visibility: hidden }' });
        if (phase === 'before') {
            await page.goto(origin + '/original-quiz.html');
            await fonts(page, '.hero h1', `quiz-original-${width}`);
            await page.screenshot({ path: path.join(output, `quiz-${phase}-${width}.png`), fullPage: true });
        } else {
            await verifyPublic(page, width);
            if (width === 1366 || width === 390) report.checks.push({width, faqMotion:await verifyFaqMotion(page, width)});
            if ([1366,390,320].includes(width)) await verifyQuiz(page, width, height);
        }
        await ctx.close();
    }
    const fixture = await systemFixture();
    try {
        const shared={name:'TDV2',auth:{user:{id:1,name:'Persona sintética',email:'persona@example.test'},roles:[],modules:[],canPreview:false,canRepresent:false},simulacion:null,representacion:null,csrfToken:'synthetic',session:{lifetime_ms:7200000},routes:{inicio:'/inicio',home:'/',connect:'/connect',logout:'/logout'}};
        fixture.pages={
            '/inicio':()=>({component:'Inicio',url:'/inicio',props:{...shared,formatos:[],administrador:false,directorio:[],urAdministracion:null,puedeColaboradores:false,sincronizadoEn:null}}),
            '/acceso-restringido':()=>({component:'AccesoRestringido',url:'/acceso-restringido',props:{...shared,access:{message:'Acceso no disponible en este caso sintético.'}}}),
        };
        Object.assign(fixture.content.sistemas[0], { proceso: 'PO-01', sistema: 'microsoft_365', aplicacionesMicrosoft365: ['excel'], uso: 'consultar', estado: 'bien' });
        for (const [width, height] of [[1920,900], [1440,900], [1366, 900], [1280,900], [1024, 900], [768, 1024], [390, 844], [320, 844]]) {
            const { ctx, page } = await context(width, height); await fixture.webSockets(ctx);
            await page.goto(fixture.origin + '/formatos/A');
            await page.getByRole('tab', { name: 'Sistemas y herramientas', exact: true }).click();
            await fonts(page, '.systems-group-content h3', `interno-${width}`, phase === 'after', 'Noto Sans');
            await page.screenshot({ path: path.join(output, `internal-${phase}-${width}.png`), fullPage: true });
            await verifySignatures(page, '.app-footer', true, 1281);
            const footerLayout=await page.locator('.app-footer-inner').evaluate(el=>{
                const box=el.getBoundingClientRect(),style=getComputedStyle(el),parts=[...el.children].map(e=>{const r=e.getBoundingClientRect();return{left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width};});
                const lines=[...el.querySelectorAll('.institutional-signature span,.institutional-signature strong')].map(e=>({height:e.getBoundingClientRect().height,lineHeight:parseFloat(getComputedStyle(e).lineHeight)}));
                return {left:box.left+parseFloat(style.paddingLeft),right:box.right-parseFloat(style.paddingRight),parts,lines};
            });
            for(const line of footerLayout.lines)assert(Math.abs(line.height-line.lineHeight)<1,'Prefijo y nombre del área en sus dos líneas completas');
            if(width>1280){assert(Math.abs(footerLayout.parts[0].left-footerLayout.left)<1);assert(Math.abs(footerLayout.parts.at(-1).right-footerLayout.right)<1,'El pie aprovecha todo el ancho interior');}
            report.checks.push({width,footerLayout});
            await page.locator('.app-footer').screenshot({path:path.join(output,`internal-footer-${width}.png`)});
            report.checks.push({width,internalFlow:await verifyFooterFlow(page,'#main-content','.app-footer')});
            for(const route of ['/inicio','/acceso-restringido']) {
                await page.goto(fixture.origin+route);await verifySignatures(page,'.app-footer',true,1281);
                await page.screenshot({path:path.join(output,`internal-${route.slice(1)}-${width}.png`),fullPage:true});
                report.checks.push({width,route,flow:await verifyFooterFlow(page,'main','.app-footer')});
            }
            await ctx.close();
        }
    } finally { await fixture.close(); }
    assert.deepEqual(report.requests.filter(r => r.url.startsWith(origin) && r.method !== 'GET'), [], 'El quiz no envía respuestas');
    assert.deepEqual(report.errors, []); report.completed = true;
} finally {
    await writeFile(path.join(output, `quiz-inter-${phase}-report.json`), JSON.stringify(report, null, 2));
    await browser.close(); await new Promise(resolve => server.close(resolve));
}

async function verifyPublic(page, width) {
    await expect(page.locator('.contact-info>.contact-area')).toHaveText(['Dirección General de Tecnologías de la Información', 'Subdirección de Inteligencia de Datos']);
    await expect(page.locator('.contact-email')).toHaveText('td.pi@uacj.mx');
    await expect(page.locator('.contact-email')).toHaveAttribute('href','mailto:td.pi@uacj.mx');
    await expect(page.locator('#ruta>.sub')).toContainText('Un camino de ocho etapas');
    const steps = page.locator('#ruta .step'); await expect(steps).toHaveCount(8);
    for (const [index,text] of [[0,'Comunicar la visión, los objetivos, el alcance y los beneficios del PITD'],[2,'Fortalecer cultura organizacional, cultura de datos y capacidades digitales'],[6,'Desarrollar, probar y poner en operación']]) await expect(steps.nth(index).locator('p')).toHaveText(text);
    const block = page.locator('#diagnostico'), cards = block.getByRole('article');
    const blue = 'rgb(33, 52, 85)', white = 'rgb(255, 255, 255)';
    const head = block.locator('.mdd-head');
    const transparent = 'rgba(0, 0, 0, 0)';
    await expect(block).toHaveCSS('background-color', white);
    const gradient = await page.locator('#ejes .band').evaluate(el => getComputedStyle(el).backgroundImage);
    assert(gradient.startsWith('linear-gradient('));
    await expect(head).toHaveCSS('background-color', blue);
    await expect(head).toHaveCSS('background-image', 'none');
    const typography = el => {
        const {fontFamily, fontSize, fontWeight, lineHeight, letterSpacing, color} = getComputedStyle(el);
        return {fontFamily, fontSize, fontWeight, lineHeight, letterSpacing, color};
    };
    for (const [source, targets] of [
        ['#ruta > h2', head.locator('h2')],
        ['#ruta > .sub', head.locator('.sub')],
        ['#ejes .card h3', block.locator('.card h3,.mdd-quiz-text h3')],
        ['#ejes .card p', block.locator('.card p,.mdd-quiz-text p')],
    ]) {
        const reference = await page.locator(source).first().evaluate(typography);
        if (source.startsWith('#ruta')) reference.color = white;
        for (const target of await targets.all()) assert.deepEqual(await target.evaluate(typography), reference);
    }
    for (const surface of await block.locator('.mdd-body,.mdd-cards,.mdd-facts,.diagnosis-game').all())
        await expect(surface).toHaveCSS('background-color', transparent);
    for (const label of await head.locator('h2,.sub,.mdd-dims li,.mdd-num').all())
        await expect(label).toHaveCSS('color', white);
    await expect(head.locator(':scope > h2:first-child')).toHaveText('Diagnóstico de madurez digital');
    await expect(head).not.toContainText(/etapa 2 de 4/i);
    await expect(head.locator('.mdd-progress')).toHaveCount(0);
    await expect(block.locator('.sub')).toHaveText('Una fotografía honesta de tu área en 8 dimensiones, alineadas a la Política de Innovación Tecnológica Digital de la UACJ.');
    await expect(head.locator('ol .mdd-num')).toHaveText(['01','02','03','04','05','06','07','08']);
    await expect(cards.locator('h3')).toHaveText(['Procedimientos','Competencias digitales','Infraestructura y seguridad','Sustentabilidad tecnológica']);
    await expect(cards.locator('.mdd-card-top:first-child')).toHaveText(['PARA TU ÁREA','PARA TI','PARA EL ENLACE TÉCNICO','PARA LA ADMINISTRACIÓN DEL ÁREA']);
    await expect(cards.locator('p + .mdd-card-dims')).toHaveText(['Dimensiones 01 · 02 · 03 · 08','Dimensión 05','Dimensiones 04 · 06 · 07 · 08','Dimensión 08']);
    await expect(cards.locator('.mdd-card-dims + .btn:last-child')).toHaveCount(4);
    await expect(cards.nth(0).getByRole('link', {name:'Diagnosticar procedimientos'})).toHaveAttribute('href','/connect?returnUrl=/inicio');
    await expect(cards.nth(1).getByRole('link', {name:'Evaluar mis competencias'})).toHaveAttribute('href','/connect?returnUrl=/competencias-digitales');
    for (const card of [cards.nth(2), cards.nth(3)]) await expect(card.getByRole('button')).toBeDisabled();
    await expect(block).not.toContainText(/≈|calentamiento/i);
    const boxes = await cards.evaluateAll(elements => elements.map(e => e.getBoundingClientRect().toJSON()));
    const gap = 16;
    const [first, second] = await cards.getByRole('link').evaluateAll(links => links.map(link => link.getBoundingClientRect().toJSON()));
    assert.equal(first.height, second.height, 'Altura de los dos botones de acceso');
    if (width <= 640) assert(boxes.every((box,i) => !i || (box.top - boxes[i-1].bottom === gap && box.left === boxes[0].left)));
    else {
        assert.equal(boxes[0].top, boxes[1].top);
        assert.equal(boxes[1].left - boxes[0].right, gap);
        assert.equal(boxes[3].left - boxes[2].right, gap);
        assert.equal(boxes[2].top - boxes[0].bottom, gap);
        assert.equal(boxes[3].top - boxes[1].bottom, gap);
        assert.equal(first.y, second.y);
        const labels = await cards.locator('.mdd-card-dims').evaluateAll(elements => elements.map(el => el.getBoundingClientRect().top));
        assert.equal(labels[0], labels[1]);
        assert.equal(labels[2], labels[3]);
    }
    await expect(block.locator('.mdd-body')).toHaveCSS('padding-left',width <= 390 ? '16px' : '24px');
    if (width > 1120) {
        const box = await block.boundingBox(), sidebar = await page.locator('aside.side').boundingBox();
        assert(box.x + box.width <= sidebar.x);
    }
    assert(await block.locator('.btn,p').evaluateAll(elements => elements.every(e => e.scrollWidth <= e.clientWidth + 1 && e.scrollHeight <= e.clientHeight + 1)));
    // Noto Sans puede dibujarse fuera de la caja de línea de los subtítulos de Ejes.
    assert(await block.locator('h3').evaluateAll(headings => headings.every(heading => {
        const group = heading.closest('.card,.diagnosis-game');
        const range = document.createRange();
        range.selectNodeContents(heading);
        const text = range.getBoundingClientRect();
        const bounds = group.getBoundingClientRect();
        return getComputedStyle(heading).overflow === 'visible' &&
            text.left >= bounds.left - 1 && text.right <= bounds.right + 1 &&
            text.top >= bounds.top - 1 && text.bottom <= group.querySelector('p').getBoundingClientRect().top + 1;
    })));
    await expect(page.locator('.diagnosis-game h3')).toHaveText('¿Qué tanto sabes de Microsoft 365?');
    await expect(page.locator('.diagnosis-game p')).toHaveText('Antes de la encuesta, pon a prueba tus conocimientos en un quiz de 5 minutos.');
    const panel = await page.locator('.panel.qa').evaluate(e => {
        const style = getComputedStyle(e);
        return {border: style.border, radius: style.borderRadius};
    });
    await expect(block).toHaveCSS('border', panel.border);
    await expect(block).toHaveCSS('border-radius', panel.radius);
    await expect(head).toHaveCSS('border-bottom-left-radius', '0px');
    await expect(head).toHaveCSS('border-bottom-right-radius', '0px');
    const bounds = await block.evaluate(section => {
        const rect = selector => section.querySelector(selector).getBoundingClientRect();
        const frame = section.getBoundingClientRect(), header = rect('.mdd-head');
        return {
            headerInsets: [header.top - frame.top, header.left - frame.left, frame.right - header.right],
            titleInset: rect('h2').top - rect('.mdd-head').top,
            headerPadding: parseFloat(getComputedStyle(section.querySelector('.mdd-head')).paddingTop),
            headerBottom: rect('.mdd-head').bottom, bodyTop: rect('.mdd-body').top,
            titleLeft: rect('h2').left, cardsLeft: rect('.mdd-cards').left, gameTextLeft: rect('.diagnosis-game h3').left,
        };
    });
    assert.deepEqual(bounds.headerInsets, [1,1,1]);
    assert(Math.abs(bounds.titleInset - bounds.headerPadding) < 1);
    assert.equal(bounds.headerBottom, bounds.bodyTop);
    assert.equal(bounds.titleLeft, bounds.cardsLeft);
    assert.equal(bounds.titleLeft, bounds.gameTextLeft);
    for (const card of await cards.all()) {
        await expect(card).toHaveCSS('background-color', white);
        await expect(card).toHaveCSS('border', panel.border);
        await expect(card).toHaveCSS('border-radius', panel.radius);
        await expect(card).toHaveCSS('box-shadow', 'none');
        await expect(card.locator('h3')).toHaveCSS('color', blue);
        await expect(card.locator('p')).toHaveCSS('color', blue);
    }
    for (const icon of await block.locator('.mdd-card-title svg,.mdd-facts svg,.quiz-launch svg').all())
        await expect(icon).toHaveCSS('stroke', blue);
    for (const label of await block.locator('.mdd-facts span,.mdd-quiz-text h3,.mdd-quiz-text p').all())
        await expect(label).toHaveCSS('color', blue);
    const linksInCards = cards.getByRole('link');
    const play = block.getByRole('button', {name:'Jugar ahora',exact:true});
    for (const link of await linksInCards.all()) {
        await expect(link).toHaveCSS('background-color', blue);
        await expect(link).toHaveCSS('color', white);
    }
    await expect(play).toHaveCSS('background-color', white);
    await expect(play).toHaveCSS('border-top-color', blue);
    await expect(play).toHaveCSS('color', blue);
    const contrast = await renderedContrast(page, '#diagnostico');
    assert(contrast.length > 0 && contrast.every(c => c.ratio >= c.required), JSON.stringify(contrast));
    report.checks.push({width, diagnosisContrast:contrast});
    for (const link of await linksInCards.all()) {
        await link.hover();
        await expect(link).toHaveCSS('background-color', 'rgb(237, 239, 242)');
        await expect(link).toHaveCSS('color', blue);
        await expect(link).toHaveCSS('border-top-color', blue);
    }
    await linksInCards.nth(1).focus();
    for (const [control, key] of [[linksInCards.first(),'Shift+Tab'],[linksInCards.nth(1),'Tab'],[play,'Tab']]) {
        await page.keyboard.press(key);
        await expect(control).toBeFocused();
        await expect(control).toHaveCSS('outline-width', '3px');
        await expect(control).toHaveCSS('outline-offset', '3px');
        await expect(control).toHaveCSS('outline-style', 'solid');
        await expect(control).toHaveCSS('outline-color', blue);
    }
    await play.hover();
    await expect(play).toHaveCSS('background-color', 'rgb(237, 239, 242)');
    await expect(play).toHaveCSS('color', blue);
    const links = page.locator('#docs a');
    const files = ['PIT_UACJ.pdf','PITD_2026_2030_Marco_General_UACJ.pdf','Decalogo_Cultura_de_Datos_UACJ.pdf','Glosario_Transformacion_Digital_UACJ.pdf'];
    const titles = ['Política de Innovación Tecnológica Digital','Plan Institucional de Transformación Digital','Decálogo','Glosario'];
    await expect(links).toHaveCount(files.length);
    for (let i=0;i<files.length;i++) {
        await expect(links.nth(i)).toHaveAccessibleName(titles[i]+' (PDF, abre en una pestaña nueva)');
        await expect(links.nth(i)).toHaveAttribute('href','/documentos/'+files[i]);
        await expect(links.nth(i)).toHaveAttribute('target','_blank'); await expect(links.nth(i)).toHaveAttribute('rel','noopener');
        assert.equal(await links.nth(i).getAttribute('download'),null);
        if (width===1366) {
            const response=await page.request.get(origin+'/documentos/'+files[i]); assert.equal(response.status(),200); assert.equal(response.headers()['content-type'],'application/pdf');
            const opened=page.waitForEvent('popup'); await links.nth(i).click(); const pdf=await opened;
            await pdf.waitForLoadState('load'); assert.equal(pdf.url(),origin+'/documentos/'+files[i]);
            assert.equal(await pdf.evaluate(()=>window.opener),null);
            await pdf.close(); // Se prueba navegación/HTTP; el visor PDF nativo no se evalúa por captura headless.
        }
    }
    await verifySignatures(page, '.public-footer', true);
    assert((await page.locator('.hero').evaluate(e=>getComputedStyle(e).backgroundImage)).includes('/images/portal/img_9885-1920.jpg'));
    await page.locator('.public-footer').screenshot({path:path.join(output,`footer-${width}.png`)});
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
    const anchors = [
        [page.locator('.topbar').getByRole('link', {name:/^Participa/}), '#diagnostico'],
        [page.locator('.qa').getByRole('link', {name:/^Registro de procedimientos/}), '#participa'],
        [page.locator('.qa').getByRole('link', {name:/^Diagnóstico de madurez/}), '#diagnostico'],
    ];
    const navDiagnosis = page.locator('.nav').getByRole('link', {name:'Diagnóstico',exact:true});
    if (await navDiagnosis.isVisible()) anchors.push([navDiagnosis, '#diagnostico']);
    for (const [link, hash] of anchors) {
        await expect(link).toHaveAttribute('href', hash);
        await link.click();
        await expect.poll(() => page.evaluate(hash => {
            const target = document.querySelector(hash);
            const top = target.getBoundingClientRect().top;
            const title = target.querySelector('h2,h3').getBoundingClientRect();
            const height = document.querySelector('.topbar').getBoundingClientRect().bottom;
            return location.hash === hash && top >= height - 1 && top < height + 40
                && title.top >= height && title.bottom <= innerHeight;
        }, hash)).toBe(true);
    }
    await page.locator('.qa a[href="#diagnostico"]').click();
    await page.screenshot({path:path.join(output,`diagnostico-ancla-${width}.png`)});
    if (width === 768 || width === 1024) {
        const play = block.getByRole('button', {name:'Jugar ahora',exact:true});
        await play.click();
        await expect(page.frameLocator('#microsoft-quiz-frame').getByRole('button', {name:'🚀 Comenzar reto',exact:true})).toBeVisible();
        await page.getByRole('button', {name:'Cerrar reto Microsoft 365'}).click();
        await expect(page.locator('#microsoft-quiz-dialog')).toBeHidden();
        await expect(play).toBeFocused();
    }
    for (const selector of ['.nav a','.hero h1','#que-es p','.card h3','#participa .btn-dark','.contact-email'])
        assert((await page.locator(selector).first().evaluate(e=>getComputedStyle(e).fontFamily)).includes('Noto Sans'));
    report.checks.push({width,publicFlow:await verifyFooterFlow(page,'.public-content','.public-footer'),content:true,documents:true});
}

async function verifyQuiz(page, width, height) {
    const digitalLink = page.getByRole('link', { name: 'Evaluar mis competencias', exact: true });
    await expect(digitalLink).toHaveAttribute('href', '/connect?returnUrl=/competencias-digitales');
    const open = page.getByRole('button', { name: 'Jugar ahora', exact: true });
    let portalScroll;
    const dialog = page.getByRole('dialog', { name: 'Reto Microsoft 365', exact: true });
    const quiz = page.frameLocator('#microsoft-quiz-frame');
    await open.scrollIntoViewIfNeeded(); await open.focus();
    portalScroll = await page.evaluate(() => [scrollX, scrollY]);
    await open.press('Enter');
    await expect(dialog).toBeVisible();
    await expect(page.getByRole('button', { name: 'Cerrar reto Microsoft 365' })).toBeFocused();
    await expect(quiz.getByRole('button', { name: '🚀 Comenzar reto', exact: true })).toBeVisible();
    await page.keyboard.press('Tab');
    await expect(quiz.getByRole('button', { name: '🚀 Comenzar reto', exact: true })).toBeFocused();
    await page.keyboard.press('Shift+Tab');
    await expect(page.getByRole('button', { name: 'Cerrar reto Microsoft 365' })).toBeFocused();
    await fonts(page, '#microsoft-quiz-frame >>> .hero h1', `quiz-${width}`, true);
    const sizes = await dialog.evaluate(el => {
        const r = el.getBoundingClientRect(), f = el.querySelector('iframe').getBoundingClientRect();
        return { x: r.x, y: r.y, width: r.width, height: r.height, iframeHeight: f.height, scroll: el.scrollHeight - el.clientHeight };
    });
    if (width < 600) { assert.equal(sizes.width, width); assert.equal(sizes.height, height); }
    else assert.ok(sizes.width >= 1100 && sizes.width < width);
    assert.equal(sizes.scroll, 0, 'Sólo el documento del iframe desplaza el juego');
    await page.screenshot({ path: path.join(output, `quiz-${phase}-${width}.png`) });
    const frame = page.frames().find(f => f !== page.mainFrame());
    assert.equal(await frame.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, 'Quiz sin desbordamiento horizontal');
    await expect(quiz.getByText('Resultado por competencias.', { exact: true })).toBeVisible();
    const help = quiz.getByRole('button', { name: '¿Cómo funciona?', exact: true });
    const panel = quiz.locator('#quiz-instructions');
    let alerts=0; const rejectAlert=async native=>{alerts++;await native.dismiss();};page.on('dialog',rejectAlert);
    const quizState=()=>quiz.locator('#xp,#streak,#counter,#score').allTextContents();
    const initialState=await quizState();
    await help.focus();await page.keyboard.press('Enter');
    await expect(help).toHaveAttribute('aria-expanded','true');await expect(panel).toBeVisible();
    await expect(quiz.locator('#quiz-instructions-title')).toBeFocused();
    await expect(panel.locator('li')).toHaveCount(4);await expect(panel).toContainText('100 XP');
    assert.equal(await panel.locator('dialog,[role="dialog"]').count(),0);
    await page.screenshot({path:path.join(output,`quiz-help-${width}.png`)});
    await quiz.getByRole('button',{name:'Entendido',exact:true}).scrollIntoViewIfNeeded();
    await page.screenshot({path:path.join(output,`quiz-help-end-${width}.png`)});
    assert.equal(await quiz.locator('body').evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
    await quiz.getByRole('button',{name:'Entendido',exact:true}).focus();await page.keyboard.press('Enter');
    await expect(panel).toBeHidden();await expect(help).toBeFocused();await expect(help).toHaveAttribute('aria-expanded','false');
    await help.click();await page.keyboard.press('Escape');await expect(panel).toBeHidden();await expect(dialog).toBeVisible();await expect(help).toBeFocused();
    assert.deepEqual(await quizState(),initialState);assert.equal(alerts,0);page.off('dialog',rejectAlert);

    // Español y pesos reales, sin depender de una fuente instalada en el sistema.
    if (width === 1366) {
        for (const target of [page, frame]) {
            const family = 'Noto Sans';
            for (const weight of [400, 500, 600, 700]) {
                await target.evaluate(({w, family}) => {
                    const probe = document.createElement('span'); probe.id = 'inter-probe';
                    probe.textContent = 'ÁÉÍÓÚ áéíóú Ññ ¿¡ ü';
                    probe.style.cssText = `position:fixed;top:0;left:0;z-index:9999;background:white;font-family:"${family}";font-size:16px;font-weight:${w}`;
                    (document.querySelector('dialog[open]') || document.body).append(probe);
                }, {w:weight, family});
                await target.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
                await fonts(page, target === page ? '#inter-probe' : '#microsoft-quiz-frame >>> #inter-probe', `español-${target === page ? 'portal' : 'quiz'}-${weight}`, true, family);
                await target.evaluate(() => document.getElementById('inter-probe').remove());
            }
        }
    }

    const rounds = width === 1366 ? [10, 7, 5, 0] : [width === 390 ? 7 : 10];
    for (let round = 0; round < rounds.length; round++) {
        const desired = rounds[round];
        await quiz.getByRole('button', { name: round ? '🔄 Jugar de nuevo' : '🚀 Comenzar reto', exact: true }).click();
        let expectedXp = 0, streak = 0, maximum = 0;
        for (let i = 0; i < 10; i++) {
            const q = questions[i], correct = width === 390 ? [0, 1, 3, 4, 5, 7, 8].includes(i) : i < desired;
            await expect(quiz.locator('#question')).toHaveText(q.q);
            await expect(quiz.locator('#counter')).toHaveText(`Reto ${i + 1} de 10`);
            const answer = quiz.locator('.answer').nth(correct ? q.c : (q.c + 1) % 4);
            await answer.focus(); await answer.press('Enter');
            await expect(quiz.locator('#feedback')).toContainText(q.e);
            if (correct) { streak++; maximum = Math.max(maximum, streak); expectedXp += 100 + (streak - 1) * 20; }
            else streak = 0;
            await expect(quiz.locator('#xp')).toHaveText(String(expectedXp));
            await expect(quiz.locator('#streak')).toHaveText(String(streak));
            await expect(quiz.locator('.answer.correct')).toHaveCount(1);
            await expect(quiz.locator('.answer.wrong')).toHaveCount(correct ? 0 : 1);
            await expect(quiz.locator('#nextBtn')).toBeFocused();
            if(i===2 && round===0) {
                const before=await quizState();await page.getByRole('button',{name:'Cerrar reto Microsoft 365'}).click();
                await open.click();assert.deepEqual(await quizState(),before,'Cerrar y abrir el test conserva progreso, XP y racha');
                await expect(quiz.locator('.answer:disabled')).toHaveCount(4);await quiz.locator('#nextBtn').focus();
            }
            assert.notEqual(await quiz.locator('#nextBtn').evaluate(el => getComputedStyle(el).outlineStyle), 'none');
            if (i === 0 && round === 0) await page.screenshot({ path: path.join(output, `question-after-${width}.png`) });
            await quiz.locator('#nextBtn').press('Enter');
        }
        await expect(quiz.locator('#correctCount')).toHaveText(`${desired}/10`);
        await expect(quiz.locator('#score')).toHaveText(`${desired * 10}%`);
        await expect(quiz.locator('#finalXP')).toHaveText(`${expectedXp} XP`);
        await expect(quiz.locator('#maxStreak')).toHaveText(String(maximum));
        await expect(quiz.locator('#resultTitle')).toBeFocused();
        const titles = { 10: '🚀 ¡Nivel experto!', 7: '🌟 ¡Muy buen nivel!', 5: '💪 ¡Buen comienzo!', 0: '🌱 ¡Vamos paso a paso!' };
        await expect(quiz.locator('#resultTitle')).toHaveText(titles[desired]);
        await expect(quiz.locator('#level')).toHaveText({ 10: 'Avanzado', 7: 'Intermedio', 5: 'Operativo', 0: 'Básico' }[desired]);
        if (desired === 10) { await expect(quiz.locator('#badges .badge')).toHaveCount(4); await expect(quiz.locator('#finalXP')).toHaveText('1900 XP'); }
        if (desired === 0) await expect(quiz.locator('#badges')).toHaveText('🎯 Primer reto completado');
        await page.screenshot({ path: path.join(output, `result-${desired}-after-${width}.png`) });
        report.checks.push(`${width}: diez preguntas, ${desired} aciertos, explicaciones/XP/rachas/nivel/medallas originales`);
    }
    await quiz.getByRole('button', { name: '🔄 Jugar de nuevo', exact: true }).click();
    await expect(quiz.locator('#xp')).toHaveText('0'); await expect(quiz.locator('#counter')).toHaveText('Reto 1 de 10');
    await quiz.locator('.answer').first().focus(); await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible(); await expect(open).toBeFocused();
    assert.deepEqual(await page.evaluate(() => [scrollX, scrollY]), portalScroll);
    await open.press('Enter'); await page.keyboard.press('Escape'); await expect(dialog).not.toBeVisible();
    await open.click(); await page.getByRole('button', { name: 'Cerrar reto Microsoft 365' }).click();
    await expect(open).toBeFocused(); assert.deepEqual(await page.evaluate(() => [scrollX, scrollY]), portalScroll);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false, 'Portal sin desbordamiento');
    report.checks.push(`${width}: diálogo, teclado, ayuda, reinicio, Escape en ambos documentos, cierre/foco/scroll`);
}
