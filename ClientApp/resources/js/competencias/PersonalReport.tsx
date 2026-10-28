import { useState } from 'react';
import { Alert, Button, MenuItem, Tab, Tabs, TextField, useMediaQuery } from '@mui/material';
import { router } from '@/lib/navigation';
import { base, levels, percent, unitName } from './types';
import type { Report, Survey } from './types';
import { Recommendations } from './Aggregates';

function Level({ value }: { value: number | null }) {
    return <span className={`cd-level cd-level-${value ?? 'na'}`}>{value === null ? 'Sin evaluación aplicable' : levels[value]}</span>;
}
function Gap({ count }: { count: number }) { return <span className="cd-gap"><span aria-hidden="true">⚠</span> {count} {count === 1 ? 'brecha' : 'brechas'}</span>; }
function Course({ course: c, block }: { course: Report['route'][number]; block: string }) {
    return <article className="cd-course"><span className="cd-eyebrow">{block}</span><h4>{c.name}</h4>
        <p className="cd-course-levels"><span>Tu nivel <Level value={c.from} /></span><span>Objetivo <Level value={c.to} /></span></p>
        <p><strong>Propósito: </strong>{c.why}</p><ul>{c.courses.map(s => <li key={s}>{s}</li>)}</ul>
        {c.missing.length > 0 && <p>Por desarrollar: {c.missing.join(' · ')}</p>}{c.license && <small>{c.license}</small>}
    </article>;
}
export default function PersonalReport({ survey, canSurvey }: { survey: Survey; canSurvey: boolean }) {
    // El servidor proyecta respuestas reales; el informe definitivo proviene de su instantánea.
    const r = survey.evaluation.report, institution = survey.institution;
    const [blockId, setBlockId] = useState(r.blocks[0]?.id ?? 'm365');
    const mobile = useMediaQuery('(max-width:700px)');
    const known = survey.instrument.modules.flatMap(b => b.tools);
    const unselected = (t: Report['tools'][number]) => t.value === null && !survey.answers[t.id] && known.some(k => k.id === t.id && k.opt);
    const label = (unit: typeof institution.unit) => unit ? unitName(unit) : institution.status === 'available' ? 'No aplica' : 'Información institucional pendiente';
    const goSurvey = (text: string) => canSurvey && <Button variant="contained" onClick={() => router.visit(`${base}/encuesta`)}>{text}</Button>;
    if (!survey.completedAt && !survey.hasResponses) return <section className="cd-card cd-welcome cd-report-empty">
        <span className="cd-eyebrow">Mi informe y ruta</span><h2>Todavía no tienes un informe</h2>
        <p>Los resultados aparecerán al responder la encuesta. Los datos institucionales y el perfil laboral no son una evaluación de tus competencias.</p>
        {goSurvey('Ir a la encuesta')}
    </section>;
    const required = r.tools.filter(t => t.expected > 0), covered = required.filter(t => t.value !== null && t.value >= t.expected).length;
    const block = r.blocks.find(b => b.id === blockId) ?? r.blocks[0], tools = r.tools.filter(t => t.block === block?.id);
    const toolLevel = (t: Report['tools'][number]) => t.value === null ? unselected(t) ? 'Sin seleccionar' : 'Pendiente' : t.value === -1 ? 'No aplica' : t.value === -2 ? 'No utiliza' : <Level value={t.value} />;
    const toolState = (t: Report['tools'][number]) => t.value === null ? unselected(t) ? 'Complementaria sin seleccionar' : 'Pendiente' : t.value === -1 ? 'No aplica' : t.gap > 0 ? <span className="cd-gap"><span aria-hidden="true">⚠</span> Brecha</span> : t.value === -2 ? 'No utiliza' : t.expected > 0 && t.value >= t.expected ? <span className="cd-met">✓ Cumple</span> : 'Sin requisito';
    return <div className="cd-report">
        {survey.editionUpdate && <Alert severity="warning">Este informe preliminar corresponde a la edición anterior. Revisa y confirma la actualización del borrador en Encuesta; las respuestas vigentes se conservarán. {goSurvey('Revisar actualización')}</Alert>}
        <section className="cd-report-summary" aria-labelledby="cd-report-title">
            {!survey.completedAt ? <Alert severity="info">Informe preliminar · Encuesta en curso. Sólo las respuestas guardadas se evalúan; los resultados pueden cambiar. {goSurvey('Continuar encuesta')}</Alert>
                : <Alert severity="success">Informe definitivo · Finalizada el {new Date(survey.completedAt).toLocaleString('es-MX')}. Las respuestas ya no se pueden modificar.</Alert>}
            <div className="cd-card cd-report-hero"><div><h2 id="cd-report-title">Mi informe y ruta</h2><h3>{institution.name}</h3>
                <p>{survey.profile.puesto || 'Perfil pendiente'} · {label(institution.level2)}</p><p>{label(institution.level3)}</p><p className="cd-muted">Instrumento: {survey.instrument.version}</p></div>
                <div className="cd-global"><span className="cd-eyebrow">Índice global</span>{r.global === null ? <p>Sin puntuación aplicable</p> : <strong>{percent(r.global)}</strong>}<Level value={r.level} />
                    <p>{covered} de {required.length} requisitos del puesto cubiertos</p><small>No aplica, pendientes y no utilizadas no acreditan competencia.</small></div></div>
            <div className="cd-report-blocks">{r.blocks.map(b => {
                const items = r.tools.filter(t => t.block === b.id), values = levels.map((_, i) => items.filter(t => t.value === i).length), total = values.reduce((a, v) => a + v, 0), gaps = items.filter(t => t.gap > 0).length;
                return <section className="cd-card" key={b.id}><h3>{b.name}</h3><strong className="cd-block-score">{b.score === null ? 'Sin puntuación' : percent(b.score)}</strong>
                    <div className="cd-report-tags"><Level value={b.level} />{gaps ? <Gap count={gaps} /> : total > 0 && <span className="cd-met">✓ Sin brechas evaluadas</span>}</div>
                    <div className="cd-distribution" aria-label={`Distribución de ${b.name}: ${values.map((n, i) => `${levels[i]} ${n}`).join(', ')}`} role="img">{values.map((n, i) => n > 0 && <span key={i} className={`cd-level-${i}`} style={{ width: `${n / total * 100}%` }} aria-hidden="true">{n}</span>)}</div>
                    <p className="cd-muted">{total} evaluadas · {items.filter(t => t.value === -1).length} No aplica · {items.filter(t => t.value === -2).length} No utiliza · {items.filter(t => t.value === null && !unselected(t)).length} pendientes</p>
                    {items.some(unselected) && <p className="cd-muted">{items.filter(unselected).length} complementarias sin seleccionar</p>}
                </section>;
            })}</div>
            <div className="cd-report-legend" aria-label="Leyenda de niveles">{levels.map((name, i) => <span key={name}><i className={`cd-level-${i}`} aria-hidden="true" />{name}</span>)}</div>
        </section>
        <section className="cd-report-section cd-report-recommendations" aria-labelledby="cd-recommendations-title"><h2 id="cd-recommendations-title">Recomendaciones personales</h2>
            {(['warning', 'info', 'success'] as const).map(kind => {
                const values = r.recommendations.filter(v => v.kind === kind);
                return values.length > 0 && <section className="cd-recommendation-group" key={kind}><h3>{({ warning: 'Prioridades de atención', info: 'Siguientes pasos', success: 'Fortalezas' })[kind]}</h3><Recommendations values={values} /></section>;
            })}
        </section>
        <section className="cd-report-section cd-report-training" aria-labelledby="cd-training-title"><h2 id="cd-training-title">Tu ruta de capacitación</h2><p>Cursos ordenados por prioridad, desde tu nivel hasta el siguiente objetivo. La disponibilidad y las licencias deben confirmarse con la institución.</p>
            {survey.instrument.trainingPhases.map((phase, i) => {
                const courses = r.route.filter(c => c.phase === i + 1), limit = i === 2 ? 4 : 6, [title, period] = phase.title.split(' · ');
                const course = (c: Report['route'][number]) => <Course key={c.tool} course={c} block={r.blocks.find(b => b.id === c.block)?.name ?? c.block} />;
                return <section className="cd-phase" key={phase.id}><header className="cd-phase-heading"><span className="cd-phase-number" aria-hidden="true">{i + 1}</span><div><h3>{title}</h3>{period && <p>{period}</p>}</div><span>{courses.length} propuestas</span></header>
                    <p>{phase.description}</p><div className="cd-course-grid">{courses.slice(0, limit).map(course)}</div>
                    {courses.length > limit && <details><summary>Ver las {courses.length - limit} propuestas restantes de esta fase</summary><div className="cd-course-grid">{courses.slice(limit).map(course)}</div></details>}
                    {!courses.length && <p className="cd-muted">Sin cursos sugeridos para esta fase. Esto no acredita por sí solo un nivel Avanzado.</p>}
                </section>;
            })}
            {r.transversal.length > 0 && <section className="cd-card"><h3>Cursos transversales</h3><ul>{r.transversal.map(c => <li key={c}>{c}</li>)}</ul></section>}
        </section>
        <section className="cd-report-section cd-report-detail" aria-labelledby="cd-detail-title"><h2 id="cd-detail-title">Resultado por herramienta</h2><p>Tu nivel frente al esperado por las funciones de tu puesto. Seleccionar un bloque cambia sólo este detalle.</p>
            {mobile ? <TextField select fullWidth label="Bloque del detalle" value={block?.id ?? ''} onChange={e => setBlockId(e.target.value)}>{r.blocks.map(b => <MenuItem key={b.id} value={b.id} sx={{ whiteSpace: 'normal' }}>{b.name}</MenuItem>)}</TextField>
                : <Tabs value={block?.id} onChange={(_, value: string) => setBlockId(value)} variant="scrollable" scrollButtons="auto" aria-label="Bloque del detalle">{r.blocks.map(b => <Tab key={b.id} value={b.id} label={b.name} id={`cd-detail-tab-${b.id}`} aria-controls={`cd-detail-panel-${b.id}`} />)}</Tabs>}
            <div className="cd-report-tools" role={mobile ? 'region' : 'tabpanel'} id={`cd-detail-panel-${block?.id}`} aria-labelledby={mobile ? 'cd-detail-block' : `cd-detail-tab-${block?.id}`} tabIndex={0}>
                <h3 id="cd-detail-block">{block?.name}</h3>
                {mobile ? <div className="cd-tool-cards">{tools.map(t => <article className="cd-tool-card" key={t.id}><span className="cd-eyebrow">Herramienta o tema</span><h4>{t.name}</h4><dl>
                    <div><dt>Tu nivel</dt><dd>{toolLevel(t)}</dd></div><div><dt>Esperado</dt><dd>{t.expected ? levels[t.expected] : 'Sin requisito'}</dd></div><div><dt>Estado</dt><dd>{toolState(t)}</dd></div>
                </dl></article>)}</div> : <table><colgroup><col className="cd-tool-name-col" /><col /><col /><col /></colgroup><thead><tr><th scope="col">Herramienta o tema</th><th scope="col">Tu nivel</th><th scope="col">Esperado</th><th scope="col">Estado</th></tr></thead><tbody>{tools.map(t => <tr key={t.id}><th scope="row">{t.name}</th><td>{toolLevel(t)}</td><td>{t.expected ? levels[t.expected] : 'Sin requisito'}</td><td>{toolState(t)}</td></tr>)}</tbody></table>}
                {!tools.length && <p>Este bloque no tiene herramientas aplicables a tu perfil.</p>}
            </div>
        </section>
    </div>;
}
