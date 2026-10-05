import type { FormContent } from '../types/tdv2';
export type BlockValue = unknown;
export function splitBlocks(content: FormContent): Record<string, BlockValue> {
    const blocks: Record<string, BlockValue> = {
        contexto: structuredClone(content.encabezado),
        medios: { medios: structuredClone(content.medios), medioOtro: content.medioOtro },
    };
    for (const table of ['identificacion', 'sistemas', 'datos', 'acuerdos'] as const)
        for (const row of content[table]) blocks[`${table}:${row.id}`] = structuredClone(row);
    content.preguntas.forEach((row, i) => { blocks[`preguntas:${i}`] = structuredClone(row); });
    for (const [code, rows] of Object.entries(content.evaluaciones))
        rows.forEach((row, i) => { blocks[`evaluaciones:${code}:${i}`] = structuredClone(row); });
    return blocks;
}
export const sameBlock = (a: unknown, b: unknown) => JSON.stringify(a ?? null) === JSON.stringify(b ?? null);
export function changedBlocks(base: FormContent, next: FormContent): string[] {
    const a = splitBlocks(base), b = splitBlocks(next);
    return [...new Set([...Object.keys(a), ...Object.keys(b)])].filter(key => !sameBlock(a[key], b[key]));
}
export function applyBlocks(original: FormContent, changes: Record<string, BlockValue>): FormContent {
    const result = structuredClone(original);
    for (const [key, value] of Object.entries(changes)) {
        if (key === 'contexto') result.encabezado = structuredClone(value) as FormContent['encabezado'];
        else if (key === 'medios') Object.assign(result, structuredClone(value));
        else if (key.startsWith('preguntas:')) result.preguntas[Number(key.slice(10))] = structuredClone(value) as FormContent['preguntas'][0];
        else if (key.startsWith('evaluaciones:')) {
            const [, code, i] = key.split(':');
            result.evaluaciones[code] ||= [];
            result.evaluaciones[code][Number(i)] = structuredClone(value) as FormContent['evaluaciones'][string][0];
        } else {
            const separator = key.indexOf(':'), table = key.slice(0, separator) as 'identificacion', id = key.slice(separator + 1);
            const rows = result[table], index = rows.findIndex(row => row.id === id);
            if (value == null) { if (index >= 0) rows.splice(index, 1); }
            else if (index < 0) rows.push(structuredClone(value) as typeof rows[number]);
            else rows[index] = structuredClone(value) as typeof rows[number];
        }
    }
    for (const [code, rows] of Object.entries(result.evaluaciones)) if (rows.every(row => row == null)) delete result.evaluaciones[code];
    return result;
}
