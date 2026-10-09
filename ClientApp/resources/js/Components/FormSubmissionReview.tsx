import { Box, Button, Typography } from '@mui/material';
export type FormPending = { seccion: string; bloque: string; campo: string; mensaje: string };
export type SubmissionReview = { listo: boolean; pendientes: FormPending[] };
export default function FormSubmissionReview({ review, canSubmit, settled, submitting, submitted, onNavigate, onSubmit }: {
    review: SubmissionReview | null; canSubmit: boolean; settled: boolean; submitting: boolean; submitted: boolean;
    onNavigate: (pending: FormPending) => void; onSubmit: () => void;

}) {
    return <Box component="section" aria-labelledby="submission-review-title" sx={{ mt: 4, pt: 3, borderTop: '1px solid', borderColor: 'divider' }}>
        <Typography id="submission-review-title" variant="h2" sx={{ mb: 2 }}>Revisión y envío de la primera etapa</Typography>
        <Typography sx={{ mb: 2 }}>Esta entrega comprende Contexto, Identificación general y Sistemas y herramientas, incluidos los Medios utilizados. Las secciones posteriores se conservan para otra etapa y no se solicitan en esta entrega.</Typography>
        {submitted ? <Typography>El formato fue enviado y está disponible para consulta.</Typography> : <>
            {!settled && <Typography sx={{ mb: 2 }}>Resuelve los cambios pendientes o errores de guardado para confirmar la revisión del servidor.</Typography>}
            {!review ? <Typography>La revisión estará disponible al recuperar el estado del servidor.</Typography> : review.pendientes.length > 0 ? <>
                <Typography>Pendientes de las respuestas guardadas:</Typography>
                <Box component="ul" sx={{ pl: 2.5 }}>
                    {review.pendientes.map(item => <li key={`${item.bloque}:${item.campo}`}>
                        <Button onClick={() => onNavigate(item)} sx={{ textAlign: 'left', justifyContent: 'flex-start', py: .5 }}>{item.mensaje}</Button>
                    </li>)}
                </Box>
            </> : <Typography sx={{ mb: 2 }}>El servidor confirmó que las respuestas guardadas cumplen los requisitos de llenado.</Typography>}
            {canSubmit && settled && review?.listo && <Button variant="contained" disabled={submitting} onClick={onSubmit}>Enviar formato</Button>}
            {!canSubmit && <Typography color="text.secondary" sx={{ mt: 2 }}>Sólo el responsable autorizado de este formato puede enviarlo.</Typography>}
        </>}
    </Box>;
}
