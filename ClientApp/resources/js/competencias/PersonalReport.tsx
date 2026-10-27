import { Alert } from '@mui/material';
import { levels, percent, unitName } from './types';
import type { Report, Survey } from './types';
import { Recommendations } from './Aggregates';

function Level({ value }: { value: number | null }) {
    return <span className={`cd-level cd-level-${value ?? 'na'}`}>{value === null ? 'Sin evaluación aplicable' : levels[value]}</span>;
}
function Gap({ count }: { count: number }) { return <span className="cd-gap"><span aria-hidden="true">⚠</span> {count} {count === 1 ? 'brecha' : 'brechas'}</span>; }
function Course({ course: c }: { course: Report['route'][number] }) {
    return <article className="cd-course"><span className="cd-eyebrow">{c.block.toUpperCase()}</span><h4>{c.name}</h4><p><Level value={c.from} /> → <Level value={c.to} /></p><p>{c.why}</p><ul>{c.courses.map(s => <li key={s}>{s}</li>)}</ul>{c.missing.length > 0 && <p>Por desarrollar: {c.missing.join(' · ')}</p>}{c.license && <small>{c.license}</small>}</article>;
}
export default function PersonalReport({ survey }: { survey: Survey }) {
    // El servidor entrega el informe definitivo persistido y el instrumento de ese intento.
    const r = survey.evaluation.report, institution = survey.institution;
    const required = r.tools.filter(t => t.expected > 0), covered = required.filter(t => t.value !== null && t.value >= t.expected).length;
    const label = (unit: typeof institution.unit) => unit ? unitName(unit) : institution.status === 'available' ? 'No aplica' : 'Información institucional pendiente';
    return <div className="cd-report">
        {!survey.completedAt ? <Alert severity="info">Informe preliminar. La encuesta aún no está finalizada; los resultados pueden cambiar con tus respuestas.</Alert>
            : <Alert severity="success">Informe definitivo · Finalizada el {new Date(survey.completedAt).toLocaleString('es-MX')}. Las respuestas ya no se pueden modificar.</Alert>}
        <section className="cd-card cd-report-hero"><div><span className="cd-eyebrow">Informe personal de madurez digital</span><h2>{institution.name}</h2><p>{survey.profile.puesto || 'Perfil pendiente'} · {label(institution.level2)}</p><p>{label(institution.level3)}</p><p className="cd-muted">Instrumento: {survey.instrument.version}</p></div><div className="cd-global"><span className="cd-eyebrow">Índice global</span><strong>{percent(r.global)}</strong><Level value={r.level} /><p>{covered} de {required.length} requisitos del puesto cubiertos</p><small>No aplica, pendientes y no utilizadas no acreditan competencia.</small></div></section>
        <div className="cd-report-blocks">{r.blocks.map(b => {
            const tools = r.tools.filter(t => t.block === b.id), values = levels.map((_, i) => tools.filter(t => t.value === i).length), total = values.reduce((a, v) => a + v, 0), gaps = tools.filter(t => t.gap > 0).length;
            return <section className="cd-card" key={b.id}><h3>{b.name}</h3><strong className="cd-block-score">{percent(b.score)}</strong><div className="cd-report-tags"><Level value={b.level} />{gaps ? <Gap count={gaps} /> : <span className={total ? 'cd-met' : 'cd-muted'}>{total ? '✓ Sin brechas evaluadas' : 'Sin evaluación aplicable'}</span>}</div>
                <div className="cd-distribution" aria-label={`Distribución de ${b.name}: ${values.map((n, i) => `${levels[i]} ${n}`).join(', ')}`} role="img">{values.map((n, i) => n > 0 && <span key={i} className={`cd-level-${i}`} style={{ width: `${n / total * 100}%` }} aria-hidden="true">{n}</span>)}</div><p className="cd-muted">{total} evaluadas · {tools.filter(t => t.value === -1).length} No aplica · {tools.filter(t => t.value === -2).length} No utiliza · {tools.filter(t => t.value === null).length} pendientes</p>
            </section>;
        })}</div>
        <div className="cd-report-legend" aria-label="Leyenda de niveles">{levels.map((name, i) => <span key={name}><i className={`cd-level-${i}`} aria-hidden="true" />{name}</span>)}</div>
        <section className="cd-report-section"><h2>Recomendaciones personales</h2><Recommendations values={r.recommendations} /></section>
        <section className="cd-report-section"><h2>Tu ruta de capacitación</h2><p>Cursos ordenados por prioridad, desde tu nivel hasta el siguiente objetivo. La disponibilidad y las licencias deben confirmarse con la institución.</p>
            {survey.instrument.trainingPhases.map((phase, i) => {
                const courses = r.route.filter(c => c.phase === i + 1), limit = i === 2 ? 4 : 6;
                return <section className="cd-phase" key={phase.id}><div className="cd-section-title"><h3>{phase.title}</h3><span>{courses.length} propuestas</span></div><p>{phase.description}</p><div className="cd-course-grid">{courses.slice(0, limit).map(c => <Course key={c.tool} course={c} />)}</div>{courses.length > limit && <details><summary>Ver las {courses.length - limit} propuestas restantes de esta fase</summary><div className="cd-course-grid">{courses.slice(limit).map(c => <Course key={c.tool} course={c} />)}</div></details>}{!courses.length && <p className="cd-muted">Sin cursos sugeridos para esta fase. Esto no acredita por sí solo un nivel Avanzado.</p>}</section>;
            })}
            {r.transversal.length > 0 && <section className="cd-card"><h3>Cursos transversales</h3><ul>{r.transversal.map(c => <li key={c}>{c}</li>)}</ul></section>}
        </section>
        <section className="cd-report-section"><h2>Resultado por herramienta</h2><p>Tu nivel frente al esperado por las funciones de tu puesto.</p><div className="cd-report-details">{r.blocks.map(b => {
            const tools = r.tools.filter(t => t.block === b.id), omitted = tools.filter(t => t.value === -2 && !t.expected);
            return <section className="cd-report-table" key={b.id}><h3>{b.name}</h3><div className="cd-report-overflow" tabIndex={0} role="region" aria-label={`Resultado por herramienta · ${b.name}`}><table><thead><tr><th scope="col">Herramienta o tema</th><th scope="col">Tu nivel</th><th scope="col">Esperado</th><th scope="col">Estado</th></tr></thead><tbody>{tools.filter(t => !omitted.includes(t)).map(t => <tr key={t.id}><th scope="row">{t.name}</th><td>{t.value === null ? 'Pendiente' : t.value === -1 ? 'No aplica' : t.value === -2 ? 'No utiliza' : <Level value={t.value} />}</td><td>{t.expected ? levels[t.expected] : 'Sin requisito'}</td><td>{t.gap > 0 ? <span className="cd-gap"><span aria-hidden="true">⚠</span> Brecha</span> : t.value === null ? 'Pendiente' : t.value === -1 ? 'No aplica' : t.expected > 0 && t.value >= t.expected ? <span className="cd-met">✓ Cumple</span> : 'Sin requisito'}</td></tr>)}</tbody></table></div>{omitted.length > 0 && <p className="cd-muted">Complementarias que no utiliza y sin requisito: {omitted.map(t => t.name).join(', ')}.</p>}</section>;
        })}</div></section>
    </div>;
}
