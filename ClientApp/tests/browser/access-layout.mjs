// Interfaz React/MUI real con identidad, transporte y auditoría simulados; no acredita permisos del backend.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';
const fixture = await systemFixture(), output = path.resolve('../.artifacts/access-layout-20261010');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, hasTouch: true, reducedMotion: 'reduce' });
await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
const page = await context.newPage(); page.setDefaultTimeout(12000);
const report = { synthetic: true, database: false, aspNet: false, mockedTransport: true, checks: [], errors: [] };
page.on('pageerror', error => report.errors.push(error.message));
const check = async (name, work) => { await work(); report.checks.push(name); console.log('UI PASS ' + name); };
const prefix = '/configuracion/pruebas-acceso';
const units = [{ id_ur: 'A', cve_ur: '06000', desc_ur: 'Área sintética de Tecnologías con nombre extenso para probar búsqueda y lectura', nivel_ur: 2, ejercicio: 2026 },
    { id_ur: 'B', cve_ur: '07000', desc_ur: 'Biblioteca sintética', nivel_ur: 3, ejercicio: 2026 }];
const person = { usuario_id: 2, nombre: 'Persona autorizada sintética', email: 'persona@example.test', id_ur: 'A', desc_ur: units[0].desc_ur, roles: [{ clave: 'responsable', nombre: 'Responsable del área' }] };
let preview = null, representation = null, serial = 0, delaySearch = null, activationError = false;
const changes = [];
const shared = () => ({ auth: { user: { id: 1, name: 'Administradora sintética', email: 'admin@example.test' }, roles: [], modules: [], canPreview: true, canRepresent: true },
    simulacion: preview, representacion: representation, contextoEdicion: preview || representation ? 'context-' + serial : 'own', csrfToken: 'synthetic', session: { lifetime_ms: 7200000 }, routes: { inicio: '/inicio', home: '/', connect: '/connect', logout: '/logout' } });
const capability = { permitido: true, escritura: true, alcance: 'rama_ur', duracion_minutos: 30 };
const buildPage = (component, url, props) => ({ component, url, props: { ...shared(), ...props } });
fixture.pages = {
    [prefix]: () => buildPage('PruebasAcceso', prefix, { puedeVistaPrueba: true, capacidad: capability }),
    [prefix + '/rol-area']: () => buildPage('VistaPrueba', prefix + '/rol-area', { unidades: units, seleccion: null, roles: [{ value: 'responsable', title: 'Responsable del área' }, { value: 'consulta', title: 'Consulta institucional' }] }),
    [prefix + '/actuar-como-usuario']: () => buildPage('ActuarComoUsuario', prefix + '/actuar-como-usuario', { capacidad: capability }),
    '/inicio': () => buildPage('Inicio', '/inicio', { formatos: [], directorio: [], administrador: false, puedeColaboradores: false, urAdministracion: null, sincronizadoEn: null }),
};
fixture.handleRequest = async (req, url, json) => {
    if (url.pathname === '/actuar-como-usuario/personas') {
        if (delaySearch) await delaySearch;
        const q = url.searchParams.get('q');
        json(q === 'error' ? { message: 'No fue posible consultar usuarios. Inténtalo de nuevo.' } : { personas: q === 'vacio' ? [] : [person], hayMas: false }, q === 'error' ? 503 : 200); return true;
    }
    if (!['/vista-prueba', '/actuar-como-usuario'].includes(url.pathname)) return false;
    let raw = ''; for await (const part of req) raw += part; const body = JSON.parse(raw || '{}'); changes.push({ path: url.pathname, method: req.method, body });
    if (req.method === 'DELETE') { preview = null; representation = null; json({ redirect: prefix }); return true; }
    if (activationError) { json({ message: 'El contexto seleccionado ya no está disponible.' }, 409); return true; }
    assert.equal(preview || representation, null); serial++;
    if (url.pathname === '/vista-prueba') preview = { mode: 'escenario', label: 'Escenario', roleLabel: 'Responsable del área', unit: units.find(unit => unit.id_ur === body.ur), readOnly: true, expiresAt: Date.now() + 1800000 };
    else representation = { id: 'synthetic-context', ...person, escritura: body.escritura, expira_en: new Date(Date.now() + 1800000).toISOString() };
    json({ redirect: '/inicio' }); return true;
};
const open = async path => page.goto(fixture.origin + path);
const picture = async name => page.screenshot({ path: path.join(output, name + '.png'), fullPage: true });
const noOverflow = async () => assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
const balanced = async () => {
    const layout = await page.locator('.access-columns').evaluate(el => {
        const [left, right] = [...el.children].map(child => child.getBoundingClientRect());
        const page = el.closest('.page').getBoundingClientRect();
        return { left: { x: left.x, y: left.y, width: left.width }, right: { x: right.x, y: right.y, width: right.width }, center: page.x + page.width / 2, viewport: innerWidth };
    });
    assert.ok(Math.abs(layout.center - layout.viewport / 2) < 2);
    assert.ok(Math.abs(layout.left.width - layout.right.width) < 2);
    if (layout.viewport >= 900) assert.ok(Math.abs(layout.left.y - layout.right.y) < 2 && layout.right.x > layout.left.x);
    else assert.ok(layout.right.y > layout.left.y && Math.abs(layout.right.x - layout.left.x) < 2);
};
try {
    await check('modalidades separadas; rol/área buscable por nombre y clave con resumen antes de aplicar', async () => {
        await open(prefix); await expect(page.getByRole('heading', { name: 'Vista por rol y área', exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'Probar rol y área', exact: true }).click();
        const field = page.getByRole('combobox', { name: 'Área de pertenencia o responsabilidad' });
        await expect(page.getByRole('button', { name: 'Aplicar vista', exact: true })).toBeDisabled();
        await field.fill('6000'); await expect(page.getByRole('option')).toHaveCount(1); await page.keyboard.press('ArrowDown'); await page.keyboard.press('Enter');
        await expect(page.getByText('Contexto por aplicar · Solo lectura', { exact: true })).toBeVisible();
        await expect(page.getByText('Área: 6000 · ' + units[0].desc_ur, { exact: true })).toBeVisible();
        await picture('vista-escritorio'); await noOverflow(); await balanced();
        await page.setViewportSize({ width: 1050, height: 900 }); await noOverflow(); await picture('vista-1050');
        await page.setViewportSize({ width: 1440, height: 1000 });
        activationError = true; await page.getByRole('button', { name: 'Aplicar vista', exact: true }).click();
        await expect(page.getByRole('alert')).toContainText('El contexto seleccionado ya no está disponible.'); await expect(field).toHaveValue('6000 · ' + units[0].desc_ur); activationError = false;
        await page.getByRole('button', { name: 'Aplicar vista', exact: true }).click();
        await expect(page.getByText('Vista por rol y área · Responsable del área', { exact: true })).toBeVisible();
    });
    await check('contexto visible al navegar, cuenta original distinguida y salida antes de cambiar de modalidad', async () => {
        await expect(page.getByText('Cuenta autenticada: admin@example.test', { exact: true })).toBeVisible();
        await open(prefix + '/rol-area'); await expect(page.getByRole('button', { name: 'Aplicar vista', exact: true })).toBeDisabled();
        await page.getByRole('tab', { name: 'Actuar como usuario', exact: true }).click();
        await expect(page.getByRole('button', { name: 'Buscar', exact: true })).toHaveCount(0);
        await expect(page.getByText('Vista por rol y área · Responsable del área', { exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'Salir de la prueba', exact: true }).click();
        await expect(page.getByText('Cuenta autenticada: admin@example.test', { exact: true })).toHaveCount(0);
    });
    await check('personas: carga, sin resultados, error y selección con nombre, correo, área y roles', async () => {
        await page.getByRole('button', { name: 'Seleccionar usuario', exact: true }).click();
        const query = page.getByLabel('Nombre o correo institucional', { exact: true });
        let release; delaySearch = new Promise(resolve => release = resolve);
        await query.fill('vacio'); await page.getByRole('button', { name: 'Buscar', exact: true }).click();
        await expect(page.getByText('Consultando…', { exact: true })).toBeVisible(); release(); delaySearch = null;
        await expect(page.getByText('No hay personas que coincidan dentro del alcance autorizado.', { exact: true })).toBeVisible();
        await query.fill('error'); await page.getByRole('button', { name: 'Buscar', exact: true }).click(); await expect(page.getByRole('alert')).toContainText('No fue posible consultar usuarios.');
        await query.fill('persona'); await page.keyboard.press('Enter');
        const option = page.getByRole('button', { name: /Persona autorizada sintética persona@example.test/ }); await option.focus(); await page.keyboard.press('Enter');
        await expect(option).toHaveAttribute('aria-pressed', 'true');
        await expect(page.getByRole('heading', { name: 'Contexto por activar', exact: true })).toBeVisible();
        await expect(page.getByText('Roles disponibles: Responsable del área', { exact: true })).toBeVisible();
        await page.getByLabel('Motivo de la representación', { exact: true }).fill('Comprobación sintética de interfaz');
        await picture('representacion-escritorio'); await noOverflow(); await balanced();
    });
    await check('activación y salida confirmadas, sin habilitar escritura por defecto; escritorio/móvil/teclado', async () => {
        for (const width of [390, 320]) { await page.setViewportSize({ width, height: 900 }); await noOverflow(); await balanced(); await picture('representacion-' + width); }
        await page.getByRole('button', { name: 'Activar representación', exact: true }).click();
        await expect(page.getByText('Actuando como Persona autorizada sintética', { exact: true })).toBeVisible();
        assert.equal(changes.at(-1).body.escritura, false); await noOverflow(); await picture('contexto-activo-movil');
        await open(prefix + '/rol-area'); await expect(page.getByRole('button', { name: 'Aplicar vista', exact: true })).toBeDisabled(); await noOverflow();
        await page.getByRole('button', { name: 'Volver a mi usuario', exact: true }).click();
        await expect(page.getByText('Actuando como Persona autorizada sintética', { exact: true })).toHaveCount(0);
        await open(prefix + '/rol-area'); const field = page.getByRole('combobox', { name: 'Área de pertenencia o responsabilidad' });
        await field.fill('Biblioteca'); await page.keyboard.press('ArrowDown'); await page.keyboard.press('Enter');
        await noOverflow(); await picture('vista-movil');
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (error) { report.failure = error.message; await picture('failure'); throw error; }
finally { await writeFile(path.join(output, 'verification.json'), JSON.stringify(report, null, 2)); await context.close(); await browser.close(); await fixture.close(); }
