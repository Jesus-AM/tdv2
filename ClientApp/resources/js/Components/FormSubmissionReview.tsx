import { Box, Button, Typography } from '@mui/material';
export type FormPending = { seccion: string; bloque: string; campo: string; mensaje: string };
export type SubmissionReview = { listo: boolean; pendientes: FormPending[] };
export type StageReview = {
    etapa: { id: number; nombre: string; habilitada: boolean; estado: string; ejercicio: number; enviadoEn: string | null; enviadoPor: string | null; enviadoNombre: string | null };
    revision: SubmissionReview;
    secciones: { id: string; nombre: string; completa: boolean }[];
    resumen: { procedimientos: number; herramientas: number; medios: string[] };
};
export default function FormSubmissionReview({ delivery, area, canSubmit, settled, submitting, busy, autosave, onNavigate, onSubmit }: {
    delivery: StageReview | null; area: string; canSubmit: boolean; settled: boolean; submitting: boolean; busy: boolean; autosave: string;
    onNavigate: (pending: FormPending) => void; onSubmit: () => void;
}) {
    const stage = delivery?.etapa, review = delivery?.revision;
    const submitted = stage?.estado === 'enviada', legacy = stage?.estado === 'historico';
    return <Box component="section" aria-labelledby="submission-review-title">
        <Typography id="submission-review-title" variant="h2" sx={{ mb: 1 }}>Revisar y enviar</Typography>
        {!delivery || !stage ? <Typography>La revisión estará disponible al recuperar el estado del servidor.</Typography> : <>
            <Typography sx={{ fontWeight: 700 }}>{stage.nombre}</Typography>
            <Typography variant="body2" sx={{ mb: 2 }}>{area} · Ejercicio {stage.ejercicio}</Typography>
            {stage.id === 1 && <Typography variant="body2" sx={{ mb: 2 }}>Esta entrega comprende Contexto, Identificación general y Sistemas y herramientas, incluidos los Medios utilizados. Las secciones posteriores se conservan para otra etapa y no se solicitan en esta entrega.</Typography>}
            <Typography variant="body2" role="status" aria-live="polite" sx={{ mb: 2 }}>Autoguardado: {autosave}</Typography>
            {legacy ? <Typography role="status">Formato enviado anteriormente. Se conserva bloqueado; el envío anterior no identifica una etapa.</Typography>
                : submitted ? <Typography role="status" sx={{ fontWeight: 600 }}>{stage.id === 1 ? 'Primera etapa enviada' : `${stage.nombre} enviada`}</Typography>
                : !stage.habilitada ? <Typography>Esta etapa todavía no está habilitada.</Typography> : null}
            {stage.enviadoEn && <Typography variant="body2" sx={{ mt: 1 }}>Enviado el {new Date(stage.enviadoEn).toLocaleString('es-MX')} por {stage.enviadoNombre || stage.enviadoPor || 'el responsable registrado'}. Sus respuestas están disponibles para consulta.</Typography>}
            {!legacy && <Box component="ul" sx={{ pl: 2.5, my: 2 }}>
                {delivery.secciones.map(section => <li key={section.id}>
                    <Typography variant="body2"><strong>{section.nombre}:</strong> {section.completa ? 'Completa' : 'Con pendientes'}</Typography>
                </li>)}
            </Box>}
            <Box sx={{ my: 2 }}>
                <Typography variant="body2">Registros de procedimientos: <strong>{delivery.resumen.procedimientos}</strong></Typography>
                <Typography variant="body2">Registros de sistemas/herramientas: <strong>{delivery.resumen.herramientas}</strong></Typography>
                <Typography variant="body2">Medios registrados: {delivery.resumen.medios.length ? delivery.resumen.medios.join(' · ') : 'Sin selección'}</Typography>
                <Typography variant="caption" color="text.secondary">Contexto es informativo. Medios utilizados conserva su captura opcional.</Typography>
            </Box>
            {!legacy && !submitted && stage.habilitada && <>
                {!settled && <Typography variant="body2" sx={{ mb: 2 }}>La revisión corresponde a las respuestas confirmadas por el servidor. Espera a que termine el guardado o corrige los errores indicados.</Typography>}
                {!!review?.pendientes.length && <>
                    <Typography sx={{ fontWeight: 600 }}>Pendientes de las respuestas guardadas</Typography>
                    <Box component="ul" sx={{ pl: 2.5 }}>
                        {review.pendientes.map((item, index) => <li key={`${item.bloque}:${item.campo}:${index}`}>
                            <Button onClick={() => onNavigate(item)} sx={{ textAlign: 'left', justifyContent: 'flex-start', py: .5 }}>{item.mensaje}</Button>
                        </li>)}
                    </Box>
                </>}
                {review?.listo && <Typography variant="body2" sx={{ mb: 2 }}>El servidor confirmó que las respuestas guardadas cumplen los requisitos de llenado.</Typography>}
                {busy && <Typography variant="body2" role="status" sx={{ mb: 2 }}>Otra sesión está editando. Espera a que guarde y termine antes de enviar.</Typography>}
                {canSubmit ? <Button variant="contained" disabled={submitting || !settled || !review?.listo || busy} onClick={onSubmit}>Enviar {stage.nombre.toLocaleLowerCase('es-MX')}</Button>
                    : <Typography variant="body2" color="text.secondary">Puedes consultar esta revisión. El envío requiere permiso del responsable autorizado sobre este formato.</Typography>}
            </>}
        </>}
    </Box>;
}
