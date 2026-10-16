export type CatalogId = 'sii' | 'sii_modulos' | 'ilda';
export const sqlCatalogs: CatalogId[] = ['sii', 'sii_modulos'];
export type SyncResult = { estado: string; mensaje?: string; completada_en?: string; resumen?: { unidades?: number; registros?: number; columnas?: number; ejercicio?: number } };
export type SyncRun = { id: string; fuentes: string; origen: string; solicitado_por: string; estado: string; etapa: string | null;
    solicitada_en: string; iniciada_en: string | null; terminada_en: string | null; resultado: Record<string, SyncResult> };
export const catalogNames: Record<CatalogId, string> = { sii: 'Unidades responsables', sii_modulos: 'Módulos SII',
    ilda: 'ILDA' };
export function catalogAttempt(catalog: CatalogId, history: SyncRun[], activeId: string | null) {
    const source = catalog === 'ilda' ? 'ilda' : 'sii';
    const includes = (run: SyncRun) => run.fuentes === source || run.fuentes === 'ambas';
    const run = history.find(run => run.id === activeId && includes(run)) || history.find(includes);
    if (!run) return { state: 'sin_intento', run: undefined, result: undefined };
    const result = run.resultado[catalog];
    // El resultado por catálogo prevalece: SII puede completar UR y fallar en módulos.
    if (result) return { state: result.estado, run, result };
    if (run.estado === 'pendiente' || run.estado === 'ejecutando')
        return { state: run.estado === 'ejecutando' && run.etapa === catalog ? 'ejecutando' : 'pendiente', run, result };
    if (run.resultado.interrupcion) return { state: 'fallida', run, result: run.resultado.interrupcion };
    // El historial está limitado por el servidor; ni un estado global ni un contador local prueban el resultado ausente.
    return { state: 'sin_resultado', run, result };
}
export function processedCount(result?: SyncResult) {
    return result?.resumen?.unidades ?? result?.resumen?.registros ?? null;
}

export function sqlServerAttempt(history: SyncRun[], activeId: string | null) {
    const units = catalogAttempt('sii', history, activeId);
    const states = sqlCatalogs.map(catalog => catalogAttempt(catalog, history, activeId).state);
    const complete = (run: SyncRun) => sqlCatalogs.every(catalog => run.resultado[catalog]?.estado === 'completada');
    const success = history.find(complete);
    const state = states.every(s => s === 'completada') ? 'completada'
        : units.run?.estado === 'ejecutando' ? 'ejecutando' : units.run?.estado === 'pendiente' ? 'pendiente'
        : states.every(s => s === 'fallida') ? 'fallida' : states.includes('fallida') && states.includes('completada') ? 'parcial'
        : units.run ? 'sin_resultado' : 'sin_intento';
    return { state, lastSuccess: success?.terminada_en || success?.resultado.sii_modulos?.completada_en || null };
}
