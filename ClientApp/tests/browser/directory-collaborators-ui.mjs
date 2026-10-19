// Bundle real en Edge; identidades, listados y escrituras sintéticos. Sin bases ni Nexo.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';

const phase = process.argv[2] || 'after';
const output = path.resolve(process.env.TDV2_VIEWS_ARTIFACTS || '../.artifacts/views-revert-20261016', phase);
await mkdir(output, { recursive: true });
const fixture = await systemFixture();
const unit = (id, code, name, level = 2, parent = null) => ({ id_ur: id, cve_ur: code, desc_ur: name,
    nivel_ur: level, id_ur_pertenece: parent, ejercicio: 2026, tipo_ur: '0' });
const root = unit('A', '023000', 'Dirección General de Tecnologías de la Información');
const child = unit('A3', '023100', 'Subdirección de Desarrollo de Sistemas y Servicios de Información Institucional', 3, 'A');
const otherChild = unit('A4', '023200', 'Subdirección de Infraestructura', 3, 'A');
const units = [root, child, otherChild, ...Array.from({ length: 9 }, (_, i) => unit('R' + i, '04' + i + '00', 'Dirección de prueba ' + i))];
const forms = units.map((u, i) => ({ ...u, porcentaje: i === 1 || i === 2 ? 100 : 20, editable: i < 2,
    propia: i < 3, enviado_en: i === 2 ? '2026-10-15T12:00:00Z' : null, url: '/formatos/' + u.id_ur }));
const person = { nombre: 'María Ángeles Núñez de la Fuente', email: 'persona@example.test', id_ur: 'A3',
    adscripcion: child.desc_ur, formato: child.desc_ur };
const initial = [
    { key: 'central:1:A3', id: null, origen: 'central', tipo: 'local', id_ur_alcance: 'A3', ur_otorgante: null, puedeRetirar: false },
    { key: 'aplicacion:2:A3', id: 2, origen: 'aplicacion', tipo: 'local', id_ur_alcance: 'A3', ur_otorgante: 'A3', puedeRetirar: false },
    { key: 'aplicacion:3:A', id: 3, origen: 'aplicacion', tipo: 'dependencias', id_ur_alcance: 'A', ur_otorgante: 'A', puedeRetirar: true },
].map(c => ({ ...c, nombre: person.nombre, email: person.email, revocada: false, pendiente: false,
    alcance: c.id_ur_alcance === 'A' ? `${root.cve_ur} · ${root.desc_ur}` : `${child.cve_ur} · ${child.desc_ur}` }));
let assigned = structuredClone(initial), level = 2, readOnly = false, personal = false, searchFailure = false, addFailure = false, pendingRemoval = false;
const writes = [], searches = [];
const common = () => ({ auth: { user: { name: 'Responsable sintético', email: 'responsable@example.test' }, roles: [], modules: [], canPreview: false, canRepresent: false },
    simulacion: null, representacion: readOnly ? { escritura: false, nombre: 'Consulta sintética' } : null,
    contextoEdicion: 'own', csrfToken: 'synthetic', session: { lifetime_ms: 7200000 },
    routes: { inicio: '/inicio', home: '/', connect: '/connect', logout: '/logout' } });
fixture.pages = {
    '/inicio': () => ({ component: 'Inicio', url: '/inicio', props: { ...common(), formatos: [...(personal ? forms.slice(0, 3) : forms), forms[0]],
        administrador: !personal, consultaInstitucional: !personal, directorio: units, urAdministracion: root, puedeColaboradores: true } }),
    '/colaboradores': () => ({ component: 'Colaboradores', url: '/colaboradores', props: { ...common(),
        unidades: level === 2 ? [root, child] : [child], tipos: level === 2 ? ['local', 'dependencias'] : ['local'],
        avisoDelegacion: null, colaboraciones: assigned.filter(c => level === 2 || c.tipo === 'local').map(c => ({ ...c,
            puedeRetirar: !readOnly && c.ur_otorgante === (level === 2 ? 'A' : 'A3') })),
        avisoAsignaciones: '' } }),
};
fixture.handleRequest = async (req, url, json) => {
    if (url.pathname === '/colaboradores/personas') {
        searches.push(Object.fromEntries(url.searchParams));
        if (searchFailure) json({ message: 'Búsqueda no disponible en esta prueba.' }, 503);
        else json({ personas: url.searchParams.get('q') === 'nadie' ? [] : [person], hayMas: url.searchParams.get('page') === '1' });
        return true;
    }
    if (url.pathname === '/colaboradores' && req.method === 'POST') {
        let data = ''; for await (const chunk of req) data += chunk;
        const body = JSON.parse(data); writes.push({ method: 'POST', body });
        if (addFailure) json({ message: 'No fue posible confirmar la asignación de prueba.' }, 409);
        else { const id = 10 + assigned.length; const scope = body.tipo === 'local' ? child : root;
            assigned.push({ ...initial[0], key: `aplicacion:${id}:${scope.id_ur}`, id, origen: 'aplicacion', tipo: body.tipo,
                id_ur_alcance: scope.id_ur, ur_otorgante: body.ur, alcance: `${scope.cve_ur} · ${scope.desc_ur}` });
            json({ message: 'Colaborador agregado.' }, 201); }
        return true;
    }
    if (url.pathname.startsWith('/colaboradores/') && req.method === 'DELETE') {
        const id = Number(url.pathname.split('/').at(-1)); writes.push({ method: 'DELETE', id });
        if (pendingRemoval) { assigned = assigned.map(c => c.id === id ? { ...c, revocada: true, pendiente: true } : c);
            json({ message: 'Retiro pendiente de confirmar.' }, 202); }
        else { assigned = assigned.filter(c => c.id !== id); json({ message: 'Colaboración retirada.' }); }
        return true;
    }
    return false;
};
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
const page = await context.newPage(); page.setDefaultTimeout(12000);
const report = { phase, transport: 'simulado', database: false, checks: [], fonts: [], errors: [] };
page.on('pageerror', e => report.errors.push(e.message));
const shot = name => page.screenshot({ path: path.join(output, name + '.png'), fullPage: true });
const check = async (name, fn) => { await fn(); report.checks.push(name); console.log('PASS ' + name); };
const noOverflow = async () => assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
async function renderedFont(selector, label) {
    await page.locator(selector).first().waitFor({ state: 'attached' });
    await page.evaluate(() => document.fonts.ready);
    const cdp = await context.newCDPSession(page); await cdp.send('DOM.enable'); await cdp.send('CSS.enable');
    const { root } = await cdp.send('DOM.getDocument');
    const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector });
    const actual = (await cdp.send('CSS.getPlatformFontsForNode', { nodeId })).fonts;
    report.fonts.push({ label, actual }); await cdp.detach();
    assert.ok(actual.some(f => f.isCustomFont && f.familyName.includes('Roboto')), JSON.stringify(actual));
}
async function searchAndSelect() {
    await page.getByRole('textbox', { name: 'Buscar persona', exact: true }).fill('María');
    const result = page.getByRole('list', { name: 'Personas disponibles' }).getByRole('button');
    await expect(result).toBeVisible(); await result.focus(); await page.keyboard.press('Enter');
    await expect(page.getByRole('button', { name: 'Agregar colaborador', exact: true })).toBeVisible();
}
try {
    for (const width of [1440, 768, 390, 320]) {
        await page.setViewportSize({ width, height: 1000 });
        await page.goto(fixture.origin + '/inicio'); await expect(page.locator('.area-root-trigger').first()).toBeVisible();
        await noOverflow(); await shot(`procedimientos-cerrados-${width}`);
        await page.getByRole('button', { name: 'Expandir visibles', exact: true }).click();
        await noOverflow(); await shot(`procedimientos-abiertos-${width}`);
        await page.goto(fixture.origin + '/colaboradores'); await expect(page.locator('[data-assignment]')).toHaveCount(3);
        await noOverflow(); await shot(`colaboradores-inicial-${width}`);
        await searchAndSelect(); await noOverflow(); await shot(`colaboradores-seleccion-${width}`);
    }
    await renderedFont('h1', 'Colaboradores');
    if (phase === 'after') {
        await check('Directorio restaurado: nombre principal, clave secundaria, estados y formatos únicos', async () => {
            await page.setViewportSize({ width: 1440, height: 1000 }); await page.goto(fixture.origin + '/inicio');
            const stats = page.locator('.operational-stat-value');
            await expect(stats.nth(0)).toHaveText(String(forms.length));
            await expect(stats.nth(1)).toHaveText(`${Math.floor(forms.reduce((s, f) => s + f.porcentaje, 0) / forms.length)}%`);
            await expect(stats.nth(2)).toHaveText('2');
            await expect(page.getByText(/Resumen institucional/)).toHaveCount(0);
            await page.getByRole('tab', { name: 'Mis áreas', exact: true }).click();
            const rootButton = page.locator('.area-root-trigger').first();
            await expect(rootButton.locator('.MuiTypography-body2')).toHaveText(root.desc_ur);
            await expect(rootButton.locator('.MuiTypography-caption').first()).toHaveText('23000 · 2 áreas dependientes · 2/3 completos');
            await rootButton.focus(); await page.keyboard.press('Space');
            assert.equal(await rootButton.evaluate(el => getComputedStyle(el).outlineWidth), '2px');
            await expect(rootButton).toHaveAttribute('aria-expanded', 'true');
            const rows = page.locator('.area-format-row');
            await expect(rows.nth(0)).toContainText('Formato del área principal');
            await expect(rows.nth(1).locator('.MuiTypography-body2')).toHaveText(child.desc_ur);
            await expect(rows.nth(1).locator('.MuiTypography-caption').first()).toHaveText('23100 · Captura completa');
            await expect(rows.nth(1)).toContainText('Captura completa');
            await expect(rows.nth(2)).toContainText('Enviado');
            await expect(rows.nth(1).getByRole('button')).toHaveText('Ver formato');
            await expect(rows.nth(1).getByRole('button')).toHaveClass(/MuiButton-contained/);
            await expect(rows.nth(2).getByRole('button')).toHaveClass(/MuiButton-outlined/);
            await renderedFont('.area-root-trigger .MuiTypography-body2', 'Procedimientos');
        });
        await check('Búsqueda, filtro, pestañas, expansión, paginación y acceso', async () => {
            await page.getByRole('tab', { name: 'Mis áreas', exact: true }).click();
            await expect(page.locator('.area-root-trigger')).toHaveCount(1);
            await page.getByRole('button', { name: 'Expandir visibles' }).click();
            await expect(page.locator('.area-format-row').nth(1)).toContainText(child.desc_ur);
            await page.getByRole('button', { name: 'Contraer', exact: true }).click();
            await expect(page.locator('.area-root-trigger').first()).toHaveAttribute('aria-expanded', 'false');
            await expect(page.locator('.area-format-row:visible')).toHaveCount(0);
            await page.getByRole('button', { name: 'Expandir visibles' }).click();
            await page.getByRole('textbox', { name: 'Buscar área o clave' }).fill('023100');
            await expect(page.getByRole('button', { name: `Abrir formato de ${child.desc_ur}`, exact: true })).toBeVisible();
            await page.getByRole('button', { name: 'Limpiar búsqueda' }).click();
            await page.getByRole('combobox', { name: 'Mostrar' }).click(); await page.getByRole('option', { name: 'Completos', exact: true }).click();
            await expect(page.locator('.area-format-row')).toHaveCount(2);
            await page.getByRole('combobox', { name: 'Mostrar' }).click(); await page.getByRole('option', { name: 'Todas las áreas', exact: true }).click();
            await page.getByRole('tab', { name: 'Todas las áreas', exact: true }).click();
            await expect(page.locator('.area-root-trigger')).toHaveCount(8);
            await page.getByRole('button', { name: /page 2/i }).click(); await expect(page.locator('.area-root-trigger')).toHaveCount(2);
            await page.getByRole('tab', { name: 'Mis áreas', exact: true }).click();
            await page.getByRole('button', { name: 'Expandir visibles' }).click();
            await page.getByRole('button', { name: `Abrir formato de ${child.desc_ur}`, exact: true }).click();
            await expect(page).toHaveURL(fixture.origin + '/formatos/A3');
        });
        await check('Vista personal restaurada: clave debajo, estados y acciones anteriores', async () => {
            personal = true; await page.goto(fixture.origin + '/inicio');
            await expect(page.locator('.personal-area-row')).toHaveCount(3);
            const row = page.locator('.personal-area-row').nth(1);
            await expect(row.locator('.MuiTypography-h3')).toHaveText(child.desc_ur);
            await expect(row.locator('.MuiTypography-caption').first()).toHaveText('23100');
            await expect(row.getByRole('button')).toHaveText('Continuar llenado');
            await expect(page.locator('.personal-area-row').nth(2).getByRole('button')).toHaveText('Ver formato');
            await expect(page.getByText(/Resumen de tus formatos/)).toHaveCount(0);
            await page.setViewportSize({ width: 320, height: 900 }); await noOverflow(); await shot('procedimientos-personal-320'); personal = false;
        });
        await check('Lista restaurada: asignaciones distintas, etiquetas originales, sin origen y acciones autorizadas', async () => {
            await page.setViewportSize({ width: 1440, height: 1000 }); await page.goto(fixture.origin + '/colaboradores');
            await expect(page.locator('[data-assignment]')).toHaveCount(3);
            assert.doesNotMatch(await page.locator('.page').innerText(), /Origen|delegación en TDV2|Agregado por Administrador/);
            await expect(page.locator('[data-assignment="central:1:A3"] .MuiChip-label')).toHaveText('Local');
            await expect(page.locator('[data-assignment="aplicacion:3:A"] .MuiChip-label')).toHaveText('Áreas dependientes');
            await expect(page.locator('.collaboration-head, .collaboration-fields, .collaborator-columns')).toHaveCount(0);
            await expect(page.locator('[data-assignment="central:1:A3"]').getByRole('button')).toHaveCount(0);
            await expect(page.locator('[data-assignment="aplicacion:2:A3"]').getByRole('button')).toHaveCount(0);
        });
        await check('Nivel 2: distribución original, resumen, alta local y dependencias, error sin pérdida', async () => {
            await searchAndSelect();
            const search = await page.getByRole('textbox', { name: 'Buscar persona' }).boundingBox();
            const results = await page.getByRole('list', { name: 'Personas disponibles' }).boundingBox();
            const summary = page.locator('section').filter({ has: page.getByRole('heading', { name: '2. Define el alcance', exact: true }) });
            const summaryBox = await summary.boundingBox();
            const area = await page.getByRole('combobox', { name: 'Área que autoriza' }).boundingBox();
            assert.ok(Math.abs(area.y - search.y) < 5 && search.x > area.x + area.width);
            assert.ok(results.y > search.y && summaryBox.x > results.x + results.width);
            await page.getByRole('radio', { name: 'Colaborador local', exact: false }).check(); addFailure = true;
            await page.getByRole('button', { name: 'Agregar colaborador', exact: true }).click();
            await expect(page.getByRole('alert')).toContainText('No fue posible confirmar');
            await expect(summary).toContainText(person.nombre);
            addFailure = false; await page.getByRole('button', { name: 'Agregar colaborador', exact: true }).click();
            await expect(page.locator('[data-assignment]')).toHaveCount(4);
            assert.deepEqual(writes.at(-1).body, { ur: 'A', email: person.email, id_ur: 'A3', tipo: 'local' });
            await searchAndSelect(); await page.getByRole('radio', { name: 'Colaborador de áreas dependientes', exact: false }).check();
            await expect(summary).toContainText(root.desc_ur);
            await page.getByRole('button', { name: 'Agregar colaborador', exact: true }).click();
            await expect(page.locator('[data-assignment]')).toHaveCount(5); assert.equal(writes.at(-1).body.tipo, 'dependencias');
        });
        await check('Retiro: confirmar asignación exacta, Escape, pendiente y reintento sin afectar otras', async () => {
            const row = page.locator('[data-assignment="aplicacion:3:A"]'); const remove = row.getByRole('button');
            await remove.focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('dialog')).toContainText(root.desc_ur);
            const before = writes.length; await page.keyboard.press('Escape'); await expect(remove).toBeFocused(); assert.equal(writes.length, before);
            pendingRemoval = true; await remove.click(); await page.getByRole('button', { name: 'Retirar colaboración', exact: true }).click();
            await expect(row).toContainText('Retiro pendiente de confirmar');
            pendingRemoval = false; await row.getByRole('button', { name: /Retirar/ }).click();
            await page.getByRole('button', { name: 'Retirar colaboración', exact: true }).click();
            await expect(row).toHaveCount(0); assert.equal(writes.at(-1).id, 3); await expect(page.locator('[data-assignment]')).toHaveCount(4);
        });
        await check('Nivel 3: sólo rol local, asignaciones de su área y retiro del otorgante', async () => {
            level = 3; assigned = structuredClone(initial); await page.goto(fixture.origin + '/colaboradores');
            await expect(page.locator('[data-assignment]')).toHaveCount(2); await searchAndSelect();
            await expect(page.getByRole('radio', { name: /dependencias/ })).toHaveCount(0);
            await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: '2. Define el alcance', exact: true }) })).toContainText(child.desc_ur);
            await page.getByRole('button', { name: 'Agregar colaborador', exact: true }).click();
            await expect(page.locator('[data-assignment]')).toHaveCount(3);
            assert.deepEqual(writes.at(-1).body, { ur: 'A3', email: person.email, id_ur: 'A3', tipo: 'local' });
            await page.locator('[data-assignment="aplicacion:2:A3"]').getByRole('button').click();
            await page.getByRole('button', { name: 'Retirar colaboración', exact: true }).click();
            await expect(page.locator('[data-assignment="aplicacion:2:A3"]')).toHaveCount(0);
        });
        await check('Búsqueda paginada, sin resultados y error; representación de consulta sin escritura', async () => {
            await searchAndSelect(); await page.getByRole('button', { name: 'Siguiente', exact: true }).click();
            await expect(page.getByText('Página 2', { exact: true })).toBeVisible(); assert.equal(searches.at(-1).ur, 'A3');
            await page.getByRole('textbox', { name: 'Buscar persona' }).fill('nadie'); await expect(page.getByText('No encontramos personas disponibles.')).toBeVisible();
            searchFailure = true; await page.getByRole('textbox', { name: 'Buscar persona' }).fill('error');
            await expect(page.getByRole('alert')).toContainText('Búsqueda no disponible');
            readOnly = true; await page.goto(fixture.origin + '/colaboradores');
            await expect(page.getByRole('textbox', { name: 'Buscar persona' })).toBeDisabled();
            await expect(page.locator('[data-assignment] button')).toHaveCount(0);
            await page.setViewportSize({ width: 390, height: 844 }); await noOverflow(); await shot('colaboradores-consulta-390');
        });
        await check('Roboto real en 400/500/600/700 y español; controles, menú, diálogos, móvil y zoom', async () => {
            readOnly = false; level = 2; searchFailure = false; assigned = structuredClone(initial);
            await page.setViewportSize({ width: 1440, height: 1000 }); await page.goto(fixture.origin + '/colaboradores');
            for (const weight of [400, 500, 600, 700]) {
                await page.evaluate(w => { const e = document.createElement('p'); e.id = 'font-probe';
                    e.textContent = 'ÁÉÍÓÚ áéíóú Ññ ¿¡ ü'; e.style.fontWeight = w; document.body.append(e); }, String(weight));
                await renderedFont('#font-probe', `español-${weight}`); await page.locator('#font-probe').evaluate(el => el.remove());
            }
            await renderedFont('label', 'Etiqueta de formulario');
            assert.equal(await page.getByRole('textbox', { name: 'Buscar persona' }).evaluate(el => getComputedStyle(el).fontFamily), 'Roboto, sans-serif');
            await page.getByRole('combobox', { name: 'Área que autoriza' }).click();
            await renderedFont('[role="option"]', 'Menú MUI'); await page.keyboard.press('Escape');
            await page.locator('[data-assignment="aplicacion:3:A"]').getByRole('button').click();
            await renderedFont('.MuiDialogTitle-root', 'Confirmación MUI'); await page.keyboard.press('Escape');
            for (const width of [1440, 768, 390, 320]) {
                await page.setViewportSize({ width, height: 1000 }); await page.goto(fixture.origin + '/inicio');
                await page.getByRole('tab', { name: 'Mis áreas', exact: true }).click();
                await page.getByRole('button', { name: 'Expandir visibles' }).click();
                await expect(page.getByText(child.desc_ur, { exact: true })).toBeVisible();
                await noOverflow(); await shot(`procedimientos-nombres-completos-${width}`);
            }
            await page.setViewportSize({ width: 1440, height: 1000 });
            await page.evaluate(() => document.documentElement.style.zoom = '2');
            await noOverflow(); await shot('procedimientos-zoom-200');
            await page.goto(fixture.origin + '/colaboradores'); await searchAndSelect();
            await page.evaluate(() => document.documentElement.style.zoom = '2');
            await noOverflow(); await shot('colaboradores-zoom-200');
            await page.evaluate(() => document.documentElement.style.zoom = '');
            await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: '2. Define el alcance', exact: true }) })).toContainText(person.nombre);
        });
    }
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (error) { report.failure = error.stack; await shot('failure'); throw error; }
finally { await writeFile(path.join(output, 'report.json'), JSON.stringify(report, null, 2)); await context.close(); await browser.close(); await fixture.close(); }
