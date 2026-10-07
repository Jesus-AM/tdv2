import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Typography } from '@mui/material';
export default function RemoveRowDialog({ open, label, process, reason, changed, busy, error, onCancel, onConfirm }: {
    open: boolean; label: string; process: boolean; reason: string; changed: boolean; busy: boolean; error: string;
    onCancel: () => void; onConfirm: () => void;
}) {
    return <Dialog open={open} onClose={() => !busy && onCancel()} fullWidth maxWidth="sm" aria-labelledby="remove-row-title" aria-describedby="remove-row-description">
        <DialogTitle id="remove-row-title">Eliminar registro</DialogTitle>
        <DialogContent id="remove-row-description">
            <Typography sx={{ fontWeight: 600, overflowWrap: 'anywhere', mb: 2 }}>{label}</Typography>
            <Typography>{process
                ? 'Se eliminará este proceso y su evaluación. Los sistemas y datos asociados se conservarán, pero quedarán desvinculados del proceso.'
                : 'Se eliminará este registro del borrador compartido.'}</Typography>
            {changed && <Typography role="status" sx={{ mt: 2 }}>El registro o sus relaciones cambiaron. Cancela y revisa la versión vigente antes de volver a confirmar la eliminación.</Typography>}
            {reason && <Typography role="status" sx={{ mt: 2 }}>{reason}</Typography>}
            {error && !reason && <Typography role="status" color="error.main" sx={{ mt: 2 }}>{error}</Typography>}
        </DialogContent>
        <DialogActions>
            <Button autoFocus disabled={busy} onClick={onCancel}>Cancelar</Button>
            <Button variant="contained" color="error" loading={busy} disabled={!!reason || changed} onClick={onConfirm}>Eliminar</Button>
        </DialogActions>
    </Dialog>;
}
