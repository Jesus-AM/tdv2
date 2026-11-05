import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import ts from 'typescript';
const source = readFileSync(new URL('../../resources/js/competencias/surveyInstructions.ts', import.meta.url), 'utf8');
const js = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
const { pendingTools, markInitial, sectionInstructions, optionLabels } = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const tools = ['empty', 'initialized', 'partial', 'na', 'initial', 'advanced'].map(id => ({ id }));
const answers = { initialized: { state: 'pending' }, partial: { state: 'level', checks: '100000000000' }, na: { state: 'na' }, initial: { state: 'level', checks: '000000000000' }, advanced: { state: 'level', checks: '111011101110' }, otherCategory: { state: 'level', level: 3 }, optional: { state: 'unused' } };
test('Inicial masivo sólo completa dos vacíos; conserva NA, parcial, cero, otras categorías y optativas', () => {
    assert.deepEqual(pendingTools(tools, answers).map(t => t.id), ['empty', 'initialized']);
    const snapshot = structuredClone(answers), result = markInitial(tools, answers, 'ciber');
    assert.deepEqual(answers, snapshot);
    assert.deepEqual(result.empty, { state: 'level', checks: '000000000000' });
    for (const id of ['partial', 'na', 'initial', 'advanced', 'otherCategory', 'optional']) assert.deepEqual(result[id], answers[id]);
    assert.equal(pendingTools(tools, result).length, 0);
    assert.deepEqual(markInitial(tools, result, 'ciber'), result);
});
test('M365/IA usan cero explícito; se puede editar después sin alterar otras respuestas', () => {
    for (const block of ['m365', 'ia']) {
        const result = markInitial([{ id: 'word' }], {}, block);
        assert.equal(result.word.level, 0);
        result.word = { state: 'level', level: 2 };
        assert.equal(markInitial([{ id: 'word' }], result, block).word.level, 2);
    }
});
test('Instrucciones de seis apartados: opciones explicativas, sin respuesta seleccionable y niveles consecutivos por rúbrica', () => {
    assert.deepEqual(Object.keys(sectionInstructions), ['perfil', 'm365', 'ciber', 'datos', 'sii', 'ia']);
    assert.deepEqual(optionLabels, ['Básico, Intermedio o Avanzado.', 'Inicial.', 'No aplica.', 'Sin respuesta.']);
    assert.equal(sectionInstructions.perfil.options, undefined);
    for (const id of ['ciber', 'datos', 'sii']) {
        assert.equal(sectionInstructions[id].consecutiveLevels, true);
        assert.match(sectionInstructions[id].options[0], /al menos 3 de sus 4 actividades/);
    }
    for (const id of ['m365', 'ia']) {
        assert.equal(sectionInstructions[id].consecutiveLevels, undefined);
        assert.match(sectionInstructions[id].options[0], /incluidas las de los niveles anteriores/);
    }
});
