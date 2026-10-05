import { Box, Typography } from '@mui/material';
import { changedBlocks, splitBlocks, sameBlock } from '@/lib/form-blocks';
import type { FormContent } from '@/types/tdv2';

const labels: Record<string, string> = {
    contexto: 'Contexto', medios: 'Medios de atención', identificacion: 'Identificación general',
    sistemas: 'Sistemas y herramientas', datos: 'Datos', acuerdos: 'Acuerdos',
    preguntas: 'Pregunta', evaluaciones: 'Evaluación', fecha: 'Fecha', area: 'Área',
    responsable: 'Responsable', prioridad: 'Prioridad', fuente: 'Fuente', tramite: 'Trámite',
    usuario: 'Usuario', resultado: 'Resultado', validacion: 'Validación', proceso: 'Proceso',
    procesos: 'Procesos', sistema: 'Sistema', uso: 'Uso', estado: 'Estado', fallas: 'Fallas',
    dato: 'Dato', origen: 'Origen', detalle: 'Detalle', acuerdo: 'Acuerdo',
    respuesta: 'Respuesta', marcada: 'Revisada', valor: 'Valor', obs: 'Observaciones', medioOtro: 'Otro medio',
};
function fields(value: unknown, prefix = ''): Record<string, string> {
    if (value == null) return {};
    if (typeof value !== 'object' || Array.isArray(value))
        return { [prefix]: Array.isArray(value) ? value.join(', ') : typeof value === 'boolean' ? value ? 'Sí' : 'No' : String(value) };
    return Object.fromEntries(Object.entries(value).filter(([key]) => !['id', 'codigo', 'tipo', 'opciones', 'criterio', 'pregunta'].includes(key))
        .flatMap(([key, item]) => Object.entries(fields(item, prefix ? `${prefix} · ${labels[key] || key}` : labels[key] || key))));
}

/** Compara sólo las respuestas diferentes; los identificadores técnicos no forman parte de la captura. */
export default function FormProposalComparison({ proposal, shared }: { proposal: FormContent; shared: FormContent }) {
    const mine = splitBlocks(proposal), theirs = splitBlocks(shared);
    const differences = changedBlocks(shared, proposal);
    return <Box className="stack">
        {!differences.length && <Typography>No hay diferencias con las respuestas compartidas.</Typography>}
        {differences.map(key => {
            const [section, code, criterion] = key.split(':');
            const row = (mine[key] || theirs[key]) as Record<string, unknown> | undefined;
            const detail = section === 'preguntas' ? `${Number(code) + 1}. ${row?.pregunta || ''}`
                : section === 'evaluaciones' ? `${code} · ${row?.criterio || Number(criterion) + 1}`
                : String(row?.codigo || row?.tramite || row?.sistema || row?.dato || row?.acuerdo || '');
            const a = fields(mine[key]), b = fields(theirs[key]);
            return <Box key={key} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, p: 2, overflowWrap: 'anywhere' }}>
                <Typography variant="h3" sx={{ mb: 1 }}>{labels[section]}{detail && ` · ${detail}`}</Typography>
                {mine[key] == null && <Typography color="warning.main">Tu propuesta retira esta fila.</Typography>}
                {theirs[key] == null && <Typography>Esta fila no está en la versión compartida.</Typography>}
                {[...new Set([...Object.keys(a), ...Object.keys(b)])].filter(field => !sameBlock(a[field], b[field])).map(field =>
                    <Box key={field} sx={{ my: 1.5 }}>
                        <Typography sx={{ fontWeight: 500 }}>{field}</Typography>
                        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 1 }}>
                            <Box><Typography variant="caption">Mi propuesta</Typography><Typography sx={{ whiteSpace: 'pre-wrap' }}>{a[field] || 'Sin respuesta'}</Typography></Box>
                            <Box><Typography variant="caption">Respuesta compartida</Typography><Typography sx={{ whiteSpace: 'pre-wrap' }}>{b[field] || 'Sin respuesta'}</Typography></Box>
                        </Box>
                    </Box>)}
            </Box>;
        })}
    </Box>;
}
