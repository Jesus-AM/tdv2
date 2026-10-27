import { Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel } from '@mui/material';
import { useState } from 'react';
import type { Notice } from './types';

/** La lectura nunca registra aceptación; solo la acción explícita de captura puede hacerlo. */
export default function PrivacyNotice({ notice, open, onClose, onAccept, busy = false }: {
    notice: Notice; open: boolean; onClose: () => void; onAccept?: () => void; busy?: boolean;
}) {
    const [confirmed, setConfirmed] = useState(false);
    const close = () => { if (!busy) { setConfirmed(false); onClose(); } };
    return <Dialog open={open} onClose={close} maxWidth="md" fullWidth aria-labelledby="cd-notice-title" scroll="paper">
        <DialogTitle id="cd-notice-title">Aviso de privacidad</DialogTitle>
        <DialogContent dividers>{notice.paragraphs.map((p, i) => <p key={i}>{p}</p>)}
            <p><a href={notice.institutionalUrl} target="_blank" rel="noopener noreferrer">Consultar el aviso institucional completo</a></p>
            {onAccept && <FormControlLabel control={<Checkbox checked={confirmed} disabled={busy} onChange={(_, checked) => setConfirmed(checked)} />} label={notice.confirmation} />}
        </DialogContent>
        <DialogActions>{onAccept && <Button href="/" disabled={busy}>Volver al portal</Button>}
            {onAccept ? <Button disabled={!confirmed || busy} variant="contained" onClick={onAccept}>Continuar</Button> : <Button onClick={close}>Cerrar</Button>}
        </DialogActions>
    </Dialog>;
}
