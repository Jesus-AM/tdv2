import type { ProcessRow, SystemRow } from '../types/tdv2';

export type SystemGroup = { key: string; procedure?: ProcessRow; rows: { row: SystemRow; index: number }[] };

/** Proyección únicamente: orden de Identificación y, dentro del grupo, orden original de herramientas.
 * El código confirmado es la relación persistida. Nunca agrupar por nombre, índice ni ID de catálogo. */
export function groupSystems(procedures: ProcessRow[], rows: SystemRow[]): SystemGroup[] {
    const groups: SystemGroup[] = procedures
        .filter(p => p.id !== 'inicial' || p.codigo || p.tramite.trim())
        .map(procedure => ({ key: `procedure:${procedure.id}`, procedure, rows: [] }));
    const byCode = new Map<string, SystemGroup>();
    for (const group of groups) {
        const code = group.procedure!.codigo;
        if (code && !byCode.has(code)) byCode.set(code, group);
    }
    const unassigned: SystemGroup = { key: 'unassigned', rows: [] };
    rows.forEach((row, index) => (byCode.get(row.proceso) || unassigned).rows.push({ row, index }));
    if (unassigned.rows.length) groups.push(unassigned);
    return groups;
}
