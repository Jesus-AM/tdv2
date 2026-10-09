import type { FormContent } from '../types/tdv2';
import { splitBlocks } from './form-blocks';
export type RowSection = 'identificacion' | 'sistemas' | 'datos' | 'acuerdos';
export type RowTarget = { section: RowSection; id: string };
export function targetRow(content: FormContent, target: RowTarget) {
    return content[target.section].find(row => row.id === target.id);
}
export function rowLabel(content: FormContent, target: RowTarget): string {
    const row = targetRow(content, target);
    if (!row) return 'El registro ya no está disponible';
    if ('tramite' in row) return [row.codigo, row.tramite].filter(Boolean).join(' · ') || 'Trámite sin nombre';
    if ('sistema' in row) return row.sistema || 'Sistema sin nombre';
    if ('dato' in row) return row.dato || 'Dato sin nombre';
    return row.acuerdo || 'Acuerdo sin descripción';
}
/** Incluye las relaciones que se modificarían, sin copiar todo el formato por cada botón de la tabla. */
export function removalBlocks(content: FormContent, target: RowTarget): string[] {
    const keys = [`${target.section}:${target.id}`], row = targetRow(content, target);
    if (row && 'codigo' in row && row.codigo) {
        content.evaluaciones[row.codigo]?.forEach((_, index) => keys.push(`evaluaciones:${row.codigo}:${index}`));
        for (const table of ['sistemas', 'datos'] as const)
            for (const linked of content[table]) if (linked.proceso === row.codigo) keys.push(`${table}:${linked.id}`);
    }
    return keys;
}
/** La confirmación incluye versiones y relaciones por ID; cambios en otras filas no la invalidan. */
export function removalSnapshot(content: FormContent, target: RowTarget, blocks: Record<string, { version: number }>): string {
    const values = splitBlocks(content);
    return JSON.stringify(removalBlocks(content, target).sort().map(key => ({ key, version: blocks[key]?.version ?? 0, value: values[key] ?? null })));
}
/** Resuelve el ID sobre el contenido vigente: nunca elimina la fila que ocupa una posición antigua. */
export function removeRow(content: FormContent, target: RowTarget) {
    const row = targetRow(content, target);
    if (!row) return;
    const index = content[target.section].findIndex(item => item.id === target.id);
    content[target.section].splice(index, 1);
    if ('codigo' in row && row.codigo) {
        delete content.evaluaciones[row.codigo];
        for (const linked of [...content.sistemas, ...content.datos]) if (linked.proceso === row.codigo) linked.proceso = '';
    }
}
