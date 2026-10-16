import { Box, Button, Typography } from '@mui/material';
import { CheckCircleOutlined, PendingOutlined, ArrowForward } from '@mui/icons-material';
import { useId, type ReactNode } from 'react';
import { countLabel } from '@/lib/form-labels';
export type FormPending = { seccion: string; bloque: string; campo: string; mensaje: string };
export type SubmissionReview = { listo: boolean; pendientes: FormPending[] };
export type ProcedureCoverage = { id: string; codigo: string; nombre: string; aplicable: boolean; completa: boolean;
    registros: number; validos: number; destino: FormPending };
export type StageReview = {
    cobertura?: ProcedureCoverage[];
    etapa: { id: number; nombre: string; habilitada: boolean; estado: string; ejercicio: number; enviadoEn: string | null; enviadoPor: string | null; enviadoNombre: string | null };
    revision: SubmissionReview;
    secciones: { id: string; nombre: string; completa: boolean }[];
    resumen: { procedimientos: number; herramientas: number };
};
export default function FormSubmissionReview({ delivery, area, canSubmit, settled, submitting, busy, autosave, onNavigate, onSubmit, saveAction }: {
    delivery: StageReview | null; area: string; canSubmit: boolean; settled: boolean; submitting: boolean; busy: boolean; autosave: string;
    onNavigate: (pending: FormPending) => void; onSubmit: () => void;
    saveAction?: ReactNode;
}) {
    const stage = delivery?.etapa, review = delivery?.revision;
    const submitted = stage?.estado === 'enviada', legacy = stage?.estado === 'historico';
    const pendingId = useId();
    return <Box component="section" aria-labelledby="submission-review-title">
        <Typography id="submission-review-title" variant="h2" sx={{ mb: 1 }}>Resumen del formato</Typography>
        {!delivery || !stage ? <Typography>La revisión estará disponible al recuperar el estado del servidor.</Typography> : <>
            <Typography variant="body2" sx={{ mb: 2 }}>{area} · Ejercicio {stage.ejercicio}</Typography>
            {stage.id === 1 && <Typography variant="body2" sx={{ mb: 2 }}>Esta entrega comprende Contexto, Identificación general y Sistemas y herramientas. Las secciones posteriores se conservan para otra etapa y no se solicitan en esta entrega.</Typography>}
            <Typography variant="body2" role="status" aria-live="polite" sx={{ mb: 1.5, minHeight: 24 }}>Autoguardado: {autosave}</Typography>
            {legacy ? <Typography role="status">Formato enviado anteriormente. Se conserva bloqueado; el envío anterior no identifica una etapa.</Typography>
                : submitted ? <Typography role="status" sx={{ fontWeight: 600 }}>Formato enviado</Typography>
                : !stage.habilitada ? <Typography>Esta etapa todavía no está habilitada.</Typography> : null}
            {stage.enviadoEn && <Typography variant="body2" sx={{ mt: 1 }}>Enviado el {new Date(stage.enviadoEn).toLocaleString('es-MX')} por {stage.enviadoNombre || stage.enviadoPor || 'el responsable registrado'}. Sus respuestas están disponibles para consulta.</Typography>}
            {!legacy && <Box component="ul" aria-label="Resumen de secciones" sx={{ p: 0, my: 2, listStyle: 'none' }}>
                {delivery.secciones.map(section => <Box component="li" key={section.id} sx={{ display: 'grid',
                    gridTemplateColumns: { xs: 'minmax(0,1fr)', sm: 'minmax(0,1fr) minmax(170px,1fr) 130px' },
                    gap: .5, py: 1, borderBottom: '1px solid', borderColor: 'divider', alignItems: 'center' }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{section.nombre}</Typography>
                    <Typography variant="body2" color="text.secondary">{section.id === 'identificacion'
                        ? countLabel(delivery.resumen.procedimientos, 'procedimiento') : section.id === 'sistemas'
                            ? countLabel(delivery.resumen.herramientas, 'herramienta') : 'Informativo'}</Typography>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: .75 }}>
                        {section.completa ? <CheckCircleOutlined color="success" sx={{ fontSize: 18 }} /> : <PendingOutlined color="warning" sx={{ fontSize: 18 }} />}
                        <Typography variant="body2">{section.completa ? 'Completa' : 'Con pendientes'}</Typography>
                    </Box>
                </Box>)}
            </Box>}
            <Box sx={{ my: 1 }}>
                {legacy && <Typography variant="body2">{countLabel(delivery.resumen.procedimientos, 'procedimiento')} · {countLabel(delivery.resumen.herramientas, 'herramienta')}</Typography>}
                <Typography variant="caption" color="text.secondary">Contexto es informativo. La captura de sistemas y herramientas es opcional. Completa las filas que inicies.</Typography>
            </Box>
            {!legacy && !submitted && stage.habilitada && <>
                {!settled && <Typography variant="body2" sx={{ mb: 2 }}>La revisión corresponde a las respuestas confirmadas por el servidor. Espera a que termine el guardado o corrige los errores indicados.</Typography>}
                {!!review?.pendientes.length && <>
                    <Typography sx={{ fontWeight: 600 }}>Pendientes</Typography>
                    <Box component="ul" sx={{ p: 0, my: 1, listStyle: 'none' }}>
                        {review.pendientes.map((item, index) => {
                            return <Box component="li" key={`${item.bloque}:${item.campo}`}
                            data-pending-block={item.bloque} data-pending-field={item.campo}
                            sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 1, py: 1, borderBottom: '1px solid', borderColor: 'divider' }}>
                            <Typography id={`${pendingId}-${index}`} variant="body2" sx={{ flex: '1 1 280px', overflowWrap: 'anywhere' }}>{item.mensaje}</Typography>
                            <Button aria-describedby={`${pendingId}-${index}`} endIcon={<ArrowForward />}
                                onClick={() => onNavigate(item)} sx={{ flexShrink: 0 }}>Ir al campo</Button>
                        </Box>;
                        })}
                    </Box>
                </>}
                <Box sx={{ mt: 2, pt: 2, borderTop: '1px solid', borderColor: 'divider' }}>
                {busy && <Typography variant="body2" role="status" sx={{ mb: 2 }}>Otra sesión está editando. Espera a que guarde y termine antes de enviar.</Typography>}
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, alignItems: 'center', '& .MuiButton-root': { minHeight: 40 } }}>
                {saveAction}
                {canSubmit ? <Button variant="contained" color="primary" loading={submitting} disabled={submitting || !settled || !review?.listo || busy} onClick={onSubmit}>{submitting ? 'Comprobando…' : 'Enviar formato'}</Button>
                    : <Typography variant="body2" color="text.secondary">Puedes consultar esta revisión. El envío requiere permiso del responsable autorizado sobre este formato.</Typography>}
                </Box>
                </Box>
            </>}
        </>}
    </Box>;
}
