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
const { splitBlocks } = await import(blocks);
const content = () => ({ encabezado: { area: 'Área', fecha: '', responsable: '' }, identificacion: [], sistemas: [], datos: [], medios: {}, medioOtro: '', evaluaciones: {}, preguntas: [], acuerdos: [] });
const live = (changes = {}) => ({ contenido: content(), version: 0, porcentaje: 0, bloques: [], editable: true, puedeEnviar: true, enviadoEn: null, seccion: 'contexto', actualizadoEn: null, actualizadoPor: null, servidorEn: new Date().toISOString(), ...changes });
const lease = () => ({ id: crypto.randomUUID(), propia: true, titular: 'Persona sintética', venceEn: new Date(Date.now() + 45000).toISOString() });
const saved = body => ({ version: 1, porcentaje: 5, actualizadoEn: new Date().toISOString(), actualizadoPor: 'Persona sintética', bloques: body.blocks.map(block => ({ ...block, version: block.version + 1, reserva: null })) });
function create(overrides = {}) {
    const writes = []; let current = live();
    const transport = {
        read: async () => structuredClone(current),
        reserve: async body => ({ bloques: body.blocks.map(block => ({ ...block, value: splitBlocks(content())[block.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }),
        renew: async body => ({ bloques: body.blocks.map(block => ({ ...block, value: splitBlocks(content())[block.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }),
        release: async () => ({}), save: async body => { writes.push(structuredClone(body)); return saved(body); },
        submit: async () => ({ version: 2, enviadoEn: new Date().toISOString() }), ...overrides,
    };
    const editor = new BlockEditor(content(), 0, 0, true, transport, () => {}); editor.receive(current);
    return { editor, writes, setLive: value => { current = value; } };
}
test('normaliza nulos y evaluaciones heredadas sin perder respuestas', () => {
    const c = content(); c.encabezado.responsable = null; c.evaluaciones = [];
    assert.deepEqual(normalizeContent(c).evaluaciones, {}); assert.equal(normalizeContent(c).encabezado.responsable, null);
});

test('editar un bloque conserva referencias ajenas y volver al valor original retira el pendiente', async () => {
    const { editor, writes } = create(); await editor.focus('medios');
    const original = editor.state.content, questions = original.preguntas, processes = original.identificacion;
    assert.equal(editor.changeBlock('medios', value => { value.medioOtro = 'Propuesta'; }), true);
    assert.equal(editor.state.dirty, true); assert.equal(editor.state.content.preguntas, questions);
    assert.equal(editor.state.content.identificacion, processes); assert.equal(original.medioOtro, '');
    editor.changeBlock('medios', value => { value.medioOtro = ''; });
    assert.equal(editor.state.dirty, false); assert.equal(await editor.flush(), true); assert.equal(writes.length, 0); editor.dispose();
});

test('estado remoto equivalente conserva las referencias del contenido y la reserva no concede escritura', () => {
    const { editor } = create(); const previous = editor.state.content;
    editor.receive(live()); assert.equal(editor.state.content, previous);
    assert.equal(editor.changeBlock('medios', value => { value.medioOtro = 'Sin reserva'; }), false);
    assert.equal(editor.state.dirty, false); editor.dispose();
});

test('latidos equivalentes no notifican React; una revocación o cambio de reserva sí', () => {
    let renders = 0;
    const editor = new BlockEditor(content(), 0, 0, true, {}, () => renders++);
    editor.receive(live()); const first = renders;
    editor.receive(live()); editor.receive(live());
    assert.equal(renders, first);
    editor.receive(live({ bloques: [{ key: 'medios', version: 0, reserva: { ...lease(), propia: false } }] }));
    assert.equal(renders, first + 1); assert.equal(editor.busy('medios'), true);
    editor.receive(live({ editable: false })); assert.equal(editor.state.locked, true); assert.equal(renders, first + 2);
    editor.dispose();
});

test('revalida la confirmación destructiva después de esperar la reserva sin modificar otra versión', async () => {
    let grant;
    const { editor, writes } = create({ reserve: body => new Promise(resolve => {
        grant = () => resolve({ bloques: body.blocks.map(block => ({ ...block, version: 1,
            value: { medios: {}, medioOtro: 'Versión posterior' }, reserva: lease() })), servidorEn: new Date().toISOString() });
    }) });
    const confirmed = editor.state.content.medioOtro;
    const actual = editor.edit(data => { data.medioOtro = 'Cambio confirmado'; }, () => editor.state.content.medioOtro === confirmed);
    grant(); assert.equal(await actual, false);
    assert.equal(editor.state.content.medioOtro, 'Versión posterior');
    assert.equal(editor.state.dirty, false); assert.equal(writes.length, 0);
    editor.dispose();
});

test('una intención de selección pendiente no escribe si el usuario ya salió del registro', async () => {
    let grant;
    const { editor } = create({ reserve: body => new Promise(resolve => {
        grant = () => resolve({ bloques: body.blocks.map(block => ({ ...block, value: splitBlocks(content())[block.key], reserva: lease() })), servidorEn: new Date().toISOString() });
    }) });
    const entering = editor.focus('medios');
    const leaving = editor.endBlock('medios');
    grant(); assert.equal(await entering, false); await leaving;
    assert.equal(editor.canEdit('medios'), false); assert.equal(editor.state.dirty, false);
    editor.dispose();
});

test('SignalR libera una fila ajena; la siguiente interacción confirma contenido y permite editar sin recarga', async () => {
    let reserves = 0;
    const current = content(); current.medioOtro = 'Confirmado por otra sesión';
    const { editor } = create({ reserve: async body => {
        reserves++;
        return { bloques: body.blocks.map(block => ({ ...block, version: 2, value: splitBlocks(current)[block.key], reserva: lease() })), servidorEn: new Date().toISOString() };
    } });
    editor.receive(live({ bloques: [{ key: 'medios', version: 1, reserva: { ...lease(), propia: false, id: null } }] }));
    assert.equal(await editor.focus('medios'), false); assert.equal(reserves, 0);
    editor.receive(live({ version: 2, contenido: current, bloques: [{ key: 'medios', version: 2, reserva: null }] }));
    assert.equal(editor.state.content.medioOtro, 'Confirmado por otra sesión');
    assert.equal(editor.busy('medios'), false); assert.equal(reserves, 0);
    assert.equal(await editor.focus('medios'), true); assert.equal(reserves, 1);
    assert.equal(editor.change(c => { c.medioOtro = 'Nueva propuesta'; }), true);
    editor.dispose();
});

test('una confirmación de envío no autoriza una versión recibida después; conserva el borrador', async () => {
    let submitted = 0;
    const { editor } = create({ submit: async () => { submitted++; return {}; } });
    editor.receive(live({ version: 3 }));
    assert.equal(await editor.submit(2), false); assert.equal(submitted, 0);
    assert.match(editor.state.error, /respuestas cambiaron/); assert.equal(editor.state.locked, false);
    editor.dispose();
});
test('dos pestañas del mismo usuario tienen identificadores independientes', () => {
    const a = create(), b = create(); assert.notEqual(a.editor.tabId, b.editor.tabId); a.editor.dispose(); b.editor.dispose();
});

test('foco durante guardado espera la versión confirmada antes de adquirir otra reserva', async () => {
    let confirm;
    const versions = [];
    const { editor } = create({
        reserve: async body => {
            versions.push(body.blocks[0].version);
            return { bloques: body.blocks.map(block => ({ ...block, value: splitBlocks(content())[block.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() };
        },
        save: body => new Promise(resolve => { confirm = () => resolve(saved(body)); }),
    });
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Propuesta'; });
    const flushing = editor.flush();
    while (!confirm) await new Promise(resolve => setTimeout(resolve, 0));
    const focused = editor.focus('contexto');
    assert.deepEqual(versions, [0]); confirm(); await flushing; await focused;
    assert.deepEqual(versions, [0, 1]); assert.equal(editor.state.conflict, false); editor.dispose();
});

test('actualizar tras error de envío permite nueva revisión sin modificar respuestas', async () => {
    const { editor, setLive } = create({ submit: async () => { throw { response: { status: 409, data: { message: 'Reserva ajena' } } }; } });
    assert.equal(await editor.submit(), false); assert.equal(editor.state.error, 'Reserva ajena');
    const before = structuredClone(editor.state.content);
    setLive(live({ revisionEnvio: { listo: true, pendientes: [] } }));
    await editor.refresh(); assert.equal(editor.state.error, ''); assert.deepEqual(editor.state.content, before);
    assert.equal(editor.state.review.listo, true); editor.dispose();
});

test('salir del bloque cancela un foco pendiente de guardado; no adquiere una reserva tardía', async () => {
    let confirm;
    const reserves = [];
    const { editor } = create({
        reserve: async body => { reserves.push(body); return { bloques: body.blocks.map(block => ({ ...block, value: splitBlocks(content())[block.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }; },
        save: body => new Promise(resolve => { confirm = () => resolve(saved(body)); }),
    });
    await editor.focus('medios'); editor.change(c => { c.medioOtro = 'Propuesta'; });
    const saving = editor.flush();
    while (!confirm) await new Promise(resolve => setTimeout(resolve, 0));
    const focus = editor.focus('contexto');
    await editor.endBlock('contexto');
    confirm(); await saving; await focus;
    assert.deepEqual(reserves.map(body => body.blocks.map(block => block.key)), [['medios']]);
    editor.dispose();
});

test('salir mientras se adquiere una reserva espera y libera esa reserva', async () => {
    let acquired; const released = [];
    const { editor } = create({
        reserve: body => new Promise(resolve => { acquired = () => resolve({ bloques: body.blocks.map(block => ({ ...block, value: splitBlocks(content())[block.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }); }),
        release: async body => { released.push(body); },
    });
    const focus = editor.focus('contexto');
    const ending = editor.endBlock('contexto');
    assert.equal(released.length, 0); acquired(); await focus; await ending;
    assert.equal(released.length, 1); assert.equal(released[0].blocks[0].key, 'contexto');
    assert.ok(released[0].blocks[0].leaseId); assert.equal(editor.state.blocks.contexto.reserva, null);
    editor.dispose();
});

test('una sección elegida durante la carga no se sustituye por la sección inicial tardía', () => {
    const editor = new BlockEditor(content(), 0, 0, true, {}, () => {});
    editor.setSection('identificacion'); editor.receive(live({ seccion: 'contexto' }));
    assert.equal(editor.state.section, 'identificacion'); editor.dispose();
});

test('navegar durante adquisición no modifica ni guarda el contexto anterior', async () => {
    let resolveReserve;
    const { editor, writes } = create({ reserve: body => new Promise(resolve => {
        resolveReserve = () => resolve({ bloques: body.blocks.map(block => ({ ...block, value: splitBlocks(content())[block.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() });
    }) });
    const focusing = editor.focus('contexto');
    assert.equal(editor.change(c => { c.encabezado.responsable = 'No permitida'; }), false);
    editor.dispose(); resolveReserve(); await focusing;
    assert.equal(editor.state.content.encabezado.responsable, ''); assert.equal(writes.length, 0);
});
test('guarda sólo el bloque modificado y confirma después de la respuesta', async () => {
    const { editor, writes } = create(); await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Pendiente'; });
    assert.equal(editor.state.updatedAt, null); assert.equal(editor.state.dirty, true);
    assert.equal(await editor.flush(true), true); assert.equal(writes[0].blocks.length, 1);
    assert.equal(writes[0].blocks[0].key, 'contexto'); assert.equal(writes[0].release, true);
    assert.ok(editor.state.updatedAt); assert.equal(editor.state.dirty, false); editor.dispose();
});
test('reserva ajena permite consulta y conserva el contenido original', async () => {
    const { editor } = create(); editor.receive(live({ bloques: [{ key: 'contexto', version: 0, reserva: { ...lease(), propia: false, id: null } }] }));
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'No autorizado'; });
    assert.equal(editor.state.content.encabezado.responsable, ''); assert.equal(editor.busy('contexto'), true); editor.dispose();
});

test('reconectar retira el aviso de conexión sin ocultar un error de guardado', async () => {
    const { editor } = create({ save: async () => { throw { response: { status: 503, data: { message: 'Guardado rechazado' } } }; } });
    editor.connectionLost(); assert.ok(editor.state.error);
    await editor.refresh(); assert.equal(editor.state.error, '');
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Pendiente'; }); await editor.flush();
    editor.connectionLost(); await editor.refresh();
    assert.equal(editor.state.issues.contexto, 'Guardado rechazado'); assert.equal(editor.state.dirty, true); editor.dispose();
});

test('rechazo atrasado de renovación no invalida un guardado ya confirmado', async () => {
    let rejectRenew;
    const { editor } = create({ renew: () => new Promise((_, reject) => { rejectRenew = reject; }) });
    await editor.focus('contexto'); await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Confirmado'; });
    assert.equal(await editor.flush(), true);
    rejectRenew({ response: { status: 409 } }); await new Promise(resolve => setTimeout(resolve, 0));
    assert.equal(editor.state.error, ''); assert.equal(editor.state.conflict, false); editor.dispose();
});
test('desconexión conserva propuesta y reintenta el mismo identificador', async () => {
    const requests = []; let fail = true;
    const { editor } = create({ save: async body => { requests.push(structuredClone(body)); if (fail) { fail = false; throw new Error('network'); } return saved(body); } });
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Propuesta'; }); assert.equal(await editor.flush(), false);
    assert.equal(editor.state.content.encabezado.responsable, 'Propuesta'); assert.equal(editor.state.dirty, true);
    assert.equal(await editor.flush(), true); assert.deepEqual(requests[0], requests[1]); editor.dispose();
});
test('editar durante un guardado conserva la propuesta posterior', async () => {
    let resolve, sent; const received = new Promise(r => { resolve = r; });
    const { editor } = create({ save: async body => { sent = body; return await received; } });
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Primero'; }); const saving = editor.flush();
    await new Promise(r => setImmediate(r)); editor.change(c => { c.encabezado.responsable = 'Segundo'; });
    resolve(saved(sent)); await saving; assert.equal(editor.state.content.encabezado.responsable, 'Segundo'); assert.equal(editor.state.dirty, true); editor.dispose();
});
test('remoto combina bloques limpios y conserva el pendiente', async () => {
    const { editor } = create(); await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Mi propuesta'; });
    const other = content(); other.medioOtro = 'Otra sesión'; editor.receive(live({ contenido: other, version: 1, bloques: [{ key: 'medios', version: 1, reserva: null }] }));
    assert.equal(editor.state.content.encabezado.responsable, 'Mi propuesta'); assert.equal(editor.state.content.medioOtro, 'Otra sesión'); editor.dispose();
});
test('conflicto conserva propuesta; recuperación explícita no guarda automáticamente', async () => {
    const { editor, writes, setLive } = create(); await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Propuesta'; });
    const other = content(); other.encabezado.responsable = 'Compartida';
    const remote = live({ contenido: other, version: 1, bloques: [{ key: 'contexto', version: 1, reserva: null }] });
    setLive(remote); editor.receive(remote); assert.equal(editor.state.conflict, true); assert.equal(await editor.flush(), false);
    await editor.recover(1); assert.equal(editor.state.content.encabezado.responsable, 'Propuesta'); assert.equal(writes.length, 0);
    assert.equal(editor.state.conflict, false); editor.dispose();
});
test('recuperación rechaza una versión que cambió después de compararla', async () => {
    const { editor, setLive } = create(); await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Propuesta'; });
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

test('reserva pendiente bloquea escritura y carga el valor confirmado antes de habilitarla', async () => {
    let grant;
    const { editor } = create({ reserve: body => new Promise(resolve => { grant = () => resolve({
        bloques: body.blocks.map(b => ({ ...b, reserva: lease(), value: { ...content().encabezado, responsable: 'Vigente' } })), servidorEn: new Date().toISOString(),
    }); }) });
    assert.equal(editor.change(c => { c.encabezado.responsable = 'Antes'; }), false);
    const focused = editor.focus('contexto'); assert.deepEqual(editor.state.preparing, ['contexto']);
    assert.equal(editor.change(c => { c.encabezado.responsable = 'Durante'; }), false);
    grant(); await focused;
    assert.equal(editor.state.content.encabezado.responsable, 'Vigente'); assert.equal(editor.canEdit('contexto'), true);
    assert.equal(editor.change(c => { c.encabezado.responsable = 'Después'; }), true); editor.dispose();
});

test('SignalR por sí solo nunca concede edición; una carrera de reserva no genera error global', async () => {
    const own = { key: 'contexto', version: 0, reserva: lease() };
    const { editor, setLive } = create({ reserve: async () => { throw { response: { status: 409 } }; } });
    editor.receive(live({ bloques: [own] })); assert.equal(editor.canEdit('contexto'), false);
    setLive(live({ bloques: [{ ...own, reserva: { ...own.reserva, propia: false, id: null } }] }));
    await editor.focus('contexto'); assert.equal(editor.busy('contexto'), true);
    assert.equal(editor.state.error, ''); assert.deepEqual(editor.state.issues, {}); editor.dispose();
});

test('vencimiento bloquea incluso antes del temporizador y no readquiere una propuesta silenciosamente', async () => {
    let reserves = 0;
    const { editor, writes } = create({ reserve: async body => { reserves++; return { bloques: body.blocks.map(b => ({ ...b, value: splitBlocks(content())[b.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }; } });
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Pendiente'; });
    editor.state.blocks.contexto.reserva.venceEn = new Date(Date.now() - 1).toISOString();
    assert.equal(editor.change(c => { c.encabezado.responsable = 'Atrasada'; }), false);
    assert.equal(await editor.flush(), false); await editor.focus('contexto');
    assert.equal(reserves, 1); assert.equal(writes.length, 0); assert.equal(editor.state.content.encabezado.responsable, 'Pendiente'); editor.dispose();
});

test('desconexión congela cambios; reconectar con titular distinto conserva la propuesta y admite otro bloque', async () => {
    const { editor, writes, setLive } = create(); await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Mi propuesta'; });
    editor.connectionLost(); assert.equal(editor.canEdit('contexto'), false);
    setLive(live({ bloques: [{ key: 'contexto', version: 0, reserva: { ...lease(), id: null, propia: false } }] })); await editor.refresh();
    assert.equal(editor.canEdit('contexto'), false); assert.equal(editor.state.content.encabezado.responsable, 'Mi propuesta');
    await editor.focus('medios'); editor.change(c => { c.medioOtro = 'Otro bloque'; }); await editor.flush();
    assert.deepEqual(writes[0].blocks.map(b => b.key), ['medios']); assert.equal(editor.state.dirty, true); editor.dispose();
});

test('cambiar de campo del registro no renueva ni vuelve a adquirir su reserva', async () => {
    let reserves = 0, renews = 0;
    const { editor } = create({ reserve: async body => { reserves++; return { bloques: body.blocks.map(b => ({ ...b, value: splitBlocks(content())[b.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }; }, renew: async () => { renews++; return { bloques: [] }; } });
    await editor.focus('contexto'); await editor.focus('contexto'); await editor.focus('contexto');
    assert.equal(reserves, 1); assert.equal(renews, 0); editor.dispose();
});

test('salir guarda antes de liberar; error conserva propuesta y no libera', async () => {
    let confirm; const operations = [];
    const { editor } = create({ save: body => new Promise(resolve => { operations.push('save'); confirm = () => resolve({ ...saved(body), bloques: body.blocks.map(b => ({ ...b, version: 1, reserva: editor.state.blocks[b.key].reserva })) }); }), release: async () => { operations.push('release'); } });
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Confirmar'; });
    const leaving = editor.endBlock('contexto'); await new Promise(r => setImmediate(r));
    assert.deepEqual(operations, ['save']); confirm(); await leaving; assert.deepEqual(operations, ['save', 'release']); editor.dispose();
    let releases = 0;
    const failed = create({ save: async () => { throw new Error('network'); }, release: async () => { releases++; } });
    await failed.editor.focus('contexto'); failed.editor.change(c => { c.encabezado.responsable = 'Conservar'; }); await failed.editor.endBlock('contexto');
    assert.equal(releases, 0); assert.equal(failed.editor.state.dirty, true); failed.editor.dispose();
});

test('el foco durante lectura inicial espera acceso y luego confirma la reserva', async () => {
    let reserves = 0;
    const editor = new BlockEditor(content(), 0, 0, true, {
        reserve: async body => { reserves++; return { bloques: body.blocks.map(b => ({ ...b, value: splitBlocks(content())[b.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }; },
        release: async () => {},
    }, () => {});
    await editor.focus('contexto'); assert.equal(reserves, 0); assert.equal(editor.canEdit('contexto'), false);
    editor.receive(live()); await new Promise(r => setImmediate(r));
    assert.equal(reserves, 1); assert.equal(editor.canEdit('contexto'), true); editor.dispose();
});

test('conservar respuesta compartida es explícito y no descarta propuestas de otros bloques', async () => {
    const { editor, setLive } = create(); await editor.focus('contexto'); await editor.focus('medios');
    editor.change(c => { c.encabezado.responsable = 'Propuesta'; c.medioOtro = 'Conservar'; });
    const shared = content(); shared.encabezado.responsable = 'Compartida';
    setLive(live({ version: 1, contenido: shared }));
    await editor.discard(1, ['contexto']);
    assert.equal(editor.state.content.encabezado.responsable, 'Compartida');
    assert.equal(editor.state.content.medioOtro, 'Conservar'); assert.equal(editor.state.dirty, true); editor.dispose();
});

test('volver durante liberación no acepta cambios hasta obtener otra reserva confirmada', async () => {
    let finish; let reserves = 0;
    const { editor } = create({ reserve: async body => { reserves++; return { bloques: body.blocks.map(b => ({ ...b, value: splitBlocks(content())[b.key] ?? null, reserva: lease() })), servidorEn: new Date().toISOString() }; },
        release: () => new Promise(resolve => { finish = resolve; }) });
    await editor.focus('contexto'); const first = editor.state.blocks.contexto.reserva.id;
    const leaving = editor.endBlock('contexto'); const returning = editor.focus('contexto');
    assert.equal(editor.change(c => { c.encabezado.responsable = 'Durante liberación'; }), false);
    assert.equal(reserves, 1); finish(); await leaving; await returning;
    assert.equal(reserves, 2); assert.notEqual(editor.state.blocks.contexto.reserva.id, first); assert.equal(editor.canEdit('contexto'), true);
    editor.dispose(); await new Promise(r => setImmediate(r)); finish();
});

test('mensaje en vuelo después de desconexión no rehabilita hasta una lectura nueva', async () => {
    const { editor, setLive } = create(); await editor.focus('contexto');
    const snapshot = live({ bloques: Object.values(editor.state.blocks) }); setLive(snapshot);
    editor.connectionLost(); editor.receive(snapshot); assert.equal(editor.canEdit('contexto'), false);
    await editor.refresh(); assert.equal(editor.canEdit('contexto'), true); editor.dispose();
});

test('recibo antiguo confirma sin reemplazar una respuesta compartida más reciente', async () => {
    let failed = false;
    const { editor, writes } = create({ save: async body => { writes.push(body); if (!failed) { failed = true; throw new Error('network'); } return saved(body); } });
    await editor.focus('contexto'); editor.change(c => { c.encabezado.responsable = 'Mi guardado anterior'; });
    await editor.flush();
    const recent = content(); recent.encabezado.responsable = 'Cambio posterior compartido';
    editor.receive(live({ version: 3, contenido: recent, bloques: [{ key: 'contexto', version: 3, reserva: null }] }));
    await editor.flush();
    assert.deepEqual(writes[0], writes[1]); assert.equal(editor.state.content.encabezado.responsable, 'Cambio posterior compartido');
    assert.equal(editor.state.version, 3); assert.equal(editor.state.blocks.contexto.version, 3); assert.equal(editor.state.dirty, false); editor.dispose();
});
