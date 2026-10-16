import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
const source = readFileSync(new URL('../../resources/js/lib/sync-presentation.ts', import.meta.url), 'utf8');
const js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { catalogAttempt, processedCount, sqlServerAttempt } = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const run = changes => ({ id: 'a', fuentes: 'sii', estado: 'ejecutando', etapa: 'sii', resultado: {}, ...changes });

test('SQL Server sólo completa con UR y módulos confirmados y no mezcla éxitos de intentos distintos', () => {
    const success = run({ id: 'old', estado: 'completada', terminada_en: '2026-10-08T12:00:00Z', resultado: { sii: { estado: 'completada' }, sii_modulos: { estado: 'completada' }, sii_moviles: { estado: 'completada' }, sii_web: { estado: 'completada' } } });
    const partial = run({ estado: 'parcial', resultado: { sii: { estado: 'completada' }, sii_modulos: { estado: 'fallida' } } });
    const failure = run({ estado: 'fallida', resultado: { sii: { estado: 'fallida' }, sii_modulos: { estado: 'fallida' }, sii_moviles: { estado: 'fallida' }, sii_web: { estado: 'fallida' } } });
    assert.deepEqual(sqlServerAttempt([success], null), { state: 'completada', lastSuccess: success.terminada_en });
    assert.deepEqual(sqlServerAttempt([partial, success], null), { state: 'parcial', lastSuccess: success.terminada_en });
    assert.deepEqual(sqlServerAttempt([failure, success], null), { state: 'fallida', lastSuccess: success.terminada_en });
    assert.equal(sqlServerAttempt([partial, run({ estado: 'parcial', resultado: { sii: { estado: 'fallida' }, sii_modulos: { estado: 'completada' } } })], null).lastSuccess, null);
    assert.equal(sqlServerAttempt([run({ resultado: { sii: { estado: 'completada' } } })], 'a').state, 'ejecutando');
    const oldSuccess = run({ estado: 'completada', resultado: { sii: { estado: 'completada' }, sii_modulos: { estado: 'completada' } } });
    assert.deepEqual(sqlServerAttempt([oldSuccess], null), { state: 'completada', lastSuccess: null });
    for (const catalog of ['sii_moviles', 'sii_web']) {
        const result = structuredClone(success); result.estado = 'parcial'; result.resultado[catalog] = { estado: 'fallida' };
        assert.deepEqual(sqlServerAttempt([result, success], null), { state: 'completada', lastSuccess: success.terminada_en });
    }
});
test('UR en curso y módulos esperando; ILDA conserva su último intento independiente', () => {
    const history = [run({}), run({ id: 'i', fuentes: 'ilda', estado: 'completada', resultado: { ilda: { estado: 'completada', resumen: { registros: 5 } } } })];
    assert.equal(catalogAttempt('sii', history, 'a').state, 'ejecutando');
    assert.equal(catalogAttempt('sii_modulos', history, 'a').state, 'pendiente');
    assert.equal(catalogAttempt('ilda', history, 'a').state, 'completada');
});
test('resultado parcial distingue éxito UR de fallo módulos sin inventar un contador', () => {
    const history = [run({ estado: 'parcial', resultado: { sii: { estado: 'completada', resumen: { unidades: 23 } }, sii_modulos: { estado: 'fallida', mensaje: 'No se pudo sincronizar.' } } })];
    assert.equal(catalogAttempt('sii', history, null).state, 'completada');
    assert.equal(catalogAttempt('sii_modulos', history, null).state, 'fallida');
    assert.equal(processedCount(catalogAttempt('sii_modulos', history, null).result), null);
    assert.equal(processedCount(catalogAttempt('sii', history, null).result), 23);
});
test('recuento cero confirmado es distinto de un resultado sin recuento', () => {
    assert.equal(processedCount({ resumen: { registros: 0 } }), 0);
    assert.equal(processedCount({ estado: 'completada' }), null);
});
test('resultado global no sustituye resultados ausentes ni inventa intentos fuera del historial', () => {
    assert.equal(catalogAttempt('sii_modulos', [run({ estado: 'completada' })], null).state, 'sin_resultado');
    assert.equal(catalogAttempt('ilda', [run({})], 'a').state, 'sin_intento');
});
test('interrupción conserva etapas confirmadas y señala las que no tienen resultado', () => {
    const history = [run({ fuentes: 'ambas', estado: 'parcial', resultado: { sii: { estado: 'completada' }, interrupcion: { estado: 'fallida', mensaje: 'Ejecución interrumpida.' } } })];
    assert.equal(catalogAttempt('sii', history, null).state, 'completada');
    assert.equal(catalogAttempt('sii_modulos', history, null).state, 'fallida');
    assert.equal(catalogAttempt('ilda', history, null).result.mensaje, 'Ejecución interrumpida.');
});
