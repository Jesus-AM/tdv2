import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
const raw = readFileSync(new URL('../../resources/js/lib/form-editor.ts', import.meta.url), 'utf8');
const js = ts.transpileModule(raw, {
    compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 },
}).outputText;
const { FormEditor, normalizeContent } = await import(
    `data:text/javascript;base64,${Buffer.from(js).toString('base64')}`
);
const content = () => ({
    encabezado: { area: 'Área', fecha: '', responsable: '' },
    identificacion: [],
    sistemas: [],
    datos: [],
    medios: {},
    medioOtro: '',
    evaluaciones: [],
    preguntas: [],
    acuerdos: [],
});
function create(put, editable = true) {
    const timers = new Map();
    let n = 0;
    const editor = new FormEditor(
        content(),
        0,
        0,
        editable,
        put,
        () => {},
        (fn) => {
            timers.set(++n, fn);
            return n;
        },
        (id) => timers.delete(id),
    );
    return { editor, timers };
}
test('convierte las evaluaciones vacías al objeto esperado por el servidor', () => {
    const { editor } = create(async () => {});
    assert.equal(Array.isArray(editor.state.content.evaluaciones), false);
    assert.deepEqual(editor.state.content.evaluaciones, {});
});
test('serializa cambios durante el envío con la nueva versión', async () => {
    let finish;
    const calls = [];
    const { editor, timers } = create((body) => {
        calls.push(body);
        return calls.length === 1
            ? new Promise((resolve) => {
                  finish = resolve;
              })
            : Promise.resolve({ version: 2, porcentaje: 10 });
    });
    editor.change((c) => {
        c.encabezado.responsable = 'Primero';
    });
    const first = editor.flush();
    editor.change((c) => {
        c.encabezado.responsable = 'Segundo';
    });
    await editor.flush();
    assert.equal(calls.length, 1);
    assert.equal(calls[0].contenido.encabezado.responsable, 'Primero');
    finish({ version: 1, porcentaje: 5 });
    await first;
    await editor.flush();
    assert.equal(calls.length, 2);
    assert.equal(calls[1].version, 1);
    assert.equal(calls[1].contenido.encabezado.responsable, 'Segundo');
    assert.equal(editor.state.dirty, false);
    editor.dispose();
    assert.equal(timers.size, 0);
});
test('un conflicto conserva borrador y bloquea nuevas escrituras', async () => {
    let calls = 0;
    const { editor } = create(async () => {
        calls++;
        throw { response: { status: 409 } };
    });
    editor.change((c) => {
        c.encabezado.responsable = 'Mi borrador';
    });
    await editor.flush();
    await editor.flush(true);
    assert.equal(calls, 1);
    assert.equal(editor.state.dirty, true);
    assert.equal(editor.state.locked, true);
    assert.equal(editor.state.conflict, true);
    assert.equal(editor.state.content.encabezado.responsable, 'Mi borrador');
});
test('consulta no modifica ni envía contenido incluso con guardado explícito', async () => {
    let calls = 0;
    const { editor } = create(async () => {
        calls++;
    }, false);
    editor.change((c) => {
        c.encabezado.responsable = 'No';
    });
    await editor.flush(true);
    assert.equal(calls, 0);
    assert.equal(editor.state.content.encabezado.responsable, '');
});
test('error de validación admite corrección sin sobrescribir la versión', async () => {
    let calls = 0;
    const { editor } = create(async () => {
        if (++calls === 1)
            throw { response: { status: 422, data: { errors: { contenido: ['Revisa la prioridad'] } } } };
        return { version: 1, porcentaje: 5 };
    });
    editor.change((c) => {
        c.encabezado.responsable = 'Prueba';
    });
    await editor.flush();
    assert.equal(editor.state.error, 'Revisa la prioridad');
    assert.equal(editor.state.locked, false);
    editor.change((c) => {
        c.encabezado.responsable = 'Corregido';
    });
    await editor.flush();
    assert.equal(editor.state.version, 1);
    assert.equal(editor.state.dirty, false);
    editor.dispose();
});

test('normaliza nulos del contrato Laravel sin perder campos históricos', () => {
    const c = content();
    c.encabezado.responsable = null;
    c.identificacion = [
        {
            id: 'ilda:1',
            codigo: null,
            prioridad: null,
            fuente: 'ILDA',
            area: 'Histórico',
            tramite: 'Servicio',
            usuario: null,
            resultado: null,
            responsable: null,
            validacion: null,
        },
    ];
    const n = normalizeContent(c);
    assert.equal(n.identificacion[0].codigo, '');
    assert.equal(n.encabezado.responsable, '');
    assert.equal(n.identificacion[0].area, 'Histórico');
    assert.equal(n.identificacion[0].tramite, 'Servicio');
});
