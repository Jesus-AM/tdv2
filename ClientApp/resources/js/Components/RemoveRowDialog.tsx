import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Typography } from '@mui/material';
export default function RemoveRowDialog({ open, label, process, reason, changed, onCancel, onConfirm }: {
    open: boolean; label: string; process: boolean; reason: string; changed: boolean;
    onCancel: () => void; onConfirm: () => void;
}) {
    return <Dialog open={open} onClose={onCancel} fullWidth maxWidth="sm" aria-labelledby="remove-row-title" aria-describedby="remove-row-description">
        <DialogTitle id="remove-row-title">Eliminar registro</DialogTitle>
        <DialogContent id="remove-row-description">
            <Typography sx={{ fontWeight: 600, overflowWrap: 'anywhere', mb: 2 }}>{label}</Typography>
            <Typography>{process
                ? 'Se eliminará este proceso y su evaluación. Los sistemas y datos asociados se conservarán, pero quedarán desvinculados del proceso.'
                : 'Se eliminará este registro del borrador compartido.'}</Typography>
            {changed && <Typography sx={{ mt: 2 }}>El registro cambió mientras el diálogo estaba abierto. Revisa su contenido actualizado antes de eliminarlo.</Typography>}
            {reason && <Typography role="status" sx={{ mt: 2 }}>{reason}</Typography>}
        </DialogContent>
        <DialogActions>
            <Button autoFocus onClick={onCancel}>Cancelar</Button>
            <Button variant="contained" color="error" disabled={!!reason} onClick={onConfirm}>Eliminar</Button>
        </DialogActions>
    </Dialog>;
}
