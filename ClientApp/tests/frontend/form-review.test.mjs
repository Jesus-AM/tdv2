import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
function moduleUrl(name) {
    const source = readFileSync(new URL(`../../resources/js/lib/${name}.ts`, import.meta.url), 'utf8');
    const js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
    const resolved = name === 'form-deletion' ? js.replaceAll("'./form-blocks'", `'${moduleUrl('form-blocks')}'`) : js;
    return `data:text/javascript;base64,${Buffer.from(resolved).toString('base64')}`;
}
const { removeRow, removalBlocks, removalSnapshot } = await import(moduleUrl('form-deletion'));
const { moduleGroups, activeModule } = await import(moduleUrl('module-navigation'));
const { questionLabel, questionOptionLabel, originLabel, countLabel } = await import(moduleUrl('form-labels'));
const { groupSystems } = await import(moduleUrl('system-groups'));

test('agrupa por código estable y orden de identificación sin ordenar ni crear filas persistidas', () => {
    const procedures = [{ id: 'p8', codigo: 'PO-08', tramite: 'Mismo nombre' }, { id: 'p9', codigo: 'PO-09', tramite: 'Mismo nombre' }, { id: 'p10', codigo: 'PO-10', tramite: 'Sin herramientas' }];
    const rows = [{ id: 'b1', proceso: 'PO-09' }, { id: 'a1', proceso: 'PO-08' }, { id: 'b2', proceso: 'PO-09' }, { id: 'orphan', proceso: 'PO-99' }, { id: 'blank', proceso: '' }];
    const before = JSON.stringify({ procedures, rows });
    const groups = groupSystems(procedures, rows);
    assert.deepEqual(groups.map(g => g.key), ['procedure:p8', 'procedure:p9', 'procedure:p10', 'unassigned']);
    assert.deepEqual(groups.map(g => g.rows.map(r => [r.row.id, r.index])), [[['a1', 1]], [['b1', 0], ['b2', 2]], [], [['orphan', 3], ['blank', 4]]]);
    assert.equal(groups[1].rows[0].row, rows[0]);
    assert.equal(JSON.stringify({ procedures, rows }), before);
});
test('renombrar, reasignar o retirar procedimientos conserva las identidades de las herramientas', () => {
    const procedures = [{ id: 'p1', codigo: 'PO-01', tramite: 'Antes' }, { id: 'p2', codigo: 'PO-02', tramite: 'Segundo' }];
    const row = { id: 's', proceso: 'PO-01', fallas: 'Respuesta íntegra' };
    procedures[0].tramite = 'Nombre completo actualizado';
    assert.equal(groupSystems(procedures, [row])[0].procedure.tramite, procedures[0].tramite);
    row.proceso = 'PO-02';
    assert.equal(groupSystems(procedures, [row])[1].rows[0].row, row);
    procedures.pop();
    const orphan = groupSystems(procedures, [row]).at(-1);
    assert.equal(orphan.key, 'unassigned'); assert.equal(orphan.rows[0].row, row); assert.equal(row.fallas, 'Respuesta íntegra');
    assert.deepEqual(groupSystems([{ id: 'inicial', codigo: '', tramite: '' }], []), []);
});

test('terminología visible adapta sólo textos conocidos sin transformar respuestas, nombres ni valores almacenados', () => {
    const stored = { pregunta: '¿Con qué frecuencia se revisan o actualizan los procesos?',
        respuesta: 'El servicio de trámites académicos', origen: 'Se origina en otro proceso' };
    const original = structuredClone(stored);
    assert.equal(questionLabel(stored.pregunta), '¿Con qué frecuencia se revisan o actualizan los procedimientos?');
    assert.equal(originLabel(stored.origen), 'Se origina en otro procedimiento');
    assert.equal(questionLabel(stored.respuesta), stored.respuesta);
    assert.equal(originLabel('Proceso oficial'), 'Proceso oficial');
    assert.equal(questionOptionLabel('No o muy pocos'), 'Ninguno o muy pocos');
    assert.equal(questionOptionLabel(stored.respuesta), stored.respuesta);
    assert.deepEqual(stored, original);
    assert.equal(countLabel(0, 'herramienta'), '0 herramientas');
    assert.equal(countLabel(1, 'procedimiento'), '1 procedimiento');
    assert.equal(countLabel(2, 'área dependiente', 'áreas dependientes'), '2 áreas dependientes');
});

test('confirmación por ID detecta versiones, nuevas relaciones y contenido vinculado sin invalidarse por otras filas', () => {
    const data = { encabezado: {}, identificacion: [{ id: 'p', codigo: 'PO-01' }], sistemas: [{ id: 's', proceso: 'PO-01', sistema: 'Sistema' }],
        datos: [], acuerdos: [], preguntas: [], medios: {}, medioOtro: '', evaluaciones: { 'PO-01': [{ valor: '3' }] } };
    const target = { section: 'identificacion', id: 'p' }, versions = { 'sistemas:s': { version: 1 } };
    const snapshot = removalSnapshot(data, target, versions);
    data.identificacion.unshift({ id: 'other', codigo: 'PO-02' });
    assert.equal(removalSnapshot(data, target, versions), snapshot);
    assert.notEqual(removalSnapshot(data, target, { 'sistemas:s': { version: 2 } }), snapshot);
    data.sistemas[0].sistema = 'Actualizado';
    assert.notEqual(removalSnapshot(data, target, versions), snapshot);
    data.sistemas[0].sistema = 'Sistema'; data.datos.push({ id: 'new-link', proceso: 'PO-01' });
    assert.notEqual(removalSnapshot(data, target, versions), snapshot);
});
test('elimina por ID aunque otra persona inserte una fila; limpia relaciones sólo del proceso elegido', () => {
    const data = { identificacion: [{ id: 'new', codigo: 'PO-03' }, { id: 'keep', codigo: 'PO-01' }, { id: 'target', codigo: 'PO-02' }],
        sistemas: [{ id: 's', proceso: 'PO-02' }], datos: [{ id: 'd', proceso: 'PO-01' }], evaluaciones: { 'PO-02': [1], 'PO-01': [2] } };
    assert.deepEqual(removalBlocks(data, { section: 'identificacion', id: 'target' }), ['identificacion:target', 'evaluaciones:PO-02:0', 'sistemas:s']);
    removeRow(data, { section: 'identificacion', id: 'target' });
    assert.deepEqual(data.identificacion.map(row => row.id), ['new', 'keep']);
    assert.equal(data.sistemas[0].proceso, ''); assert.equal(data.datos[0].proceso, 'PO-01');
    assert.deepEqual(data.evaluaciones, { 'PO-01': [2] });
});
test('una fila ausente no elimina otra; una fila ILDA se retira por su identidad estable', () => {
    const data = { identificacion: [{ id: 'ilda:1', codigo: 'PO-01' }], sistemas: [], datos: [], evaluaciones: { 'PO-01': [] } };
    const before = structuredClone(data);
    removeRow(data, { section: 'identificacion', id: 'missing' });
    assert.deepEqual(data, before);
    removeRow(data, { section: 'identificacion', id: 'ilda:1' });
    assert.deepEqual(data.identificacion, []); assert.deepEqual(data.evaluaciones, {});
});
test('alias del módulo conserva una sola entrada y selección activa del formato', () => {
    const modules = [{ key: 'procedimientos_institucionales', route: '/inicio' }, { key: 'procesos_operativos', route: '/inicio' }];
    const groups = moduleGroups(modules, 'https://tdv2.test');
    assert.equal(groups.length, 1);
    assert.equal(activeModule(groups.map(g => g.module), '/formatos/A', 'https://tdv2.test').key, 'procedimientos_institucionales');
});
test('navegación respeta padres publicados, evita duplicados y no eleva huérfanos', () => {
    const modules = [{ key: 'procesos_operativos', route: '/inicio' }, { key: 'configuracion', route: '/configuracion' },
        { key: 'sincronizaciones', parent: 'configuracion', route: '/configuracion/sincronizaciones' },
        { key: 'pruebas_acceso', parent: 'ausente', route: '/configuracion/pruebas-acceso' },
        { key: 'configuracion', route: '/configuracion' }, { key: 'external', route: 'https://foreign.test/' }];
    const groups = moduleGroups(modules, 'https://tdv2.test');
    assert.equal(groups.length, 2); assert.deepEqual(groups[1].children.map(m => m.key), ['sincronizaciones']);
    assert.equal(activeModule(groups.flatMap(g => [g.module, ...g.children]), '/configuracion/sincronizaciones', 'https://tdv2.test').key, 'sincronizaciones');
    assert.equal(moduleGroups([modules[2]], 'https://tdv2.test').length, 0);
});
