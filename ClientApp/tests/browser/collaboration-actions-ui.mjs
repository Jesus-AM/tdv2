// Bundle real en Edge; identidad, asignaciones, reservas y HTTP simulados. Ninguna base ni fuente institucional.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';
const fixture = await systemFixture(), output = path.resolve('../.artifacts/collaboration-actions-20261011');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, hasTouch: true, reducedMotion: 'reduce' });
await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
await fixture.webSockets(context);
const page = await context.newPage(); page.setDefaultTimeout(15000);
const report = { synthetic: true, database: false, aspNet: false, mockedTransport: true, checks: [], errors: [] };
page.on('pageerror', error => report.errors.push(error.message));
const check = async (name, work) => { await work(); report.checks.push(name); console.log('UI PASS ' + name); };
const picture = async name => page.screenshot({ path: path.join(output, name + '.png'), fullPage: true });
const noOverflow = async () => assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
const tab = async name => { await page.getByRole('tab', { name, exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); };
const common = { auth: { user: { id: 1, name: 'Responsable sintético', email: 'responsable@example.test' }, roles: [], modules: [], canPreview: false, canRepresent: false },
    simulacion: null, representacion: null, contextoEdicion: 'own', csrfToken: 'synthetic', session: { lifetime_ms: 7200000 }, routes: { inicio: '/inicio', home: '/', connect: '/connect', logout: '/logout' } };
const link = { nombre: 'Persona autorizada con un nombre extenso para revisar la adaptación de las asignaciones', email: 'persona@example.test',
    id_ur_alcance: 'A3', alcance: '110 · Área asignada de servicios institucionales con nombre extenso', tipo: 'local', revocada: false, pendiente: false, ur_otorgante: 'A3' };
const assignments = [{ ...link, key: 'central:1:A3', id: null, origen: 'central', puedeRetirar: false },
    { ...link, key: 'aplicacion:2:A3', id: 2, origen: 'aplicacion', puedeRetirar: false },
    { ...link, key: 'aplicacion:3:A', id: 3, origen: 'aplicacion', puedeRetirar: true, tipo: 'dependencias', id_ur_alcance: 'A', ur_otorgante: 'A', alcance: '100 · Área principal' }];
fixture.pages = { '/colaboradores': () => ({ component: 'Colaboradores', url: '/colaboradores', props: { ...common, unidades: [], tipos: [], avisoDelegacion: null,
    avisoAsignaciones: 'Sólo se muestran asignaciones cuyo rol y área pueden comprobarse. Si faltan esos datos, no se presume alcance institucional.', colaboraciones: assignments } }) };
fixture.content.sistemas[0] = { ...fixture.content.sistemas[0], proceso: 'PO-01', sistema: 'excel', uso: 'consultar', estado: 'bien' };
fixture.delivery = () => ({ etapa: { id: 1, nombre: 'Primera etapa', habilitada: true, estado: fixture.submitted ? 'enviada' : 'borrador', ejercicio: 2026, enviadoEn: fixture.submitted ? '2026-10-10T12:00:00Z' : null, enviadoPor: 'persona@example.test' },
    revision: { listo: true, pendientes: [] }, secciones: [{ id: 'contexto', nombre: 'Contexto', completa: true }, { id: 'identificacion', nombre: 'Identificación general', completa: true }, { id: 'sistemas', nombre: 'Sistemas y herramientas', completa: true }],
    resumen: { procedimientos: 3, herramientas: 1, medios: [] } });
try {
    await check('Colaboradores muestra Agregado por Administrador sin revelar el origen; conserva asignaciones y acciones', async () => {
        await page.goto(fixture.origin + '/colaboradores');
        await expect(page.locator('[data-assignment]')).toHaveCount(3);
        const central = page.locator('[data-assignment="central:1:A3"]');
        await expect(central.getByText('Agregado por Administrador', { exact: true })).toBeVisible();
        await expect(central).toContainText('Solo consulta.');
        await expect(central.getByRole('button')).toHaveCount(0);
        await expect(page.locator('[data-assignment="aplicacion:2:A3"]')).toContainText('Origen: delegación en TDV2');
        await expect(page.locator('[data-assignment="aplicacion:2:A3"]')).toContainText('El retiro corresponde al área que otorgó la asignación.');
        const forbiddenOrigin = /nexo|origen central|externo|sincronizado/i;
        assert.doesNotMatch(await page.locator('.page').innerText(), forbiddenOrigin);
        const accessibleText = await page.locator('.page').evaluate(el => [...el.querySelectorAll('[title], [aria-label]')]
            .map(item => [item.getAttribute('title'), item.getAttribute('aria-label')].join(' ')).join('\n'));
        assert.doesNotMatch(accessibleText, forbiddenOrigin);
        await central.getByText('Agregado por Administrador', { exact: true }).hover();
        await expect(page.getByRole('tooltip')).toHaveCount(0);
        await expect(page.locator('[data-assignment="aplicacion:2:A3"]').getByRole('button')).toHaveCount(0);
        const remove = page.locator('[data-assignment="aplicacion:3:A"]').getByRole('button');
        await remove.focus(); await page.keyboard.press('Enter'); await expect(page.getByRole('dialog')).toBeVisible();
        await expect(page.getByRole('dialog')).toContainText(link.nombre);
        await expect(page.getByRole('dialog')).toContainText('100 · Área principal');
        assert.doesNotMatch(await page.getByRole('dialog').innerText(), forbiddenOrigin);
        await page.keyboard.press('Escape'); await picture('colaboradores-escritorio');
        for (const width of [390, 320]) {
            await page.setViewportSize({ width, height: 900 }); await noOverflow();
            await central.getByText('Agregado por Administrador', { exact: true }).tap();
            await expect(page.getByRole('tooltip')).toHaveCount(0);
            await picture('colaboradores-' + width);
        }
        assert.equal(fixture.requests.length, 0);
    });
    await check('Guardar secundario conserva la última captura sin enviar; doble clic no duplica el guardado', async () => {
        await page.setViewportSize({ width: 1440, height: 1000 }); await page.goto(fixture.origin + '/formatos/A'); await tab('Sistemas y herramientas');
        const comments = page.getByRole('textbox', { name: 'Fallas o comentarios 1', exact: true });
        let release; fixture.waitSave = new Promise(resolve => release = resolve);
        await comments.click(); await expect(comments).toBeEditable(); await comments.fill('Última captura sintética antes de enviar.');
        const save = page.getByRole('button', { name: 'Guardar', exact: true }); await expect(save).toHaveClass(/MuiButton-outlined/);
        await save.dblclick(); await expect(save).toHaveAttribute('aria-busy', 'true'); await expect(save).toContainText('Guardando…');
        assert.equal(fixture.submitted, false); assert.equal(fixture.requests.filter(r => r.path.endsWith('/enviar')).length, 0);
        release(); await expect(save).toHaveAttribute('aria-busy', 'false');
        assert.equal(fixture.content.sistemas[0].fallas, 'Última captura sintética antes de enviar.');
        assert.equal(fixture.requests.filter(r => r.path.endsWith('/bloques')).length, 1);
        await page.reload(); await tab('Sistemas y herramientas'); await expect(comments).toHaveValue('Última captura sintética antes de enviar.');
    });
    await check('Guardar antes de Enviar, altura consistente, azul/borde y teclado en escritorio/móvil', async () => {
        await tab('Revisar y enviar');
        for (const width of [1440, 390, 320]) {
            await page.setViewportSize({ width, height: 900 });
            const save = page.getByRole('button', { name: 'Guardar', exact: true }), send = page.getByRole('button', { name: 'Enviar formato', exact: true });
            await expect(send).toHaveClass(/MuiButton-contained/); await expect(send).toHaveClass(/MuiButton-colorPrimary/);
            await expect(send).toBeEnabled(); await expect(save).toHaveClass(/MuiButton-outlined/);
            const rgb = await send.evaluate(el => getComputedStyle(el).backgroundColor.match(/[\d.]+/g).map(Number)); assert.ok(rgb[2] > rgb[0]);
            const a = await save.boundingBox(), b = await send.boundingBox();
            assert.ok(Math.abs(a.height - b.height) < 1 && (b.y > a.y || b.x > a.x)); await noOverflow();
            await save.focus(); await page.keyboard.press('Tab'); await expect(send).toBeFocused(); await picture('acciones-' + width);
        }
    });
    await check('Cambio remoto invalida confirmación; envío usa versión reciente y sólo confirma tras ACK, sin duplicación', async () => {
        await page.getByRole('button', { name: 'Enviar formato', exact: true }).click(); await expect(page.getByRole('dialog')).toBeVisible();
        fixture.version++; fixture.broadcast(); await expect(page.getByRole('button', { name: 'Confirmar envío', exact: true })).toBeDisabled();
        await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
        await page.getByRole('button', { name: 'Enviar formato', exact: true }).click();
        const current = fixture.version; let release; fixture.waitSubmit = new Promise(resolve => release = resolve);
        await page.getByRole('button', { name: 'Confirmar envío', exact: true }).dblclick();
        await expect(page.getByRole('button', { name: 'Enviando…', exact: true })).toBeDisabled();
        const requests = fixture.requests.filter(r => r.path.endsWith('/enviar')); assert.equal(requests.length, 1); assert.equal(requests[0].body.version, current);
        await expect(page.getByText('Formato enviado', { exact: true })).toHaveCount(0);
        release(); await expect(page.getByText('Formato enviado', { exact: true })).toBeVisible(); await picture('envio-confirmado');
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (error) { report.failure = error.message; await picture('failure'); throw error; }
finally { await writeFile(path.join(output, 'verification.json'), JSON.stringify(report, null, 2)); await context.close(); await browser.close(); await fixture.close(); }
