// Medición reproducible exclusivamente sintética; no accede a identidad ni PostgreSQL.
import { readFileSync, mkdirSync, writeFileSync } from 'node:fs';
import ts from 'typescript';
function module(name, replacements = {}) {
    let js = ts.transpileModule(readFileSync(new URL(`../../resources/js/lib/${name}.ts`, import.meta.url), 'utf8'), {
        compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 },
    }).outputText;
    for (const [key, value] of Object.entries(replacements)) js = js.replaceAll(`'${key}'`, `'${value}'`);
    return `data:text/javascript;base64,${Buffer.from(js).toString('base64')}`;
}
const blocks = module('form-blocks');
const { BlockEditor } = await import(module('block-editor', { './form-editor': module('form-editor'), './form-blocks': blocks }));
const { splitBlocks } = await import(blocks);
const content = { encabezado: {}, identificacion: [], sistemas: [], datos: [], acuerdos: [], preguntas: [], evaluaciones: {}, medios: {}, medioOtro: '' };
for (const table of ['identificacion', 'sistemas', 'datos', 'acuerdos'])
    content[table] = Array.from({ length: 200 }, (_, i) => ({ id: `sintetico-${i}`, nombre: `Registro sintético ${i}`, descripcion: 'Contenido sintético de prueba'.repeat(8) }));
let notifications = 0;
const result = body => ({ bloques: body.blocks.map(b => ({ ...b, value: splitBlocks(content)[b.key], reserva: { id: 'sintetica', propia: true, titular: 'Prueba', venceEn: new Date(Date.now() + 45000).toISOString() } })), servidorEn: new Date().toISOString() });
const editor = new BlockEditor(content, 0, 0, true, { reserve: async b => result(b), renew: async b => result(b) }, () => notifications++);
editor.receive({ contenido: content, version: 0, porcentaje: 0, editable: true, bloques: [], servidorEn: new Date().toISOString() });
await editor.focus('identificacion:sintetico-0');
const times = [];
for (let i = 0; i < 150; i++) {
    const start = performance.now();
    if (editor.changeBlock) editor.changeBlock('identificacion:sintetico-0', row => { row.nombre = `Cambio ${i}`; });
    else editor.change(c => { c.identificacion[0].nombre = `Cambio ${i}`; });
    times.push(performance.now() - start);
}
editor.dispose(); times.sort((a, b) => a - b);
const report = { synthetic: true, rows: 800, keystrokes: 150, averageMs: times.reduce((a, b) => a + b, 0) / times.length, p95Ms: times[Math.floor(times.length * .95)], notifications };
mkdirSync('.artifacts/performance', { recursive: true });
writeFileSync(`.artifacts/performance/editor-${process.argv[2] || 'after'}.json`, JSON.stringify(report, null, 2));
console.log(JSON.stringify(report));
