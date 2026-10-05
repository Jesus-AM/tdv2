import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
function module(name, replacements = {}) {
    const source = readFileSync(new URL(`../../resources/js/lib/${name}.ts`, import.meta.url), 'utf8');
    let js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
    for (const [key, value] of Object.entries(replacements)) js = js.replaceAll(`'${key}'`, `'${value}'`);
    return `data:text/javascript;base64,${Buffer.from(js).toString('base64')}`;
}
const normalizer = module('form-editor'), blocks = module('form-blocks');
const { BlockEditor } = await import(module('block-editor', { './form-editor': normalizer, './form-blocks': blocks }));
const { normalizeContent } = await import(normalizer);
const content = () => ({ encabezado: { area: 'Área', fecha: '', responsable: '' }, identificacion: [], sistemas: [], datos: [], medios: {}, medioOtro: '', evaluaciones: {}, preguntas: [], acuerdos: [] });
const live = (changes = {}) => ({ contenido: content(), version: 0, porcentaje: 0, bloques: [], editable: true, puedeEnviar: true, enviadoEn: null, seccion: 'contexto', actualizadoEn: null, actualizadoPor: null, servidorEn: new Date().toISOString(), ...changes });
const lease = () => ({ id: crypto.randomUUID(), propia: true, titular: 'Persona sintética', venceEn: new Date(Date.now() + 45000).toISOString() });
const saved = body => ({ version: 1, porcentaje: 5, actualizadoEn: new Date().toISOString(), actualizadoPor: 'Persona sintética', bloques: body.blocks.map(block => ({ ...block, version: block.version + 1, reserva: null })) });
function create(overrides = {}) {
    const writes = []; let current = live();
    const transport = {
        read: async () => structuredClone(current),
        reserve: async body => ({ bloques: body.blocks.map(block => ({ ...block, reserva: lease() })), servidorEn: new Date().toISOString() }),
        renew: async body => ({ bloques: body.blocks.map(block => ({ ...block, reserva: lease() })), servidorEn: new Date().toISOString() }),
        release: async () => ({}), save: async body => { writes.push(structuredClone(body)); return saved(body); },
        submit: async () => ({ version: 2, enviadoEn: new Date().toISOString() }), ...overrides,
    };
    const editor = new BlockEditor(content(), 0, 0, true, transport, () => {}); editor.receive(current);
    return { editor, writes, setLive: value => { current = value; } };
}
test('normaliza nulos y evaluaciones heredadas sin perder respuestas', () => {
    const c = content(); c.encabezado.responsable = null; c.evaluaciones = [];
    assert.deepEqual(normalizeContent(c).evaluaciones, {}); assert.equal(normalizeContent(c).encabezado.responsable, '');
});
test('dos pestañas del mismo usuario tienen identificadores independientes', () => {
    const a = create(), b = create(); assert.notEqual(a.editor.tabId, b.editor.tabId); a.editor.dispose(); b.editor.dispose();
});

test('una sección elegida durante la carga no se sustituye por la sección inicial tardía', () => {
    const editor = new BlockEditor(content(), 0, 0, true, {}, () => {});
    editor.setSection('identificacion'); editor.receive(live({ seccion: 'contexto' }));
    assert.equal(editor.state.section, 'identificacion'); editor.dispose();
});

test('navegar o cambiar representación mientras llega una reserva no envía la propuesta del editor anterior', async () => {
    let resolveReserve;
    const { editor, writes } = create({ reserve: body => new Promise(resolve => {
        resolveReserve = () => resolve({ bloques: body.blocks.map(block => ({ ...block, reserva: lease() })), servidorEn: new Date().toISOString() });
    }) });
    editor.change(c => { c.encabezado.responsable = 'Propuesta del contexto anterior'; });
    const saving = editor.flush();
    editor.dispose(); resolveReserve();
    assert.equal(await saving, false); assert.equal(writes.length, 0);
});
test('guarda sólo el bloque modificado y confirma después de la respuesta', async () => {
    const { editor, writes } = create(); editor.change(c => { c.encabezado.responsable = 'Pendiente'; });
    assert.equal(editor.state.updatedAt, null); assert.equal(editor.state.dirty, true);
    assert.equal(await editor.flush(true), true); assert.equal(writes[0].blocks.length, 1);
    assert.equal(writes[0].blocks[0].key, 'contexto'); assert.equal(writes[0].release, true);
    assert.ok(editor.state.updatedAt); assert.equal(editor.state.dirty, false); editor.dispose();
});
test('reserva ajena permite consulta y conserva el contenido original', () => {
    const { editor } = create(); editor.receive(live({ bloques: [{ key: 'contexto', version: 0, reserva: { ...lease(), propia: false, id: null } }] }));
    editor.change(c => { c.encabezado.responsable = 'No autorizado'; });
    assert.equal(editor.state.content.encabezado.responsable, ''); assert.equal(editor.busy('contexto'), true); editor.dispose();
});

test('reconectar retira el aviso de conexión sin ocultar un error de guardado', async () => {
    const { editor } = create({ save: async () => { throw { response: { status: 503, data: { message: 'Guardado rechazado' } } }; } });
    editor.connectionLost(); assert.ok(editor.state.error);
    editor.receive(live()); assert.equal(editor.state.error, '');
    editor.change(c => { c.encabezado.responsable = 'Pendiente'; }); await editor.flush();
    editor.connectionLost(); editor.receive(live());
    assert.equal(editor.state.error, 'Guardado rechazado'); assert.equal(editor.state.dirty, true); editor.dispose();
});

test('rechazo atrasado de renovación no invalida un guardado ya confirmado', async () => {
    let rejectRenew;
    const { editor } = create({ renew: () => new Promise((_, reject) => { rejectRenew = reject; }) });
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Confirmado'; });
    assert.equal(await editor.flush(), true);
    rejectRenew({ response: { status: 409 } }); await new Promise(resolve => setTimeout(resolve, 0));
    assert.equal(editor.state.error, ''); assert.equal(editor.state.conflict, false); editor.dispose();
});
test('desconexión conserva propuesta y reintenta el mismo identificador', async () => {
    const requests = []; let fail = true;
    const { editor } = create({ save: async body => { requests.push(structuredClone(body)); if (fail) { fail = false; throw new Error('network'); } return saved(body); } });
    editor.change(c => { c.encabezado.responsable = 'Propuesta'; }); assert.equal(await editor.flush(), false);
    assert.equal(editor.state.content.encabezado.responsable, 'Propuesta'); assert.equal(editor.state.dirty, true);
    assert.equal(await editor.flush(), true); assert.deepEqual(requests[0], requests[1]); editor.dispose();
});
test('editar durante un guardado conserva la propuesta posterior', async () => {
    let resolve, sent; const received = new Promise(r => { resolve = r; });
    const { editor } = create({ save: async body => { sent = body; return await received; } });
    editor.change(c => { c.encabezado.responsable = 'Primero'; }); const saving = editor.flush();
    await new Promise(r => setImmediate(r)); editor.change(c => { c.encabezado.responsable = 'Segundo'; });
    resolve(saved(sent)); await saving; assert.equal(editor.state.content.encabezado.responsable, 'Segundo'); assert.equal(editor.state.dirty, true); editor.dispose();
});
test('remoto combina bloques limpios y conserva el pendiente', () => {
    const { editor } = create(); editor.change(c => { c.encabezado.responsable = 'Mi propuesta'; });
    const other = content(); other.medioOtro = 'Otra sesión'; editor.receive(live({ contenido: other, version: 1, bloques: [{ key: 'medios', version: 1, reserva: null }] }));
    assert.equal(editor.state.content.encabezado.responsable, 'Mi propuesta'); assert.equal(editor.state.content.medioOtro, 'Otra sesión'); editor.dispose();
});
test('conflicto conserva propuesta; recuperación explícita no guarda automáticamente', async () => {
    const { editor, writes, setLive } = create(); editor.change(c => { c.encabezado.responsable = 'Propuesta'; });
    const other = content(); other.encabezado.responsable = 'Compartida';
    const remote = live({ contenido: other, version: 1, bloques: [{ key: 'contexto', version: 1, reserva: null }] });
    setLive(remote); editor.receive(remote); assert.equal(editor.state.conflict, true); assert.equal(await editor.flush(), false);
    await editor.recover(1); assert.equal(editor.state.content.encabezado.responsable, 'Propuesta'); assert.equal(writes.length, 0);
    assert.equal(editor.state.conflict, false); editor.dispose();
});
test('recuperación rechaza una versión que cambió después de compararla', async () => {
    const { editor, setLive } = create(); editor.change(c => { c.encabezado.responsable = 'Propuesta'; });
    setLive(live({ version: 4 })); await assert.rejects(() => editor.recover(3)); assert.equal(editor.state.content.encabezado.responsable, 'Propuesta'); editor.dispose();
});
test('respuesta fuera de orden no reemplaza la versión reciente', () => {
    const { editor } = create(); const latest = content(); latest.encabezado.responsable = 'Nueva';
    editor.receive(live({ version: 3, contenido: latest })); editor.receive(live({ version: 2 }));
    assert.equal(editor.state.content.encabezado.responsable, 'Nueva'); assert.equal(editor.state.version, 3); editor.dispose();
});
test('envío confirmado bloquea edición; fallo no anuncia éxito', async () => {
    const { editor } = create({ submit: async () => { throw { response: { status: 409, data: { message: 'Reserva ajena' } } }; } });
    assert.equal(await editor.submit(), false); assert.equal(editor.state.submitted, null); assert.equal(editor.state.error, 'Reserva ajena'); editor.dispose();
    const success = create(); assert.equal(await success.editor.submit(), true); assert.equal(success.editor.state.locked, true); success.editor.dispose();
});
