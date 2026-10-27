import { useState } from 'react';
import axios from 'axios';
import { Alert, Autocomplete, Button, TextField } from '@mui/material';
import { errorText } from '@/lib/http';
import { router } from '@/lib/navigation';
import ResetSurveys from './ResetSurveys';
import { base, unitName } from './types';
import type { Config, Unit } from './types';

export default function Configuration({ data, readOnly }: { data: Config; readOnly: boolean }) {
    const [unit, setUnit] = useState<Unit | null>(null); const [target, setTarget] = useState('');
    const [query, setQuery] = useState(''); const [people, setPeople] = useState<{ email: string; name: string }[]>([]);
    const [selected, setSelected] = useState<string>(''); const [reason, setReason] = useState('');
    const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [searched, setSearched] = useState(false); const [more, setMore] = useState(false);
    const [message, setMessage] = useState('');
    async function saveTarget() {
        if (!unit || busy) return; setBusy(true); setError(''); setMessage('');
        try { await axios.put(`${base}/api/metas/${encodeURIComponent(unit.id)}`, { generation: data.generation, value: target === '' ? null : Number(target), version: data.targets.find(t => t.unitId === unit.id)?.version || 0 }); setMessage('Meta guardada.'); router.reload(); }
        catch (e) { setError(errorText(e)); } finally { setBusy(false); }
    }
    async function search() {
        setBusy(true); setError(''); setSelected(''); setPeople([]);
        try { const result = (await axios.get<{ people: typeof people; more: boolean }>(`${base}/api/representacion/personas`, { params: { q: query, page: 1 } })).data; setPeople(result.people); setMore(result.more); setSearched(true); }
        catch (e) { setError(errorText(e)); } finally { setBusy(false); }
    }
    function represent() { setBusy(true); router.post(`${base}/api/representacion`, { email: selected, motivo: reason }, { onError: e => setError(Object.values(e)[0]), onFinish: () => setBusy(false) }); }
    return <>
        <ResetSurveys readOnly={readOnly} />
        {error && <Alert severity="error">{error}</Alert>}{message && <Alert severity="success">{message}</Alert>}
        <section className="cd-card"><h2>Actuar como usuario</h2><p>Comprueba la navegación y los permisos efectivos mediante la representación autorizada por Nexo. La vista representada es de solo consulta.</p>
            <div className="cd-filters"><TextField label="Buscar persona por nombre o correo" value={query} disabled={readOnly || busy} onChange={e => setQuery(e.target.value)} /><Button disabled={readOnly || busy || query.trim().length < 2} variant="outlined" onClick={() => void search()}>Buscar</Button></div>
            {searched && people.length === 0 && <p>No se encontraron personas autorizadas para representar.</p>}
            {people.length > 0 && <><Autocomplete options={people} value={people.find(p => p.email === selected) || null} getOptionLabel={p => `${p.name} · ${p.email}`} disabled={readOnly || busy} onChange={(_, p) => setSelected(p?.email || '')} renderInput={p => <TextField {...p} label="Persona representada" />} />{more && <p>Hay más resultados. Escribe un nombre o correo más específico.</p>}<TextField label="Motivo de la consulta" value={reason} slotProps={{ htmlInput: { maxLength: 300 } }} multiline minRows={2} fullWidth disabled={readOnly || busy} onChange={e => setReason(e.target.value)} /><Button variant="contained" disabled={readOnly || busy || !selected || reason.trim().length < 5} onClick={represent}>Iniciar representación de consulta</Button></>}
        </section>
        <section className="cd-card"><h2>Metas de participación</h2><p>Cada meta corresponde al personal adscrito <strong>directamente</strong> a la UR, sin incluir sus dependientes. Los resúmenes suman las metas directas de las unidades de la rama. Una meta de cero es explícita; un campo vacío retira la meta.</p><p>Si falta una meta en alguna unidad de la rama, se muestra la cobertura parcial sin calcular porcentaje de participación.</p>
            <div className="cd-filters"><Autocomplete options={data.units} value={unit} getOptionLabel={unitName} isOptionEqualToValue={(a, b) => a.id === b.id} onChange={(_, u) => { setUnit(u); setTarget(u ? data.targets.find(t => t.unitId === u.id)?.value.toString() || '' : ''); }} renderInput={p => <TextField {...p} label="Unidad responsable de la meta" />} /><TextField type="number" label="Meta de adscripción directa" value={target} disabled={readOnly || busy || !unit} onChange={e => setTarget(e.target.value)} slotProps={{ htmlInput: { min: 0, max: 1000000, step: 1 } }} /><Button variant="contained" disabled={readOnly || busy || !unit || target !== '' && (!Number.isInteger(Number(target)) || Number(target) < 0)} onClick={() => void saveTarget()}>Guardar meta</Button></div>
            <div className="cd-table-scroll" tabIndex={0} role="region" aria-label="Metas vigentes"><table><thead><tr><th>Unidad responsable</th><th>Meta directa</th><th>Actualización</th></tr></thead><tbody>{data.targets.map(t => <tr key={t.unitId}><th scope="row">{data.units.find(u => u.id === t.unitId)?.description || t.unitId}</th><td>{t.value}</td><td>{new Date(t.updatedAt).toLocaleString('es-MX')}</td></tr>)}</tbody></table>{data.targets.length === 0 && <p>No se han configurado metas.</p>}</div>
        </section>
        <section className="cd-card"><h2>Estado de los catálogos compartidos</h2><p>{data.catalog.source}</p><p>Disponibilidad: <strong>{data.catalog.available ? 'Catálogo publicado disponible' : 'Sin actualización correcta registrada'}</strong></p><p>Última actualización correcta: {data.catalog.completedAt ? new Date(data.catalog.completedAt).toLocaleString('es-MX') : 'Sin información'}</p><p>La actualización se administra desde el mecanismo existente de Transformación Digital.</p></section>
    </>;
}
