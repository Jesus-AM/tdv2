// Compara una instantánea local previa y el motor vigente; jamás recupera archivos de GitHub.
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import ts from 'typescript';
const [sourceRoot, output] = process.argv.slice(2);
if (!sourceRoot || !output) throw new Error('Uso: node measure-capture.mjs <directorio lib/snapshot> <salida.json>');
function module(name, replacements = {}) {
    let js = ts.transpileModule(readFileSync(path.join(sourceRoot, name + '.ts'), 'utf8'), {
        compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 },
    }).outputText;
    for (const [key, value] of Object.entries(replacements)) js = js.replaceAll(`'${key}'`, `'${value}'`);
    return `data:text/javascript;base64,${Buffer.from(js).toString('base64')}`;
}
const blockModule = module('form-blocks');
const { BlockEditor } = await import(module('block-editor', { './form-editor': module('form-editor'), './form-blocks': blockModule }));
const { splitBlocks } = await import(blockModule);
const report = { synthetic: true, scope: 'CPU del motor; sin React, HTTP ni PostgreSQL', cases: [] };
for (const count of [20, 100, 200]) {
    const content = { encabezado: {}, identificacion: Array.from({ length: count }, (_, i) => ({ id: `s-${i}`, tramite: '', codigo: '', responsable: 'Texto sintético'.repeat(8) })),
        sistemas: [], datos: [], acuerdos: [], preguntas: [], evaluaciones: {}, medios: {}, medioOtro: '' };
    const result = body => ({ bloques: body.blocks.map(b => ({ ...b, value: splitBlocks(content)[b.key], reserva: { id: 'test-lease', propia: true, titular: 'Sintética', venceEn: new Date(Date.now() + 45000).toISOString() } })), servidorEn: new Date().toISOString() });
    const editor = new BlockEditor(content, 0, 0, true, { reserve: async body => result(body), renew: async body => result(body), release: async () => {} }, () => {});
    editor.receive({ contenido: content, version: 0, porcentaje: 0, editable: true, bloques: [], servidorEn: new Date().toISOString() });
    await editor.focus('identificacion:s-0');
    const times = [];
    for (let i = 0; i < 1000; i++) {
        const start = performance.now(); editor.changeBlock('identificacion:s-0', row => { row.tramite = `Texto sintético ${i}`; });
        if (i >= 100) times.push(performance.now() - start);
    }
    editor.dispose(); times.sort((a,b) => a-b);
    report.cases.push({ records: count, samples: times.length, averageMs: times.reduce((a,b)=>a+b,0)/times.length, p95Ms: times[Math.floor(times.length*.95)] });
}
writeFileSync(output, JSON.stringify(report, null, 2)); console.log(JSON.stringify(report));
