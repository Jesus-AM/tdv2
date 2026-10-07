import type { FormContent } from '../types/tdv2';
export type BlockValue = unknown;
export function splitBlocks(content: FormContent): Record<string, BlockValue> {
    const blocks: Record<string, BlockValue> = {
        contexto: content.encabezado,
        medios: { medios: content.medios, medioOtro: content.medioOtro },
    };
    for (const table of ['identificacion', 'sistemas', 'datos', 'acuerdos'] as const)
        for (const row of content[table]) blocks[`${table}:${row.id}`] = row;
    content.preguntas.forEach((row, i) => { blocks[`preguntas:${i}`] = row; });
    for (const [code, rows] of Object.entries(content.evaluaciones))
        rows.forEach((row, i) => { blocks[`evaluaciones:${code}:${i}`] = row; });
    return blocks;
}
export const sameBlock = (a: unknown, b: unknown) => a === b || JSON.stringify(a ?? null) === JSON.stringify(b ?? null);
export function blockValue(content: FormContent, key: string): unknown {
    if (key === 'contexto') return content.encabezado;
    if (key === 'medios') return { medios: content.medios, medioOtro: content.medioOtro };
    const [table, id, index] = key.split(':');
    if (table === 'preguntas') return content.preguntas[Number(id)];
    if (table === 'evaluaciones') return content.evaluaciones[id]?.[Number(index)];
    return content[table as 'identificacion'].find(row => row.id === key.slice(table.length + 1));
}
export function changedBlocks(base: FormContent, next: FormContent): string[] {
    if (base === next) return [];
    const keys: string[] = [];
    if (!sameBlock(base.encabezado, next.encabezado)) keys.push('contexto');
    if (!sameBlock(base.medios, next.medios) || base.medioOtro !== next.medioOtro) keys.push('medios');
    for (const table of ['identificacion', 'sistemas', 'datos', 'acuerdos'] as const) {
        if (base[table] === next[table]) continue;
        const before = new Map<string, unknown>(base[table].map(row => [row.id, row]));
        for (const row of next[table]) { if (!sameBlock(before.get(row.id), row)) keys.push(`${table}:${row.id}`); before.delete(row.id); }
        for (const id of before.keys()) keys.push(`${table}:${id}`);
    }
    if (base.preguntas !== next.preguntas) for (let i = 0; i < Math.max(base.preguntas.length, next.preguntas.length); i++)
        if (!sameBlock(base.preguntas[i], next.preguntas[i])) keys.push(`preguntas:${i}`);
    if (base.evaluaciones !== next.evaluaciones) for (const code of new Set([...Object.keys(base.evaluaciones), ...Object.keys(next.evaluaciones)])) {
        const a = base.evaluaciones[code] || [], b = next.evaluaciones[code] || [];
        if (a === b) continue;
        for (let i = 0; i < Math.max(a.length, b.length); i++) if (!sameBlock(a[i], b[i])) keys.push(`evaluaciones:${code}:${i}`);
    }
    return keys;
}
export function applyBlocks(original: FormContent, changes: Record<string, BlockValue>): FormContent {
    let result = original;
    for (const [key, value] of Object.entries(changes)) {
        if (sameBlock(blockValue(original, key), value)) continue;
        if (result === original) result = { ...original };
        if (key === 'contexto') result.encabezado = structuredClone(value) as FormContent['encabezado'];
        else if (key === 'medios') Object.assign(result, structuredClone(value));
        else if (key.startsWith('preguntas:')) {
            if (result.preguntas === original.preguntas) result.preguntas = [...original.preguntas];
            result.preguntas[Number(key.slice(10))] = structuredClone(value) as FormContent['preguntas'][0];
        }
        else if (key.startsWith('evaluaciones:')) {
            const [, code, i] = key.split(':');
            if (result.evaluaciones === original.evaluaciones) result.evaluaciones = { ...original.evaluaciones };
            if (!result.evaluaciones[code] || result.evaluaciones[code] === original.evaluaciones[code]) result.evaluaciones[code] = [...(original.evaluaciones[code] || [])];
            result.evaluaciones[code][Number(i)] = structuredClone(value) as FormContent['evaluaciones'][string][0];
        } else {
            const separator = key.indexOf(':'), table = key.slice(0, separator) as 'identificacion', id = key.slice(separator + 1);
            if (result[table] === original[table]) result[table] = [...original[table]];
            const rows = result[table], index = rows.findIndex(row => row.id === id);
            if (value == null) { if (index >= 0) rows.splice(index, 1); }
            else if (index < 0) rows.push(structuredClone(value) as typeof rows[number]);
            else rows[index] = structuredClone(value) as typeof rows[number];
        }
    }
    if (result.evaluaciones !== original.evaluaciones)
        for (const [code, rows] of Object.entries(result.evaluaciones)) if (rows.every(row => row == null)) delete result.evaluaciones[code];
    return result;
}
