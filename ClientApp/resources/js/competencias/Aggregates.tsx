import { useMemo, useState } from 'react';
import { Alert, Autocomplete, Button, MenuItem, TextField } from '@mui/material';
import { router } from '@/lib/navigation';
import { base, levels, percent } from './types';
import type { Aggregates, Distribution, Recommendation, Summary, Unit, UnitSummary } from './types';

const unitName = (unit: Pick<Unit, 'code' | 'description'>) => unit.description ? `${unit.code} - ${unit.description}` : unit.code;

export function Recommendations({ values }: { values: Recommendation[] }) {
    return <div className="cd-recommendations">{values.map((r, i) => <Alert key={i} severity={r.kind}><strong>{r.title}</strong><p>{r.text}</p></Alert>)}</div>;
}
export function Kpis({ value, results = false }: { value: Summary; results?: boolean }) {
    const items = results ? [['Índice global', percent(value.maturity)], ['Encuestas finalizadas', value.completed], ['Unidades participantes', value.participatingUnits]]
        : [['Encuestas finalizadas', value.completed], ['Encuestas en curso', value.inProgress], ['Avance frente a la meta', value.target === null ? 'Sin meta completa' : value.target === 0 ? 'Meta 0' : percent(value.participation)], ['Unidades participantes', value.participatingUnits]];
    return <div className="cd-kpis">{items.map(([label, text]) => <section className="cd-card" key={label}><span>{label}</span><strong>{text}</strong></section>)}</div>;
}
export function DistributionTable({ rows, title }: { rows: Distribution[]; title: string }) {
    return <section className="cd-card"><h2>{title}</h2><div className="cd-table-scroll" tabIndex={0} role="region" aria-label={title}><table><thead><tr><th scope="col">{title}</th><th scope="col">Índice</th><th scope="col">Evaluadas</th>{levels.map(l => <th scope="col" key={l}>{l}</th>)}<th scope="col">No aplica</th><th scope="col">No utiliza</th></tr></thead><tbody>
        {rows.map(r => <tr key={r.id}><th scope="row">{r.name}</th><td><Score value={r.score} /></td><td>{r.count}</td>{r.levels.map((n, i) => <td key={i}>{n}</td>)}<td>{r.notApplicable}</td><td>{r.notUsed}</td></tr>)}
    </tbody></table></div></section>;
}
export function Score({ value }: { value: number | null }) { return <div className="cd-score"><span>{percent(value)}</span>{value !== null && <span className="cd-bar" aria-hidden="true"><i style={{ width: `${Math.min(value, 100)}%` }} /></span>}</div>; }
export default function AggregateView({ initial, results }: { initial: Aggregates; results: boolean }) {
    const data = initial;
    const query = new URLSearchParams(window.location.search);
    const rootQuery = query.get('root');
    const defaultRoot = rootQuery && data.options.some(u => u.id === rootQuery) ? rootQuery : data.selectedUnit;
    const [root, setRoot] = useState(defaultRoot || (!data.institutional && data.roots.length === 1 ? data.roots[0].id : ''));
    const [area, setArea] = useState(rootQuery ? data.selectedUnit || '' : '');
    const [level, setLevel] = useState(data.selectedLevel?.toString() || '');
    const [search, setSearch] = useState('');
    const [position, setPosition] = useState(data.selectedPosition || '');
    const [expanded, setExpanded] = useState<Set<string>>(() => new Set(data.units.map(u => u.id)));
    const within = (id: string, ancestor: string) => { const seen = new Set<string>(); let current = data.options.find(u => u.id === id); while (current && !seen.has(current.id)) { if (current.id === ancestor) return true; seen.add(current.id); current = data.options.find(u => u.id === current!.parent); } return false; };
    const roots = data.institutional ? data.options.filter(u => !level || u.level === Number(level)) : data.roots;
    const descendants = root ? data.options.filter(u => u.id !== root && within(u.id, root)) : [];
    const apply = () => { const params = new URLSearchParams(); if (area || root) params.set('unit', area || root); if (root && area) params.set('root', root); if (level) params.set('level', level); if (position) params.set('position', position); router.visit(`${base}/${results ? 'resultados-generales' : 'avance-dependencia'}?${params}`); };
    const exportQuery = new URLSearchParams(); if (data.selectedUnit) exportQuery.set('unit', data.selectedUnit); if (data.selectedLevel !== null) exportQuery.set('level', String(data.selectedLevel)); if (data.selectedPosition) exportQuery.set('position', data.selectedPosition);
    const tree = useMemo(() => {
        const list: { row: UnitSummary; depth: number }[] = []; const seen = new Set<string>();
        const visit = (row: UnitSummary, depth: number) => { if (seen.has(row.id)) return; seen.add(row.id); list.push({ row, depth }); if (expanded.has(row.id) || search) data.units.filter(c => c.parent === row.id).forEach(c => visit(c, depth + 1)); };
        data.units.filter(r => !data.units.some(p => p.id === r.parent)).forEach(r => visit(r, 0));
        return list.filter(({ row }) => !search || `${row.code} ${row.name}`.toLocaleLowerCase().includes(search.toLocaleLowerCase()));
    }, [data, expanded, search]);
    const parentName = (id: string | null, fallback = 'Área no disponible') => { const parent = data.options.find(u => u.id === id); return parent ? unitName(parent) : fallback; };
    const context = data.selectedUnit ? data.options.find(u => u.id === data.selectedUnit) : null;
    return <>
        <section className="cd-card"><span className="cd-eyebrow">{data.institutional ? 'Vista institucional' : 'Ramas autorizadas por responsabilidad vigente'}</span><h2>{context ? unitName(context) : data.institutional ? 'Todas las unidades autorizadas' : data.roots.map(unitName).join(' · ')}</h2>
            {!data.institutional && data.roots.length === 1 && <p>Contexto principal: <strong>{unitName(data.roots[0])}</strong></p>}
            <div className="cd-filters">
                {data.institutional && <TextField select label="Nivel de UR" value={level} onChange={e => { setLevel(e.target.value); setRoot(''); setArea(''); }}><MenuItem value="">Todos los niveles</MenuItem>{data.levels.map(l => <MenuItem value={l} key={l}>Nivel de UR {l}</MenuItem>)}</TextField>}
                {(data.institutional || data.roots.length > 1) && <Autocomplete options={roots} value={roots.find(u => u.id === root) || null} getOptionLabel={unitName} isOptionEqualToValue={(a, b) => a.id === b.id} onChange={(_, u) => { setRoot(u?.id || ''); setArea(''); }} renderInput={p => <TextField {...p} label={data.institutional ? 'Unidad responsable' : 'Contexto de responsabilidad'} placeholder="Buscar por clave o nombre" />} />}
                {descendants.length > 0 && <Autocomplete options={descendants} value={descendants.find(u => u.id === area) || null} getOptionLabel={unitName} isOptionEqualToValue={(a, b) => a.id === b.id} onChange={(_, u) => { setArea(u?.id || ''); if (u) setLevel(''); }} renderOption={(props, u) => <li {...props} key={u.id}><span>{unitName(u)}<small className="cd-parent">Depende de {parentName(u.parent)}</small></span></li>} renderInput={p => <TextField {...p} label="Unidad dependiente" placeholder="Toda la rama" />} />}
                <TextField select label="Perfil laboral" value={position} onChange={e => setPosition(e.target.value)}><MenuItem value="">Todos los perfiles</MenuItem>{data.positionOptions.map(p => <MenuItem value={p} key={p}>{p}</MenuItem>)}</TextField><Button variant="contained" onClick={apply}>Aplicar filtros</Button>
            </div>
            {data.selectedPosition && <p>Perfil laboral: <strong>{data.selectedPosition}</strong>. La meta de UR abarca todos los puestos; no se calcula porcentaje frente a esa meta al filtrar por puesto.</p>}<p>Los resúmenes incluyen al personal adscrito a las unidades dependientes. {data.selectedLevel !== null ? `Agrupación: UR de nivel ${data.selectedLevel}.` : 'Agrupación: jerarquía de unidades.'}</p>
        </section>
        <Kpis value={data.summary} results={results} />
        {!results && <section className="cd-card"><h2>Participación y madurez</h2><p>Meta de adscripción directa: {data.summary.target === null ? `Sin meta completa · ${data.summary.configuredTargets} de ${data.summary.totalUnits} unidades con meta.` : `${data.summary.target} personas esperadas.`} Las metas son objetivos de participación; no representan un padrón de empleados.</p><p>Índice de madurez de encuestas finalizadas: <strong>{percent(data.summary.maturity)}</strong></p></section>}
        {data.summary.completed === 0 && <Alert severity="info">No hay encuestas finalizadas en el conjunto consultado. Los resultados se mostrarán cuando existan respuestas definitivas.</Alert>}
        {results && <><section className="cd-card"><h2>Distribución por nivel de competencia digital</h2><div className="cd-kpis">{levels.map((l, i) => <div key={l}><strong>{data.competenceLevels[i]}</strong><span>{l}</span></div>)}</div><Button href={`${base}/api/resultados/descarga?${exportQuery}`} variant="outlined">Descargar resultados agregados</Button></section><DistributionTable rows={data.blocks} title="Resultados por bloque" /></>}
        <section className="cd-card"><div className="cd-section-title"><h2>{results ? 'Comparación entre unidades' : 'Detalle por unidad'}</h2><TextField size="small" label="Buscar área por clave o nombre" value={search} onChange={e => setSearch(e.target.value)} /></div><p>Cada fila resume su rama. Las filas de padres e hijos se superponen y no deben sumarse. El total superior cuenta a cada participante una sola vez.</p>
            <div className="cd-table-scroll" tabIndex={0} role="region" aria-label="Detalle jerárquico de unidades"><table><thead><tr><th scope="col">Unidad responsable</th><th scope="col">Finalizadas<br />Rama / directas</th><th scope="col">En curso<br />Rama / directas</th><th scope="col">Meta de la rama</th><th scope="col">Participación</th><th scope="col">Madurez</th></tr></thead><tbody>{tree.map(({ row: r, depth }) => <tr key={r.id}><th scope="row" style={{ paddingLeft: 12 + depth * 20 }}>
                {data.units.some(c => c.parent === r.id) && <button className="cd-expand" aria-expanded={expanded.has(r.id)} aria-label={`${expanded.has(r.id) ? 'Contraer' : 'Expandir'} ${r.name}`} onClick={() => setExpanded(prev => { const next = new Set(prev); if (next.has(r.id)) next.delete(r.id); else next.add(r.id); return next; })}>{expanded.has(r.id) ? '−' : '+'}</button>}{unitName({ code: r.code, description: r.name })}{r.parentName && <small className="cd-parent">Depende de {parentName(r.parent, r.parentName)}</small>}</th><td>{r.summary.completed} / {r.directCompleted}</td><td>{r.summary.inProgress} / {r.directInProgress}</td><td>{r.summary.target ?? 'Sin meta'}</td><td>{r.summary.participation === null ? 'Sin porcentaje' : percent(r.summary.participation)}</td><td><Score value={r.summary.maturity} /></td></tr>)}</tbody></table>{tree.length === 0 && <p>No hay unidades que coincidan con la búsqueda.</p>}</div>
        </section>
        {results && <><DistributionTable rows={data.positions} title="Resultados por perfil laboral" /><DistributionTable rows={data.tools} title="Resultados por herramienta o tema" /><section className="cd-card"><h2>Recomendaciones del conjunto consultado</h2><Recommendations values={data.recommendations} /></section></>}
    </>;
}
