import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
const source = readFileSync(new URL('../../resources/js/lib/area-directory.ts', import.meta.url), 'utf8');
const js = ts.transpileModule(source, {
    compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 },
}).outputText;
const { buildAreaDirectory, filterAreaTree, displayUnitCode } = await import(
    `data:text/javascript;base64,${Buffer.from(js).toString('base64')}`
);
const unit = (id, level, parent, name = id) => ({
    id_ur: id,
    cve_ur: `C${id}`,
    desc_ur: name,
    nivel_ur: level,
    id_ur_pertenece: parent,
    ejercicio: 2026,
});
const a = unit('a', 2, null, 'Tecnologías'),
    b = unit('b', 3, 'a', 'Sistemas'),
    c = unit('c', 4, 'b', 'Aplicaciones'),
    d = unit('d', 2, null, 'Finanzas');
const form = (u, p, edit) => ({
    ...u,
    porcentaje: p,
    editable: edit,
    url: `/formatos/${u.id_ur}`,
    actualizado_en: null,
    actualizado_por: null,
});
const flatten = (nodes) => nodes.flatMap((n) => [n.unit.id_ur, ...flatten(n.children)]);
const data = () => buildAreaDirectory([c, d, b, a], [form(a, 0, true), form(b, 100, true), form(d, 30, false)]);

test('tipo 0 queda como puente interno, sin formato ni contador; clave no determina tipo', () => {
    const bridge = { ...unit('bridge', 3, 'a'), tipo_ur: '0' };
    const child = { ...b, id_ur_pertenece: 'bridge', cve_ur: '06000', tipo_ur: '1' };
    const tree = buildAreaDirectory([a, bridge, child], [form(a, 0, true), form(bridge, 100, true), form(child, 40, true)]);
    assert.deepEqual(flatten(tree.roots), ['a', 'b']);
    assert.equal(tree.roots[0].areaCount, 2);
    assert.equal(tree.roots[0].forms.length, 2);
    assert.equal(tree.roots[0].children[0].unit.cve_ur, '06000');
});

test('06000 y 6000 encuentran la misma UR sin modificar identificadores ni agrupar duplicados', () => {
    const original = { ...a, cve_ur: '06000' };
    const other = { ...d, cve_ur: '6000' };
    const tree = buildAreaDirectory([original, other], [form(original, 0, true), form(other, 0, false)]);
    assert.deepEqual(flatten(filterAreaTree(tree.roots, '06000', 'todos')), ['a', 'd']);
    assert.deepEqual(flatten(filterAreaTree(tree.roots, '6000', 'todos')), ['a', 'd']);
    assert.equal(displayUnitCode(original.cve_ur), '6000');
    assert.equal(original.cve_ur, '06000');
    assert.equal(displayUnitCode('000'), '0');
    assert.equal(displayUnitCode('000ABC'), '000ABC');
    assert.equal(displayUnitCode('000123456789012345678901234567890'), '123456789012345678901234567890');
});
test('directorio limitado a niveles 2 y 3 sin alterar el promedio de sus formatos', () => {
    const tree = data();
    assert.deepEqual(
        tree.roots.map((n) => n.unit.id_ur),
        ['a', 'd'],
    );
    const branch = tree.roots[0];
    assert.equal(branch.areaCount, 2);
    assert.equal(branch.forms.length, 2);
    assert.equal(branch.children[0].children.length, 0);
    assert.equal(branch.children[0].form.id_ur, 'b');
    assert.deepEqual(flatten(filterAreaTree(tree.roots, 'aplicaciones', 'todos')), []);
    assert.equal(branch.forms.reduce((s, f) => s + Number(f.porcentaje), 0) / branch.forms.length, 50);
});
test('buscar una dependencia conserva sus ancestros y normaliza acentos', () => {
    let nodes = filterAreaTree(data().roots, 'sistemas', 'todos');
    assert.deepEqual(flatten(nodes), ['a', 'b']);
    assert.equal(nodes[0].contextOnly, true);
    nodes = filterAreaTree(data().roots, 'tecnologias', 'todos');
    assert.deepEqual(flatten(nodes), ['a', 'b']);
});
test('filtros conservan contexto pero no ofrecen el formato que no coincide', () => {
    const nodes = filterAreaTree(data().roots, '', 'completos');
    assert.deepEqual(flatten(nodes), ['a', 'b']);
    assert.equal(nodes[0].contextOnly, true);
    assert.equal(nodes[0].children[0].contextOnly, false);
    assert.deepEqual(flatten(filterAreaTree(data().roots, '', 'edicion')), ['a', 'b']);
    assert.equal(filterAreaTree(data().roots, 'desconocido', 'todos').length, 0);
});
test('áreas huérfanas y ciclos quedan visibles una sola vez', () => {
    const x = unit('x', 3, 'ausente'),
        y = unit('y', 3, 'z'),
        z = unit('z', 3, 'y');
    const tree = buildAreaDirectory([z, a, y, x], []);
    const ids = flatten([...tree.roots, ...tree.others]);
    assert.equal(ids.length, 4);
    assert.equal(new Set(ids).size, 4);
    assert.equal(tree.others.length, 2);
});
test('cada área principal tiene su propio grupo incluso si el catálogo la anida', () => {
    const nested = unit('e', 2, 'a');
    const tree = buildAreaDirectory([a, nested, b, c], []);
    assert.deepEqual(
        tree.roots.map((n) => n.unit.id_ur),
        ['a', 'e'],
    );
    assert.equal(flatten(tree.roots).length, 3);
});

test('excluye otros niveles incluso con un catálogo antiguo o formatos inesperados', () => {
    const hidden = [unit('r', 1, null), c, unit('e', 5, 'c')];
    const tree = buildAreaDirectory(
        [...hidden, a, b],
        hidden.map((u) => form(u, 100, true)),
    );
    assert.deepEqual(flatten([...tree.roots, ...tree.others]), ['a', 'b']);
    assert.equal(tree.roots[0].forms.length, 0);
});
