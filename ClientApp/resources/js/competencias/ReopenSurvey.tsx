import { useRef, useState } from 'react';
import axios from 'axios';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField } from '@mui/material';
import { errorText } from '@/lib/http';
import { base } from './types';
import type { Unit } from './types';

interface Person { id: string; name: string; email: string; version: number; revision: number; completedAt: string | null; lastSubmittedAt: string; status: string; unit: Unit | null; }
interface Search { generation: string; people: Person[]; more: boolean; }
export default function ReopenSurvey({ readOnly }: { readOnly: boolean }) {
    const [query, setQuery] = useState(''), [data, setData] = useState<Search | null>(null), [selected, setSelected] = useState<Person | null>(null);
    const [busy, setBusy] = useState(false), [error, setError] = useState(''), [done, setDone] = useState(false), [changed, setChanged] = useState(false);
    const inFlight = useRef(false), operation = useRef('');
    async function search() {
        if (inFlight.current || readOnly) return; inFlight.current = true; setBusy(true); setError('');
        try { setData((await axios.get<Search>(`${base}/api/gestion-encuestas/personas`, { params: { q: query } })).data); }
        catch (e) { setError(errorText(e)); } finally { setBusy(false); inFlight.current = false; }
    }
    async function reopen() {
        if (inFlight.current || readOnly || !selected || !data || changed) return; inFlight.current = true; setBusy(true); setError('');
        try { await axios.post(`${base}/api/gestion-encuestas/reabrir`, { operation: operation.current, survey: selected.id, version: selected.version, generation: data.generation }); setDone(true); setData(null); }
        catch (e) { setError(errorText(e)); if (axios.isAxiosError(e) && [403, 409].includes(e.response?.status ?? 0)) setChanged(true); }
        finally { setBusy(false); inFlight.current = false; }
    }
    function close() { if (!busy) { setSelected(null); setError(''); setDone(false); setChanged(false); } }
    return <section className="cd-card"><h2>Reabrir una encuesta individual</h2><p>Permite corregir una encuesta finalizada conservando su captura y el envío anterior como histórico. No concede acceso ni reinicia otras encuestas.</p>
        <form className="cd-filters" onSubmit={e => { e.preventDefault(); void search(); }}><TextField label="Buscar encuesta por nombre o correo" value={query} onChange={e => setQuery(e.target.value)} disabled={readOnly || busy} /><Button type="submit" variant="outlined" disabled={readOnly || busy || query.trim().length < 2}>Buscar encuesta</Button></form>
        {!selected && error && <Alert severity="error">{error}</Alert>}
        {data?.people.length === 0 && <p>No hay encuestas enviadas que coincidan con la búsqueda.</p>}
        <div className="cd-survey-people">{data?.people.map(p => <article key={p.id}><h3>{p.name}</h3><p>{p.email}</p><p>{p.unit ? `${p.unit.code} - ${p.unit.description || p.unit.code}` : 'Unidad no disponible'}</p><p><strong>{p.status}</strong> · Último envío: {new Date(p.lastSubmittedAt).toLocaleString('es-MX')}</p><Button variant="outlined" disabled={readOnly || busy || !p.completedAt} onClick={() => { setSelected(p); operation.current = crypto.randomUUID(); setError(''); setDone(false); setChanged(false); }}>Reabrir encuesta</Button></article>)}</div>
        {data?.more && <p>Hay más coincidencias. Escribe un nombre o correo más específico.</p>}
        <Dialog open={!!selected} onClose={close} aria-labelledby="cd-reopen-title" fullWidth maxWidth="sm"><DialogTitle id="cd-reopen-title">{done ? 'Encuesta reabierta' : 'Confirmar reapertura individual'}</DialogTitle><DialogContent>
            {selected && <><p><strong>{selected.name}</strong><br />{selected.email}</p><p>Último envío: {new Date(selected.lastSubmittedAt).toLocaleString('es-MX')}</p></>}
            {done ? <Alert severity="success">La persona puede corregir sus respuestas y volver a enviar. Su informe anterior permanece en el historial.</Alert> : <p>Se conservarán todas las respuestas, el perfil, la aceptación de privacidad y el informe definitivo anterior. La encuesta pasará a En curso hasta que la persona vuelva a enviarla.</p>}
            {error && <Alert severity="error">{error}</Alert>}
        </DialogContent><DialogActions><Button disabled={busy} onClick={close}>{done ? 'Cerrar' : 'Cancelar'}</Button>{!done && <Button variant="contained" disabled={busy || changed} onClick={() => void reopen()}>{busy ? 'Reabriendo…' : 'Reabrir encuesta'}</Button>}</DialogActions></Dialog>
    </section>;
}
