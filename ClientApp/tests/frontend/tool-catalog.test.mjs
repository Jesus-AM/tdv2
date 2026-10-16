import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
const source = readFileSync(new URL('../../resources/js/lib/system-catalog.ts', import.meta.url), 'utf8');
const js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { toolCatalog, suiteCatalog, toolUsed, reassignmentConflict } = await import('data:text/javascript;base64,' + Buffer.from(js).toString('base64'));
const procedures = [{ id: 'p1', codigo: 'PO-01' }, { id: 'p2', codigo: 'PO-02' }];
test('catálogo exacto agrupado, Forms dentro de Microsoft y Adobe alfabético sin productos adicionales', () => {
    assert.deepEqual(toolCatalog.map(t => t.id), ['sii_v2', 'aplicaciones_web', 'aplicaciones_moviles', 'microsoft_365', 'limesurvey', 'power_bi', 'tableau', 'adobe_creative_cloud', 'otra']);
    assert.equal(toolCatalog[0].group, 'Sistemas institucionales');
    assert.equal(toolCatalog[0].label, 'Sistema Integral de Información');
    assert.equal(suiteCatalog.microsoft_365.options.length, 8);
    assert.ok(suiteCatalog.microsoft_365.options.some(([id, label]) => id === 'forms' && label === 'Forms — formularios'));
    const adobe = suiteCatalog.adobe_creative_cloud.options.map(([, label]) => label);
    assert.equal(adobe.length, 20); assert.deepEqual(adobe, [...adobe].sort((a, b) => a.localeCompare(b, 'es')));
});
test('filas locales pendientes, propia fila, reasignación, cambios y retiro actualizan disponibilidad', () => {
    const first = { id: 'a', proceso: 'PO-01', sistema: 'microsoft_365' };
    const second = { id: 'b', proceso: 'PO-01', sistema: '' };
    assert.equal(toolUsed(second, 'microsoft_365', [first, second], procedures), true);
    assert.equal(toolUsed(first, 'microsoft_365', [first, second], procedures), false);
    assert.equal(toolUsed({ ...second, proceso: 'PO-02' }, 'microsoft_365', [first], procedures), false);
    assert.equal(reassignmentConflict({ ...first, id: 'b', proceso: 'PO-02' }, 'PO-01', [first], procedures), true);
    assert.equal(toolUsed(second, 'microsoft_365', [{ ...first, sistema: 'tableau' }], procedures), false);
    assert.equal(toolUsed(second, 'microsoft_365', [], procedures), false);
    assert.equal(toolUsed({ ...second, proceso: '' }, 'microsoft_365', [{ ...first, proceso: '' }], procedures), false);
});
test('Otra conserva opción global y compara nombre exacto con espacios/caso normalizados', () => {
    const first = { id: 'a', proceso: 'PO-01', sistema: 'otra', sistemaOtro: ' Mi   herramienta ' };
    const second = { id: 'b', proceso: 'PO-02', sistema: 'otra', sistemaOtro: 'mi\tHERRAMIENTA' };
    assert.equal(toolUsed(second, 'otra', [first], procedures), false);
    assert.equal(reassignmentConflict(second, 'PO-01', [first], procedures), true);
    assert.equal(reassignmentConflict({ ...second, sistemaOtro: 'Mi herramienta 2' }, 'PO-01', [first], procedures), false);
    assert.equal(first.sistemaOtro, ' Mi   herramienta ');
});
