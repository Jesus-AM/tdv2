// Revisión visual del bundle real con catálogo sintético; no arranca ASP.NET ni conecta bases o identidad.
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { chromium, expect } from '@playwright/test';

const phase = process.argv[2] === 'before' ? 'before' : 'after';
const root = path.resolve('tdv2/wwwroot'), output = path.resolve('.artifacts/operational-layout', phase);
await mkdir(output, { recursive: true });
const unit = (id, code, name, level = 2, parent = null, kind = '0') => ({
    id_ur: id, cve_ur: code, desc_ur: name, nivel_ur: level, id_ur_pertenece: parent, tipo_ur: kind, ejercicio: 2026,
});
const roots = Array.from({ length: 10 }, (_, i) => unit(`area-${i}`, `0${6000 + i * 100}`, `Área de ejemplo ${i + 1}`));
roots[3].desc_ur = 'Área de ejemplo con un nombre extenso para comprobar su lectura completa en pantallas pequeñas';
const auxiliary = unit('aux', '06001', 'Nodo tipo N excluido', 3, roots[0].id_ur, ' n ');
const child = { ...unit('child', '06002', 'Dependencia de ejemplo', 3, 'aux'), id_ur_principal: roots[0].id_ur };
const orphan = unit('orphan', '08001', 'Área sin agrupador de ejemplo', 3);
const forms = [...roots, child, orphan].map((u, i) => ({ ...u, porcentaje: (i % 6) * 20,
    editable: i === 0 || u === child, propia: i === 0 || u === child, url: `/formatos/${u.id_ur}` }));
let denyCollaborators = false, personal = false, delay = 0, reads = 0;
const document = () => ({ component: 'Inicio', url: '/inicio', props: {
    auth: { user: { name: 'Persona de ejemplo', email: 'ejemplo@example.invalid' }, roles: [], canPreview: false, canRepresent: false,
        modules: [
            { id: 1, key: 'procesos_operativos', name: 'Procesos operativos', route: '/inicio', parent: null },
            { id: 2, key: 'configuracion', name: 'Configuración', route: '/configuracion', parent: null },
            { id: 3, key: 'sincronizaciones', name: 'Sincronizaciones', route: '/configuracion/sincronizaciones', parent: 'configuracion' },
            { id: 4, key: 'pruebas_acceso', name: 'Pruebas de acceso', route: '/configuracion/pruebas-acceso', parent: 'configuracion' },
        ] }, simulacion: null, representacion: null, contextoEdicion: 'own', csrfToken: '', session: { lifetime_ms: 7200000 },
    routes: { inicio: '/inicio', home: '/', connect: '/connect', logout: '/logout' },
    administrador: !personal, consultaInstitucional: !personal, directorio: [...roots, auxiliary, child, orphan],
    urAdministracion: roots[0], puedeColaboradores: !denyCollaborators, sincronizadoEn: null,
    formatos: personal ? forms.filter(f => f.propia) : forms,
} });
const server = createServer(async (req, res) => {
    const url = new URL(req.url, 'http://localhost');
    if (req.method !== 'GET') { res.writeHead(405).end(); return; }
    if (url.pathname === '/user/photo') { res.setHeader('Content-Type', 'application/json'); res.end('{"photo":null}'); return; }
    if (url.pathname === '/inicio' && req.headers['x-tdv2-page']) {
        reads++; await new Promise(resolve => setTimeout(resolve, delay));
        res.setHeader('Content-Type', 'application/json'); res.end(JSON.stringify(document())); return;
    }
    const target = path.resolve(root, '.' + (url.pathname === '/inicio' ? '/index.html' : url.pathname));
    if (!target.startsWith(root + path.sep)) { res.writeHead(404).end(); return; }
    try {
        const data = await readFile(target);
        res.setHeader('Content-Type', ({ '.js': 'text/javascript', '.css': 'text/css', '.html': 'text/html', '.svg': 'image/svg+xml' })[path.extname(target)] || 'application/octet-stream');
        res.end(data);
    } catch { res.writeHead(404).end(); }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const origin = `http://127.0.0.1:${server.address().port}`;
let browser;
const report = { synthetic: true, database: false, authentication: false, phase, passed: false, measurements: [], checks: [], errors: [] };
try {
    browser = await chromium.launch({ channel: 'msedge', headless: true });
    const page = await browser.newPage();
    page.on('pageerror', e => report.errors.push(e.message));
    await page.route('**/*', route => new URL(route.request().url()).origin === origin ? route.continue() : route.abort());
    for (const [width, height] of [[1920, 1080], [1366, 768], [768, 1024], [390, 844], [320, 740]]) {
        await page.setViewportSize({ width, height });
        await page.goto(origin + '/inicio');
        await expect(page.locator('.area-root-trigger').first()).toBeVisible();
        await page.waitForTimeout(220);
        const measured = await page.evaluate(() => {
            const rect = el => { const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height, right: r.right, bottom: r.bottom }; };
            const rows = [...document.querySelectorAll('.area-root-trigger')].map(rect);
            const stats = [...document.querySelectorAll('.surface')].slice(0, 3).map(rect);
            const nav = document.querySelector('#desktop-navigation');
            return { viewport: { width: innerWidth, height: innerHeight }, overflow: document.documentElement.scrollWidth > innerWidth,
                firstArea: rows[0], completeRowsInViewport: rows.filter(r => r.bottom <= innerHeight).length, rows, stats,
                logo: rect(document.querySelector('.MuiToolbar-root img')), navigation: nav && rect(nav),
                heading: rect(document.querySelector('.operational-heading')), font: getComputedStyle(document.querySelector('h1')).fontFamily };
        });
        report.measurements.push(measured);
        assert.equal(measured.overflow, false);
        if (width > 900) assert.equal(measured.navigation.x - measured.logo.right, 32);
        if (phase === 'after' && width >= 768) {
            measured.stats.forEach(r => assert.ok(r.height >= 76 && r.height <= 84));
            assert.ok(measured.firstArea.height >= 76 && measured.firstArea.height <= 88);
        }
        if (phase === 'after') {
            const labelsFit = await page.locator('.operational-stat-copy .MuiTypography-body2').evaluateAll(labels =>
                labels.every(label => label.scrollWidth <= label.clientWidth));
            assert.equal(labelsFit, true);
        }
        await page.screenshot({ path: path.join(output, `inicio-${width}.png`) });
    }
    report.checks.push('Escritorio 1920×1080 y 1366×768, tableta 768×1024, móviles 390×844 y 320×740 sin desbordamiento; cabecera conservada.');
    if (phase === 'after') {
        await page.setViewportSize({ width: 1366, height: 768 });
        await page.goto(origin + '/inicio');
        const stat = label => page.getByText(label, { exact: true }).locator('xpath=ancestor::*[contains(@class,"surface")][1]');
        await expect(stat('Formatos disponibles')).toContainText(String(forms.length));
        await expect(stat('Avance promedio')).toContainText(`${Math.round(forms.reduce((s, f) => s + f.porcentaje, 0) / forms.length)}%`);
        await expect(stat('Formatos completos')).toContainText(String(forms.filter(f => f.porcentaje === 100).length));
        await expect(page.getByText(auxiliary.desc_ur)).toHaveCount(0);
        await expect(page.getByText('10 áreas principales · 12 áreas con formato', { exact: true })).toHaveCount(1);
        await page.getByRole('button', { name: 'Expandir visibles' }).click();
        const open = name => page.getByRole('button', { name: `Abrir formato de ${name}`, exact: true });
        await expect(open(roots[1].desc_ur)).toBeVisible(); // Sin dependientes: conserva su formato propio.
        await expect(open(child.desc_ur)).toBeVisible();
        const destination = page.waitForRequest(r => new URL(r.url()).pathname === forms[1].url);
        await open(roots[1].desc_ur).click(); await destination;
        await page.goto(origin + '/inicio');
        await page.getByRole('button', { name: 'Expandir visibles' }).click();
        await page.getByRole('button', { name: 'Contraer', exact: true }).click();
        await expect(open(roots[0].desc_ur)).not.toBeVisible();
        const search = page.getByLabel('Buscar área o clave');
        for (const code of ['06000', '6000']) { await search.fill(code); await expect(open(child.desc_ur)).toBeVisible(); }
        await search.fill('sin coincidencias'); await expect(page.getByText('No encontramos áreas con esos filtros')).toBeVisible();
        await page.getByRole('button', { name: 'Limpiar filtros', exact: true }).click();
        await page.getByRole('combobox', { name: 'Mostrar', exact: true }).click();
        await page.getByRole('option', { name: 'Completos', exact: true }).click();
        await expect(open(roots[5].desc_ur)).toBeVisible();
        await expect(open(roots[0].desc_ur)).toHaveCount(0);
        await expect(page.getByRole('listbox')).not.toBeVisible();
        await page.getByRole('combobox', { name: 'Mostrar', exact: true }).click();
        await page.getByRole('option', { name: 'Todas las áreas', exact: true }).click();
        await page.getByRole('tab', { name: 'Mis áreas', exact: true }).click();
        await expect(page.locator('.area-root-trigger')).toHaveCount(1);
        await page.getByRole('button', { name: 'Expandir visibles' }).click(); await expect(open(child.desc_ur)).toBeVisible();
        await page.getByRole('tab', { name: 'Todas las áreas', exact: true }).click();
        await page.getByRole('button', { name: /page 2|página 2/i }).click();
        await expect(page.locator('.area-root-trigger')).toHaveCount(2);
        await page.goto(origin + '/inicio');
        const refresh = page.getByRole('button', { name: 'Actualizar', exact: true });
        await expect(refresh).toBeVisible();
        delay = 800; const beforeReads = reads;
        await refresh.click(); await expect(refresh).toBeDisabled(); await expect(refresh.getByRole('progressbar')).toBeVisible();
        await expect(refresh).toBeEnabled(); assert.equal(reads, beforeReads + 1); delay = 0;
        await page.setViewportSize({ width: 390, height: 844 }); await page.emulateMedia({ reducedMotion: 'reduce' });
        const trigger = page.locator('.area-root-trigger').first(); await trigger.focus(); await page.keyboard.press('Enter');
        await expect(trigger).toBeFocused(); await expect(open(child.desc_ur)).toBeVisible();
        await expect(trigger).toHaveAccessibleDescription(/1 áreas dependientes.*40%/);
        assert.equal(await trigger.evaluate(el => getComputedStyle(el).transitionDuration), '0s');
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        await page.screenshot({ path: path.join(output, 'inicio-movil-expandido.png'), fullPage: true });
        denyCollaborators = true; personal = true; await refresh.click();
        await expect(page.getByRole('button', { name: 'Colaboradores', exact: true })).toHaveCount(0);
        await expect(page.getByRole('heading', { name: 'Formatos de mis áreas' })).toBeVisible();
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
        await page.screenshot({ path: path.join(output, 'inicio-personal-movil.png'), fullPage: true });
        report.checks.push('Cálculos, conteo único, tipo N, tipo 0, claves 06000/6000, expansión, formato sin dependientes, ruta, búsqueda/vacío, filtros, pestañas y paginación conservados.');
        report.checks.push('Actualizar con carga, Colaboradores condicionado por prop, vista personal, teclado y movimiento reducido. No verifica autorización del servidor.');
    }
    assert.deepEqual(report.errors, []);
    report.passed = true;
    console.log(JSON.stringify({ passed: true, output, checks: report.checks }));
} finally {
    await writeFile(path.join(output, 'verification.json'), JSON.stringify(report, null, 2));
    await browser?.close(); await new Promise(resolve => server.close(resolve));
}
