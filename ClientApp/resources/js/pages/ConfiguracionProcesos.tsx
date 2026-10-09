import { useEffect, useState } from 'react';
import axios from 'axios';
import { Alert, Box, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle, FormControl, FormHelperText, InputLabel, ListItemText, MenuItem, Select, Typography } from '@mui/material';
import { Tune } from '@mui/icons-material';
import { Head } from '@/lib/navigation';
import { errorText } from '@/lib/http';
import { displayUnitCode } from '@/lib/area-directory';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import SettingsSection from '@/Components/SettingsSection';
import type { Unit } from '@/types/tdv2';

type Settings = { version: number; levels: number[]; excludedTypes: string[] };
type Impact = { confirmation: string; entering: Unit[]; leaving: Unit[];
    forms: { id: string; version: number; enviadoEn: string | null }[];
    scopeChanges: { unidad: Unit; localAntes: string | null; localDespues: string | null; dependenciasAntes: string[]; dependenciasDespues: string[] }[] };
export default function ConfiguracionProcesos({ settings, levels, types }: { settings: Settings; levels: number[]; types: string[] }) {
    const [saved, setSaved] = useState(settings), [draft, setDraft] = useState(settings);
    const [impact, setImpact] = useState<Impact | null>(null), [busy, setBusy] = useState(false), [error, setError] = useState(''), [message, setMessage] = useState('');
    const dirty = JSON.stringify(saved) !== JSON.stringify(draft);
    useEffect(() => {
        const prevent = (event: BeforeUnloadEvent) => { if (dirty) { event.preventDefault(); event.returnValue = ''; } };
        window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent);
    }, [dirty]);
    async function preview() {
        setBusy(true); setError(''); setMessage('');
        try { setImpact((await axios.post<Impact>('/configuracion/procesos/vista-previa', draft)).data); }
        catch (e) { setError(errorText(e)); } finally { setBusy(false); }
    }
    async function save() {
        if (!impact || busy) return;
        setBusy(true); setError('');
        try {
            const response = await axios.put<{ version: number }>('/configuracion/procesos', { ...draft, confirmation: impact.confirmation });
            const next = { ...draft, version: response.data.version }; setSaved(next); setDraft(next); setImpact(null); setMessage('Participación guardada.');
        } catch (e) { setError(errorText(e)); setImpact(null); } finally { setBusy(false); }
    }
    const label = (u: Unit) => `${displayUnitCode(u.cve_ur)} · ${u.desc_ur}`;
    const unitName = (id: string | null) => id === null ? 'Sin formato' : [...(impact?.entering || []), ...(impact?.leaving || []), ...(impact?.scopeChanges.map(c => c.unidad) || [])].find(u => u.id_ur === id)?.desc_ur || id;
    return <AuthenticatedLayout><Head title="Configuración procesos" /><div className="page settings-page">
        <PageHeading title="Configuración procesos" description="Define las áreas que participan en Procedimientos Institucionales."
            breadcrumbs={[{ label: 'Configuración', href: '/configuracion' }]} />
        <Box sx={{ maxWidth: 960 }}><SettingsSection title="Participación de áreas" icon={<Tune />}
            description="Participar determina qué áreas tienen formato; no concede roles ni permisos. Los formatos excluidos y sus respuestas se conservan.">
            <Box className="settings-field-grid">
                <FormControl size="small"><InputLabel id="participation-levels">Niveles participantes</InputLabel>
                    <Select multiple labelId="participation-levels" label="Niveles participantes" value={draft.levels} disabled={busy}
                        renderValue={values => values.map(n => `Nivel ${n}`).join(', ')}
                        onChange={e => { setDraft({ ...draft, levels: (e.target.value as number[]).slice().sort((a, b) => a - b) }); setMessage(''); }}>
                        {levels.map(n => <MenuItem key={n} value={n}><Checkbox checked={draft.levels.includes(n)} /><ListItemText primary={`Nivel ${n}`} /></MenuItem>)}
                    </Select></FormControl>
                <FormControl size="small"><InputLabel id="participation-types">Tipos excluidos</InputLabel>
                    <Select multiple labelId="participation-types" label="Tipos excluidos" aria-describedby="participation-types-help" value={draft.excludedTypes} disabled={busy}
                        renderValue={values => values.map(t => t || 'Sin tipo').join(', ')}
                        onChange={e => { setDraft({ ...draft, excludedTypes: (e.target.value as string[]).slice().sort() }); setMessage(''); }}>
                        {types.map(t => <MenuItem key={t} value={t}><Checkbox checked={draft.excludedTypes.includes(t)} /><ListItemText primary={t || 'Sin tipo (nulo o vacío)'} /></MenuItem>)}
                    </Select><FormHelperText id="participation-types-help">Participan todos los tipos excepto los seleccionados.</FormHelperText></FormControl>
            </Box>
            <Box className="settings-section-actions">
                <Button variant="contained" disabled={!dirty} loading={busy} onClick={() => void preview()}>Revisar cambios</Button>
                <Typography variant="body2" role="status" color="text.secondary">{busy ? 'Preparando…' : dirty ? 'Cambios pendientes' : message}</Typography>
            </Box>
            {error && <Alert severity="error" sx={{ mt: 2 }} onClose={() => setError('')}>{error}</Alert>}
        </SettingsSection></Box>
        <Dialog open={!!impact} onClose={() => { if (!busy) setImpact(null); }} fullWidth maxWidth="md">
            <DialogTitle>Confirmar participación de áreas</DialogTitle>
            <DialogContent dividers>
                <Typography variant="body2" sx={{ mb: 2 }}>No se eliminarán datos. Las áreas excluidas dejarán de admitir nuevas escrituras. Los enviados siguen bloqueados, incluso al reactivarlos.</Typography>
                {([['Entrarán', impact?.entering || []], ['Saldrán', impact?.leaving || []]] as const).map(([title, units]) => <Box key={title} sx={{ mb: 2 }}>
                    <Typography sx={{ fontWeight: 600 }}>{title}: {units.length}</Typography>
                    {units.map(u => <Typography variant="body2" key={u.id_ur}>{label(u)}</Typography>)}
                </Box>)}
                <Typography sx={{ fontWeight: 600 }}>Formatos existentes afectados: {impact?.forms.length || 0}</Typography>
                {impact?.forms.map(f => <Typography variant="body2" key={f.id}>{unitName(f.id)} — {f.enviadoEn ? 'Enviado' : 'Iniciado'}</Typography>)}
                <Typography sx={{ fontWeight: 600, mt: 2 }}>Alcances centrales por adscripción</Typography>
                <Typography variant="body2" color="text.secondary">Sólo se aplican a personas con una asignación central vigente y explícita en Nexo. Las colaboraciones delegadas conservan su vínculo original.</Typography>
                {impact?.scopeChanges.map(s => <Box key={s.unidad.id_ur} sx={{ mt: 1 }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>{label(s.unidad)}</Typography>
                    {s.localAntes !== s.localDespues && <Typography variant="body2">Local: {unitName(s.localAntes)} → {unitName(s.localDespues)}</Typography>}
                    <Typography variant="body2">Dependencias: {s.dependenciasAntes.length} → {s.dependenciasDespues.length} formatos</Typography>
                    {s.dependenciasDespues.filter(id => !s.dependenciasAntes.includes(id)).map(id => <Typography variant="body2" key={`add-${id}`}>Se incorpora: {unitName(id)}</Typography>)}
                    {s.dependenciasAntes.filter(id => !s.dependenciasDespues.includes(id)).map(id => <Typography variant="body2" key={`remove-${id}`}>Sale del alcance: {unitName(id)}</Typography>)}
                </Box>)}
            </DialogContent>
            <DialogActions><Button autoFocus disabled={busy} onClick={() => setImpact(null)}>Cancelar</Button><Button variant="contained" loading={busy} onClick={() => void save()}>Confirmar y guardar</Button></DialogActions>
        </Dialog>
    </div></AuthenticatedLayout>;
}
