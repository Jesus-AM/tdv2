import { useState } from 'react';
import axios from 'axios';
import { Alert, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, TextField } from '@mui/material';
import { errorText } from '@/lib/http';
import { router } from '@/lib/navigation';
import { base } from './types';

interface ResetSummary {
    operation: string; scope: string; includeTargets: boolean; expiresAt?: string; completedAt: string | null;
    counts: { submissions?: number; drafts: number; completed: number; profiles: number; answers: number; reports: number; consents: number; targets: number };
}
export default function ResetSurveys({ readOnly }: { readOnly: boolean }) {
    const [targets, setTargets] = useState(false), [summary, setSummary] = useState<ResetSummary | null>(null);
    const [phrase, setPhrase] = useState(''), [busy, setBusy] = useState(false), [error, setError] = useState('');
    const [review, setReview] = useState(false);
    async function prepare() {
        if (busy || readOnly) return; setBusy(true); setError(''); setPhrase('');
        try { setSummary((await axios.post<ResetSummary>(base + '/api/reinicio/resumen', { includeTargets: targets })).data); setReview(false); }
        catch (e) { setError(errorText(e)); } finally { setBusy(false); }
    }
    async function execute() {
        if (!summary || busy || readOnly || review) return; setBusy(true); setError(''); window.dispatchEvent(new Event('cd-reset-operation'));
        try {
            const result = (await axios.post<ResetSummary>(base + '/api/reinicio', { operation: summary.operation, confirmation: phrase })).data;
            setSummary(result); window.dispatchEvent(new Event('cd-reset-operation'));
            if (typeof BroadcastChannel !== 'undefined') { const channel = new BroadcastChannel('tdv2-competencias-generation'); channel.postMessage('reset'); channel.close(); }
        } catch (e) {
            setError(errorText(e));
            if (axios.isAxiosError(e) && e.response?.status === 409) { setReview(true); setPhrase(''); }
            // Ante error de red se conserva la operación: reintentar nunca vuelve a borrar.
        } finally { setBusy(false); }
    }
    function close() {
        if (busy) return;
        setBusy(true);
        router.reload({ onFinish: () => { setSummary(null); setError(''); setPhrase(''); setBusy(false); window.dispatchEvent(new Event('cd-reset-release')); } });
    }
    return <section className="cd-card cd-reset"><h2>Reiniciar encuestas</h2><p>Reinicio global de Competencias digitales, para todas las unidades. Elimina capturas, respuestas, informes y confirmaciones del aviso. Los participantes podrán comenzar nuevamente desde Perfil.</p>
        <FormControlLabel control={<Checkbox checked={targets} disabled={readOnly || busy || !!summary} onChange={e => setTargets(e.target.checked)} />} label="Eliminar también las metas de participación" />
        <p>Se conservan identidades, Nexo, catálogos, conexiones, auditoría, historial de migraciones y todos los datos de Procedimientos institucionales.</p>
        {!summary && error && <Alert severity="error">{error}</Alert>}
        <Button color="error" variant="outlined" disabled={readOnly || busy} onClick={() => void prepare()}>Revisar reinicio global</Button>
        <Dialog open={!!summary} onClose={() => !busy && close()} aria-labelledby="cd-reset-title" fullWidth maxWidth="sm">
            <DialogTitle id="cd-reset-title">{summary?.completedAt ? 'Reinicio confirmado' : 'Confirmar reinicio global'}</DialogTitle>
            <DialogContent>{summary && <>
                <p><strong>{summary.scope}</strong></p>
                <dl className="cd-reset-counts">{Object.entries({ 'Encuestas en captura': summary.counts.drafts, 'Encuestas finalizadas': summary.counts.completed, 'Perfiles capturados': summary.counts.profiles, 'Respuestas por herramienta o tema': summary.counts.answers, 'Informes definitivos': summary.counts.reports, 'Envíos históricos': summary.counts.submissions ?? 0, 'Confirmaciones del aviso': summary.counts.consents, [summary.includeTargets ? 'Metas que se eliminarán' : 'Metas que se conservarán']: summary.counts.targets }).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>
                <p>Los perfiles, respuestas e informes están incluidos en las encuestas; los conteos no deben sumarse como registros independientes.</p>
                <Alert severity={summary.completedAt ? 'success' : 'warning'}>{summary.completedAt ? `Operación ${summary.operation} completada el ${new Date(summary.completedAt).toLocaleString('es-MX')}.` : 'Se perderán todas las respuestas e informes. No hay deshacer desde la aplicación; la recuperación requiere un respaldo institucional previo.'}</Alert>
                {!summary.completedAt && <><p>Resumen válido hasta {new Date(summary.expiresAt!).toLocaleTimeString('es-MX')}. Cualquier cambio en los datos exige revisarlo nuevamente.</p><TextField autoFocus fullWidth margin="normal" label="Escribe REINICIAR ENCUESTAS" value={phrase} disabled={busy || review} onChange={e => setPhrase(e.target.value)} autoComplete="off" /></>}
            </>}{error && <Alert severity="error">{error}</Alert>}</DialogContent>
            <DialogActions><Button disabled={busy} onClick={close}>{summary?.completedAt ? 'Cerrar y actualizar' : 'Cancelar'}</Button>{!summary?.completedAt && (review ? <Button onClick={() => void prepare()} disabled={busy}>Revisar nuevo resumen</Button> : <Button variant="contained" color="error" disabled={busy || phrase !== 'REINICIAR ENCUESTAS'} onClick={() => void execute()}>{busy ? 'Reiniciando…' : 'Eliminar encuestas de toda la aplicación'}</Button>)}</DialogActions>
        </Dialog>
    </section>;
}
