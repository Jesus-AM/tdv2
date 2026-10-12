// Bundle React real; respuestas, reservas, SignalR y sincronizaciones simulados en memoria. Sin bases ni autenticación real.
import assert from 'node:assert/strict';
import { chromium, expect } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { systemFixture } from './system-ui-fixture.mjs';
const fixture = await systemFixture(), output = path.resolve('../.artifacts/interface-layout-20261010');
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, hasTouch: true, reducedMotion: 'reduce' });
await fixture.webSockets(context);
await context.route('**/*', route => new URL(route.request().url()).origin === fixture.origin ? route.continue() : route.abort());
const page = await context.newPage(); page.setDefaultTimeout(15000);
const report = { synthetic: true, database: false, aspNet: false, mockedTransport: true, checks: [], errors: [], screenshots: [] };
page.on('pageerror', error => report.errors.push(error.message));
const check = async (name, work) => { await work(); report.checks.push(name); console.log('UI PASS ' + name); };
const picture = async name => { await page.screenshot({ path: path.join(output, name + '.png'), fullPage: true }); report.screenshots.push(name); };
const overflow = async () => assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
const tab = async name => { await page.getByRole('tab', { name, exact: true }).click(); await expect(page.getByLabel('Cargando estado de colaboración')).toHaveCount(0); };
const finish = async () => { await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await expect(page.locator('[data-edit-state="owned"]')).toHaveCount(0); };
const row = () => page.locator('[data-edit-block="sistemas:inicial"]');
const common = { auth: { user: { id: 1, name: 'Persona sintética', email: 'persona@example.test' }, roles: [], modules: [], canPreview: false, canRepresent: false },
    simulacion: null, representacion: null, contextoEdicion: 'own', csrfToken: 'synthetic', session: { lifetime_ms: 7200000 }, routes: { inicio: '/inicio', home: '/', connect: '/connect', logout: '/logout' } };
const area = (id, parent = null) => ({ id_ur: id, cve_ur: id, desc_ur: `Área sintética ${id}`, nivel_ur: parent ? 3 : 2, id_ur_pertenece: parent, tipo_ur: '0', ejercicio: 2026 });
const forms = Array.from({ length: 8 }, (_, i) => ({ ...area(String(i + 100)), porcentaje: i * 10, editable: i === 0, propia: i === 0, url: '/formatos/A' }));
forms.push({ ...area('1011', '101'), porcentaje: 100, editable: false, propia: false, url: '/formatos/A' });
forms[2].desc_ur = 'Área con nombre extenso para comprobar relaciones, alineación y lectura en todas las pantallas';
let settings = { version: 1, activa: false, intervalo_minutos: 60, hora: '02:00', zona_horaria: 'America/Ciudad_Juarez', incluir_ilda: false };
let status = { ejecucion_activa: null, proxima_en: null, procesador_visto_en: null, procesador_reciente: false, ilda_habilitada: true };
let catalogs = { sii: { registros: 31, completada_en: '2026-10-08T12:00:00Z' }, sii_modulos: { registros: 1103, completada_en: '2026-10-07T12:00:00Z' }, ilda: { registros: 29, completada_en: '2026-10-06T12:00:00Z' } };
const execution = changes => ({ id: 'a', fuentes: 'ambas', origen: 'manual', solicitado_por: 'persona@example.test', estado: 'parcial', etapa: null,
    solicitada_en: '2026-10-09T12:00:00Z', iniciada_en: '2026-10-09T12:00:01Z', terminada_en: '2026-10-09T12:01:00Z', resultado: {}, ...changes });
let history = [execution({ resultado: { sii: { estado: 'completada', resumen: { unidades: 31 } }, sii_modulos: { estado: 'fallida', mensaje: 'No fue posible sincronizar los módulos. Se conservó la copia anterior.' }, ilda: { estado: 'completada', resumen: { registros: 29 } } } })];
fixture.pages = {
    '/inicio': () => ({ component: 'Inicio', url: '/inicio', props: { ...common, formatos: forms, directorio: forms, administrador: true, consultaInstitucional: true, puedeColaboradores: true, urAdministracion: forms[0], sincronizadoEn: null } }),
    '/configuracion/sincronizaciones': () => ({ component: 'Sincronizaciones', url: '/configuracion/sincronizaciones', props: { ...common, configuracion: settings, estado: status, catalogos: catalogs, historial: history } }),
};
fixture.content.identificacion[0].tramite = 'Procedimiento institucional con nombre extenso de atención y seguimiento de solicitudes '.repeat(6);
fixture.content.identificacion[1].tramite = 'Adquisición especializada Beta';
fixture.content.identificacion[0].resultado = '';
fixture.content.sistemas[0] = { ...fixture.content.sistemas[0], proceso: 'PO-01', sistema: 'sii_v2', uso: 'consultar', estado: 'bien' };
fixture.content.medios = { 'Correo electrónico': true, 'Archivo compartido': false, 'Formato impreso': false };
fixture.catalog = { estado: 'disponible', modulos: [{ id: '7', descripcion: 'Módulo sintético' }] };
fixture.delivery = () => {
    const missing = !fixture.content.identificacion[0].resultado;
    return { etapa: { id: 1, nombre: 'Primera etapa', habilitada: true, estado: fixture.submitted ? 'enviada' : 'borrador', ejercicio: 2026,
        enviadoEn: fixture.submitted ? '2026-10-10T12:00:00Z' : null, enviadoPor: 'persona@example.test', enviadoNombre: 'Persona sintética' },
        revision: { listo: !missing, pendientes: missing ? [{ seccion: 'identificacion', bloque: 'identificacion:inicial', campo: 'resultado', mensaje: 'Identificación general · Registro 1 · PO-01 · Procedimiento sintético: completa ¿Qué entrega?.' }] : [] },
        secciones: [{ id: 'contexto', nombre: 'Contexto', completa: true }, { id: 'identificacion', nombre: 'Identificación general', completa: !missing }, { id: 'sistemas', nombre: 'Sistemas y herramientas', completa: true }],
        resumen: { procedimientos: fixture.content.identificacion.length, herramientas: fixture.content.sistemas.length, medios: ['Correo electrónico'] } };
};
try {
    await check('Inicio: indicadores reales del fixture, barra alineada, permisos y expansión conservados al actualizar', async () => {
        await page.goto(fixture.origin + '/inicio');
        await expect(page.getByText('Áreas de la universidad', { exact: true })).toHaveCount(0);
        await expect(page.locator('.operational-stat')).toHaveCount(3);
        await expect(page.locator('.operational-stat').nth(0)).toContainText('9');
        await expect(page.locator('.operational-stat').nth(1)).toContainText('42%');
        const toolbar = await page.locator('.area-toolbar').evaluate(el => [...el.children].map(c => c.getBoundingClientRect().y));
        assert.ok(Math.max(...toolbar) - Math.min(...toolbar) < 12);
        await page.getByRole('button', { name: '101 · Área sintética 101', exact: true }).click();
        const own = page.getByRole('button', { name: 'Abrir formato de Área sintética 101', exact: true });
        await expect(own.locator('xpath=..')).toContainText('Solo consulta'); await expect(own).toHaveText('Ver formato');
        await page.getByRole('button', { name: 'Actualizar', exact: true }).click(); await expect(own).toBeVisible();
        await picture('inicio-escritorio');
    });
    await check('Inicio: búsqueda, filtro, grupos y desplazamiento se recuperan al volver del formato', async () => {
        await page.getByLabel('Buscar área o clave').fill('Área');
        await page.getByRole('combobox', { name: 'Mostrar', exact: true }).click();
        await page.getByRole('option', { name: 'En captura', exact: true }).click();
        await page.getByRole('button', { name: 'Contraer', exact: true }).click();
        await page.getByRole('button', { name: '106 · Área sintética 106', exact: true }).click();
        const open = page.getByRole('button', { name: 'Abrir formato de Área sintética 106', exact: true }); await open.scrollIntoViewIfNeeded();
        const before = await page.evaluate(() => window.scrollY); await open.click();
        await expect(page.getByRole('tab', { name: 'Contexto', exact: true })).toBeVisible();
        await page.getByRole('button', { name: 'Procedimientos Institucionales', exact: true }).click();
        await expect(page.getByLabel('Buscar área o clave')).toHaveValue('Área'); await expect(open).toBeVisible();
        await expect(page.getByRole('combobox', { name: 'Mostrar', exact: true })).toHaveText('En captura');
        assert.ok(Math.abs((await page.evaluate(() => window.scrollY)) - before) < 3);
    });
    await check('Procedimiento: ancho limitado, búsqueda por código/nombre, nombre completo y reserva durante menú/ayuda', async () => {
        await page.goto(fixture.origin + '/formatos/A'); await tab('Sistemas y herramientas');
        const procedure = page.getByRole('combobox', { name: 'Procedimiento 1', exact: true });
        const cellWidth = await procedure.locator('xpath=ancestor::td').evaluate(el => el.getBoundingClientRect().width);
        assert.ok(cellWidth >= 280 && cellWidth <= 360);
        await procedure.click(); await procedure.fill('');
        await expect(row().getByRole('button', { name: 'Ayuda: Procedimiento seleccionado · PO-01', exact: true })).toBeVisible();
        await procedure.fill('Beta'); await expect(row()).toHaveAttribute('data-edit-state', 'owned');
        fixture.broadcast(); await expect(procedure).toHaveValue('Beta');
        await expect(page.getByRole('option', { name: 'PO-02 · Adquisición especializada Beta', exact: true })).toBeVisible();
        await procedure.fill('PO-01'); const option = page.getByRole('option').first();
        assert.ok((await option.boundingBox()).height > 40); await option.click();
        await row().getByRole('button', { name: 'Ayuda: Procedimiento seleccionado · PO-01', exact: true }).click();
        await expect(page.getByRole('dialog')).toContainText(fixture.content.identificacion[0].tramite.trim()); await page.keyboard.press('Escape');
        await expect(row()).toHaveAttribute('data-edit-state', 'owned'); await finish();
        await picture('sistemas-escritorio');
    });
    await check('Herramienta/Otro y Medios: campos dentro de su celda, varias filas y guardado independiente', async () => {
        await page.getByRole('combobox', { name: 'Sistema o herramienta 1', exact: true }).click();
        await page.getByRole('option', { name: 'Otra (escríbela)', exact: true }).click();
        await page.getByLabel('Nombre de la herramienta 1', { exact: true }).fill('Aplicación de prueba con detalle completo');
        await page.getByRole('combobox', { name: '¿Para qué se usa? 1', exact: true }).click(); await page.getByRole('option', { name: 'Otro (escríbelo)', exact: true }).click();
        await page.getByLabel('Describe el uso 1', { exact: true }).fill('Revisar expedientes'); await finish();
        assert.equal(fixture.content.sistemas[0].sistemaOtro, 'Aplicación de prueba con detalle completo');
        await page.getByLabel('Archivo compartido', { exact: true }).click(); await expect(page.getByLabel('Archivo compartido', { exact: true })).toBeChecked(); await finish(); assert.equal(fixture.content.medios['Archivo compartido'], true);
        assert.equal(await page.locator('.systems-table').getByText('Medios utilizados').count(), 0);
        await page.getByRole('button', { name: 'Agregar fila', exact: true }).click(); await finish(); assert.equal(fixture.content.sistemas.length, 2);
        await picture('sistemas-condicionales');
    });
    await check('Revisión: Ir al campo abre y enfoca; error y ACK conservan borrador, foco y estado de guardado', async () => {
        await page.setViewportSize({ width: 390, height: 900 });
        await tab('Revisar y enviar'); await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toBeDisabled();
        await picture('revision-pendiente'); await page.getByRole('button', { name: 'Ir al campo', exact: true }).click();
        const field = page.getByLabel('¿Qué entrega? 1', { exact: true }); await expect(field).toBeFocused();
        const rect = await field.boundingBox();
        const presence = await page.locator('[data-edit-block="identificacion:inicial"] .row-editing-cell').boundingBox();
        assert.ok(rect.x >= presence.x + presence.width - 1 && rect.x < 390);
        await picture('revision-destino-movil'); await page.setViewportSize({ width: 1440, height: 1000 });
        await expect(page.locator('[data-edit-block="identificacion:inicial"]')).toHaveAttribute('data-edit-state', 'owned');
        fixture.saveFault = true; await field.fill('Respuesta que debe conservarse');
        await expect(page.locator('.form-actions [role=status]')).toHaveText('No se pudo guardar', { timeout: 10000 });
        await expect(field).toHaveValue('Respuesta que debe conservarse'); await expect(field).toBeFocused(); fixture.saveFault = false;
        let release; fixture.waitSave = new Promise(resolve => release = resolve);
        await page.getByRole('button', { name: 'Guardar', exact: true }).click(); await tab('Revisar y enviar');
        await expect(page.getByText('Autoguardado: Guardando…', { exact: true })).toBeVisible(); await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toBeDisabled();
        release(); fixture.waitSave = null; await expect(page.getByRole('button', { name: 'Ir al campo', exact: true })).toHaveCount(0);
        await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toBeEnabled();
        await expect(page.getByText('Primera etapa lista para enviar', { exact: true })).toHaveCount(0); await picture('revision-completa');
    });
    await check('Sincronizaciones: resultados separados, fecha correcta preservada, reintento SII sin duplicar', async () => {
        await page.goto(fixture.origin + '/configuracion/sincronizaciones');
        const modules = page.locator('[data-catalog="sii_modulos"]'), units = page.locator('[data-catalog="sii"]');
        await expect(units.getByText('Completada', { exact: true })).toBeVisible(); await expect(modules.getByText('Fallida', { exact: true })).toBeVisible();
        const last = await modules.locator('[data-last-success]').innerText(); await expect(modules).toContainText('1,103');
        fixture.syncRequest = async body => { assert.equal(body.fuentes, 'sii'); status.ejecucion_activa = 'b'; history.unshift(execution({ id: 'b', fuentes: 'sii', estado: 'pendiente', resultado: {}, terminada_en: null })); return { message: 'Solicitud aceptada.' }; };
        const prior = fixture.requests.filter(r => r.path.endsWith('/ejecutar')).length;
        await page.getByRole('button', { name: 'Sincronizar SQL Server', exact: true }).dblclick();
        await expect(page.locator('[data-sync-source="sii"]').getByRole('button')).toBeDisabled();
        await expect(modules.getByRole('button')).toHaveCount(0);
        assert.equal(fixture.requests.filter(r => r.path.endsWith('/ejecutar')).length, prior + 1);
        history[0].estado = 'ejecutando'; history[0].etapa = 'sii_modulos'; history[0].resultado.sii = { estado: 'completada', resumen: { unidades: 31 } };
        await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click();
        await expect(modules.getByText('En curso', { exact: true })).toBeVisible(); await expect(units.getByText('Completada', { exact: true })).toBeVisible();
        await expect(modules.locator('[data-last-success]')).toHaveText(last); await picture('sincronizaciones-en-curso');
        history[0].estado = 'parcial'; history[0].etapa = null; history[0].resultado.sii_modulos = { estado: 'fallida', mensaje: 'No fue posible sincronizar. Se conservó la copia anterior.' }; status.ejecucion_activa = null;
        await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click(); await expect(modules.getByText('Fallida', { exact: true })).toBeVisible();
        await expect(modules.locator('[data-last-success]')).toHaveText(last); assert.equal(settings.activa, false); await picture('sincronizaciones-fallo');
    });
    await check('SQL Server: éxito total, fallo total con último éxito conservado e ILDA ejecutada aparte', async () => {
        const sql = page.locator('[data-sync-source="sii"]'), ilda = page.locator('[data-sync-source="ilda"]');
        const sqlState = sql.locator(':scope > [role="status"]');
        history[0].estado = 'completada'; history[0].terminada_en = '2026-10-10T12:01:00Z';
        history[0].resultado.sii_modulos = { estado: 'completada', completada_en: '2026-10-10T12:00:50Z', resumen: { registros: 1104 } };
        catalogs.sii_modulos = { registros: 1104, completada_en: '2026-10-10T12:00:50Z' };
        await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click(); await expect(sqlState).toHaveText('Completada');
        const last = await sql.getByText(/^Última ejecución correcta de ambos catálogos:/).innerText(); assert.ok(!last.includes('No disponible'));
        await picture('sql-server-completado');
        history.unshift(execution({ id: 'failed', fuentes: 'sii', estado: 'fallida', resultado: { sii: { estado: 'fallida', mensaje: 'No fue posible leer unidades.' }, sii_modulos: { estado: 'fallida', mensaje: 'No fue posible leer módulos.' } } }));
        await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click(); await expect(sqlState).toHaveText('Fallida');
        await expect(sql.getByText(/^Última ejecución correcta de ambos catálogos:/)).toHaveText(last); await picture('sql-server-fallido');
        fixture.syncRequest = async body => { assert.equal(body.fuentes, 'ilda'); status.ejecucion_activa = 'independent'; history.unshift(execution({ id: 'independent', fuentes: 'ilda', estado: 'pendiente', resultado: {}, terminada_en: null })); return { message: 'Solicitud aceptada.' }; };
        await ilda.getByRole('button', { name: 'Sincronizar ILDA', exact: true }).click();
        await expect(ilda.getByRole('button')).toBeDisabled(); await expect(sqlState).toHaveText('Fallida');
        history[0].estado = 'completada'; history[0].resultado.ilda = { estado: 'completada', resumen: { registros: 30 } }; status.ejecucion_activa = null;
        await page.getByRole('button', { name: 'Actualizar estado', exact: true }).click(); await expect(ilda).toContainText('30 registros procesados'); await expect(sqlState).toHaveText('Fallida');
        assert.equal(settings.activa, false);
    });
    await check('Pendientes sin registros: Ir al campo enfoca Agregar fila y Agregar trámite, sin alterar respuestas', async () => {
        const previousDelivery = fixture.delivery;
        const previous = { identificacion: fixture.content.identificacion, sistemas: fixture.content.sistemas };
        const delivery = previousDelivery();
        fixture.content.identificacion = []; fixture.content.sistemas = [];
        fixture.delivery = () => ({ ...delivery, revision: { listo: false, pendientes: [
            { seccion: 'identificacion', bloque: 'identificacion', campo: '', mensaje: 'Identificación general: agrega y completa al menos un registro.' },
            { seccion: 'sistemas', bloque: 'sistemas', campo: '', mensaje: 'Sistemas y herramientas: agrega y completa al menos un registro.' },
        ] }, resumen: { ...delivery.resumen, procedimientos: 0, herramientas: 0 } });
        await page.goto(fixture.origin + '/formatos/A'); await tab('Revisar y enviar');
        await page.getByRole('button', { name: 'Ir al campo', exact: true }).first().click();
        await expect(page.getByRole('button', { name: 'Agregar trámite o servicio', exact: true })).toBeFocused();
        await tab('Revisar y enviar'); await page.getByRole('button', { name: 'Ir al campo', exact: true }).last().click();
        await expect(page.getByRole('button', { name: 'Agregar fila', exact: true })).toBeFocused();
        Object.assign(fixture.content, previous); fixture.delivery = previousDelivery;
    });
    await check('Escritorio/móvil y teclado: sin desbordamiento, opciones completas, consulta y confirmación real del transporte', async () => {
        for (const width of [390, 320]) {
            await page.setViewportSize({ width, height: 900 });
            for (const [url, name, section] of [['/inicio', 'inicio'], ['/configuracion/sincronizaciones', 'sync'], ['/formatos/A', 'revision', 'Revisar y enviar'], ['/formatos/A', 'sistemas', 'Sistemas y herramientas']]) {
                await page.goto(fixture.origin + url);
                if (section) await tab(section);
                else if (name === 'inicio') await expect(page.locator('.operational-stat').first()).toBeVisible();
                else await expect(page.locator('[data-catalog="sii"]')).toBeVisible();
                await overflow(); await picture(`${name}-${width}`);
            }
            const procedure = page.getByRole('combobox', { name: 'Procedimiento 1', exact: true }); await procedure.tap(); await procedure.fill('Beta');
            const box = await page.getByRole('listbox').boundingBox(); assert.ok(box.x >= 0 && box.x + box.width <= width + 1);
            await page.keyboard.press('ArrowDown'); await page.keyboard.press('Enter'); await finish(); assert.equal(fixture.content.sistemas[0].proceso, 'PO-02');
        }
        fixture.readOnly = true; await page.reload(); await tab('Revisar y enviar'); await expect(page.getByRole('button', { name: 'Enviar formato', exact: true })).toHaveCount(0);
        await tab('Sistemas y herramientas'); await expect(page.getByRole('combobox', { name: 'Procedimiento 1', exact: true })).toHaveJSProperty('readOnly', true);
        fixture.readOnly = false; await page.reload(); await tab('Revisar y enviar'); await page.getByRole('button', { name: 'Enviar formato', exact: true }).click();
        let release; fixture.waitSubmit = new Promise(resolve => release = resolve); await page.getByRole('button', { name: 'Confirmar envío', exact: true }).click();
        await expect(page.getByText('Formato enviado', { exact: true })).toHaveCount(0); release();
        await expect(page.getByText('Formato enviado', { exact: true })).toBeVisible(); await picture('revision-enviada');
    });
    assert.deepEqual(report.errors, []); report.passed = true;
} catch (error) { report.failure = error.message; await picture('failure'); throw error; }
finally { await writeFile(path.join(output, 'verification.json'), JSON.stringify(report, null, 2)); await browser.close(); await fixture.close(); }
