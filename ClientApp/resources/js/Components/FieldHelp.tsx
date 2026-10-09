import { useId, useState, type ReactNode } from 'react';
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Tooltip, Typography } from '@mui/material';
import { CloseOutlined, HelpOutlineOutlined } from '@mui/icons-material';

/** Ayuda de consulta: nunca enfoca campos de captura ni pide reservas. MUI restaura el foco al cerrar. */
export default function FieldHelp({ title, explanation, example, advice }: {
    title: string; explanation: ReactNode; example?: string; advice?: string;
}) {
    const [open, setOpen] = useState(false), id = useId();
    return <>
        <Tooltip title={`Ayuda: ${title}`}>
            <IconButton size="small" aria-label={`Ayuda: ${title}`} aria-haspopup="dialog" data-field-help
                onClick={event => { event.stopPropagation(); setOpen(true); }} sx={{ ml: .3, color: 'text.primary' }}>
                <HelpOutlineOutlined sx={{ fontSize: 19 }} />
            </IconButton>
        </Tooltip>
        <Dialog open={open} onClose={() => setOpen(false)} aria-labelledby={id} maxWidth="sm" fullWidth
            data-field-help-dialog onClick={event => event.stopPropagation()}>
            <DialogTitle id={id} sx={{ pr: 7 }}>{title}</DialogTitle>
            <IconButton aria-label="Cerrar ayuda" onClick={() => setOpen(false)} sx={{ position: 'absolute', right: 8, top: 8 }}>
                <CloseOutlined />
            </IconButton>
            <DialogContent sx={{ fontSize: 14, '& p': { fontSize: 14, lineHeight: 1.65 } }}>
                {typeof explanation === 'string' ? <Typography>{explanation}</Typography> : explanation}
                {example && <Box sx={{ mt: 2 }}><Typography sx={{ fontWeight: 600 }}>Ejemplo</Typography><Typography>{example}</Typography></Box>}
                {advice && <Box sx={{ mt: 2, pt: 1.5, borderTop: '1px solid', borderColor: 'divider' }}>
                    <Typography sx={{ fontWeight: 600 }}>Consejo</Typography><Typography color="text.secondary">{advice}</Typography>
                </Box>}
            </DialogContent>
            <DialogActions><Button autoFocus onClick={() => setOpen(false)}>Cerrar</Button></DialogActions>
        </Dialog>
    </>;
}
