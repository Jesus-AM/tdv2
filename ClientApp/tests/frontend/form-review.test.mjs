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
test('una fila ausente o de ILDA no elimina otra fila', () => {
    const data = { identificacion: [{ id: 'ilda:1', codigo: 'PO-01' }] };
    const before = structuredClone(data);
    removeRow(data, { section: 'identificacion', id: 'missing' });
    removeRow(data, { section: 'identificacion', id: 'ilda:1' });
    assert.deepEqual(data, before);
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
