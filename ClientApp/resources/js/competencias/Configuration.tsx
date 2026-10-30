import { useState } from 'react';
import axios from 'axios';
import { Alert, Autocomplete, Button, TextField } from '@mui/material';
import { errorText } from '@/lib/http';
import { router } from '@/lib/navigation';
import ResetSurveys from './ResetSurveys';
import ReopenSurvey from './ReopenSurvey';
import { base } from './types';
import type { Config } from './types';

export default function Configuration({ data, readOnly }: { data: Config; readOnly: boolean }) {
    const [query, setQuery] = useState(''); const [people, setPeople] = useState<{ email: string; name: string }[]>([]);
    const [selected, setSelected] = useState<string>(''); const [reason, setReason] = useState('');
    const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [searched, setSearched] = useState(false); const [more, setMore] = useState(false);
    async function search() {
        setBusy(true); setError(''); setSelected(''); setPeople([]);
        try { const result = (await axios.get<{ people: typeof people; more: boolean }>(`${base}/api/representacion/personas`, { params: { q: query, page: 1 } })).data; setPeople(result.people); setMore(result.more); setSearched(true); }
        catch (e) { setError(errorText(e)); } finally { setBusy(false); }
    }
    function represent() { setBusy(true); router.post(`${base}/api/representacion`, { email: selected, motivo: reason }, { onError: e => setError(Object.values(e)[0]), onFinish: () => setBusy(false) }); }
    return <>
        {!readOnly && <nav className="cd-config-nav" aria-label="Opciones de Configuración">{data.sections?.map(s => <Button key={s.key} aria-current={data.section === s.key ? 'page' : undefined} variant={data.section === s.key ? 'contained' : 'outlined'} onClick={() => router.visit(s.path)}>{s.name}</Button>)}</nav>}
        {!data.section && <section className="cd-card"><h2>Configuración</h2><p>Selecciona una opción autorizada para administrar encuestas o consultar la vista de otra persona.</p>{!data.sections?.length && <Alert severity="info">No hay opciones de configuración disponibles para tu cuenta.</Alert>}</section>}
        {data.section === 'gestion_encuestas' && <><ReopenSurvey readOnly={readOnly} /><ResetSurveys readOnly={readOnly} /></>}
        {data.section === 'actuar_como_usuario' && <>
        {error && <Alert severity="error">{error}</Alert>}
        <section className="cd-card"><h2>Actuar como usuario</h2><p>Comprueba la navegación y los permisos efectivos mediante la representación autorizada por Nexo. La vista representada es de solo consulta.</p>
            <div className="cd-filters"><TextField label="Buscar persona por nombre o correo" value={query} disabled={readOnly || busy} onChange={e => setQuery(e.target.value)} /><Button disabled={readOnly || busy || query.trim().length < 2} variant="outlined" onClick={() => void search()}>Buscar</Button></div>
            {searched && people.length === 0 && <p>No se encontraron personas autorizadas para representar.</p>}
            {people.length > 0 && <><Autocomplete options={people} value={people.find(p => p.email === selected) || null} getOptionLabel={p => `${p.name} · ${p.email}`} disabled={readOnly || busy} onChange={(_, p) => setSelected(p?.email || '')} renderInput={p => <TextField {...p} label="Persona representada" />} />{more && <p>Hay más resultados. Escribe un nombre o correo más específico.</p>}<TextField label="Motivo de la consulta" value={reason} slotProps={{ htmlInput: { maxLength: 300 } }} multiline minRows={2} fullWidth disabled={readOnly || busy} onChange={e => setReason(e.target.value)} /><Button variant="contained" disabled={readOnly || busy || !selected || reason.trim().length < 5} onClick={represent}>Iniciar representación de consulta</Button></>}
        </section>
        </>}
        {!data.section && <><section className="cd-card"><h2>Población de participación</h2><p>Se obtiene automáticamente de las personas con acceso vigente a Competencias digitales. Incluye a quienes todavía no han ingresado y respeta su adscripción institucional actual. Ya no se capturan metas manuales.</p>
            {data.targets.length > 0 && <details><summary>Consultar metas manuales históricas</summary><p>Estos valores se conservan como referencia y no intervienen en la participación actual.</p><div className="cd-table-scroll" tabIndex={0} role="region" aria-label="Metas históricas"><table><thead><tr><th>Unidad responsable</th><th>Meta histórica</th><th>Actualización</th></tr></thead><tbody>{data.targets.map(t => <tr key={t.unitId}><th scope="row">{data.units.find(u => u.id === t.unitId)?.description || t.unitId}</th><td>{t.value}</td><td>{new Date(t.updatedAt).toLocaleString('es-MX')}</td></tr>)}</tbody></table></div></details>}
        </section>
        <section className="cd-card"><h2>Estado de los catálogos compartidos</h2><p>{data.catalog.source}</p><p>Disponibilidad: <strong>{data.catalog.available ? 'Catálogo publicado disponible' : 'Sin actualización correcta registrada'}</strong></p><p>Última actualización correcta: {data.catalog.completedAt ? new Date(data.catalog.completedAt).toLocaleString('es-MX') : 'Sin información'}</p><p>La actualización se administra desde el mecanismo existente de Transformación Digital.</p></section></>}
    </>;
}
