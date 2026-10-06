import type { FormContent } from '../types/tdv2';
export function normalizeContent(input: FormContent): FormContent {
    const c = structuredClone(input);
    const text = <T extends object>(row: T): T =>
        Object.fromEntries(Object.entries(row).map(([key, value]) => [key, value === null ? '' : value])) as T;
    // El encabezado retirado es histórico: incluso sus nulos se conservan sin normalización.
    c.identificacion = c.identificacion.map(text);
    c.sistemas = c.sistemas.map((row) => text({ ...row, proceso: row.proceso ?? row.procesos?.[0] ?? '' }));
    c.datos = c.datos.map(text);
    c.acuerdos = c.acuerdos.map(text);
    c.medioOtro ||= '';
    c.evaluaciones = Object.fromEntries(
        Object.entries(c.evaluaciones || {}).map(([code, rows]) => [
            code,
            rows.filter((row) => row.criterio !== 'Tiempo de atención').map(text),
        ]),
    );
    c.preguntas = c.preguntas.map((q) => ({ ...q, respuesta: q.respuesta || '' }));
    return c;
}
