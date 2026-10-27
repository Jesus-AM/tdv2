import { useEffect, useRef, useState } from 'react';
import axios from 'axios';
import { Alert, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, LinearProgress } from '@mui/material';
import { router } from '@/lib/navigation';
import { errorText } from '@/lib/http';
import { base, levels, percent, unitName } from './types';
import type { Answer, Survey, Tool } from './types';
import PrivacyNotice from './PrivacyNotice';

function ProfileFields({ survey, profile, disabled, onChange }: { survey: Survey; profile: Survey['profile']; disabled: boolean; onChange: (id: string, value: string | string[]) => void }) {
    const institution = survey.institution;
    const label = (unit: typeof institution.unit) => unit ? unitName(unit) : institution.status === 'available' ? 'No aplica' : 'Información institucional pendiente';
    const fields = (ids: string[]) => ids.map(id => survey.instrument.profile.find(f => f.id === id)!).map(f => ['antig', 'puesto'].includes(f.id)
        ? <label className="cd-field" key={f.id}>{f.label} *<select id={`perfil:${f.id}`} required disabled={disabled} value={profile[f.id] as string || ''} onChange={e => onChange(f.id, e.target.value)}><option value="">Selecciona una opción</option>{f.options.map(o => <option key={o}>{o}</option>)}</select></label>
        : <fieldset className={`cd-functions ${f.id === 'funciones' || f.id === 'freq' ? 'cd-wide' : ''}`} id={`perfil:${f.id}`} key={f.id} disabled={disabled}><legend>{f.label} *</legend><div className="cd-choices">{f.options.map(o => <label className="cd-pill" key={o}><input type={f.multiple ? 'checkbox' : 'radio'} name={f.id} checked={f.multiple ? Array.isArray(profile[f.id]) && profile[f.id].includes(o) : profile[f.id] === o} onChange={e => {
            if (!f.multiple) onChange(f.id, o);
            else { const values = Array.isArray(profile[f.id]) ? profile[f.id] as string[] : []; onChange(f.id, e.target.checked ? [...values, o] : values.filter(v => v !== o)); }
        }} />{o}</label>)}</div></fieldset>);
    return <div className="cd-profile-groups">
        <section className="cd-profile-group" aria-labelledby="cd-institution-title">
        <h3 id="cd-institution-title">Datos institucionales</h3>
        <p className="cd-eyebrow">Información completada automáticamente: nombre y adscripción</p>
        {institution.status !== 'available' && <div id="perfil:institucion" tabIndex={-1}><Alert severity="warning">La adscripción institucional está pendiente o es inconsistente. Puedes guardar el borrador; solicita su revisión para continuar al siguiente paso.</Alert></div>}
        <div className="cd-form-grid">
            <div className="cd-field cd-wide"><span>Nombre completo</span><div className="cd-readonly">{institution.name}</div></div>
            <div className="cd-field"><span>Dependencia, instituto o dirección</span><div className="cd-readonly">{label(institution.level2)}</div></div>
            <div className="cd-field"><span>Subdirección, coordinación o departamento</span><div className="cd-readonly">{label(institution.level3)}</div></div>
        </div>
        </section>
        <section className="cd-profile-group" aria-labelledby="cd-work-title"><h3 id="cd-work-title">Perfil laboral</h3><div className="cd-form-grid">{fields(['puesto', 'antig', 'funciones', 'sup'])}</div></section>
        <section className="cd-profile-group" aria-labelledby="cd-tools-title"><h3 id="cd-tools-title">Uso de herramientas e información</h3><div className="cd-form-grid">{fields(['freq', 'datosP'])}</div></section>
    </div>;
}
function Question({ tool, block, answer, onChange, readOnly }: { tool: Tool; block: string; answer: Answer | undefined; onChange: (a: Answer) => void; readOnly: boolean }) {
    const checklist = ['ciber', 'datos', 'sii'].includes(block);
    const checks = answer?.checks || '000000000000';
    const status = answer?.state === 'na' ? 'No aplica' : answer?.state === 'level' ? checklist ? 'Rúbrica respondida' : levels[answer.level!] : 'Pendiente';
    const choose = (value: string) => onChange(value === 'na' ? { state: 'na' } : { state: 'level', level: Number(value) });
    return <article className={`cd-question ${answer?.state === 'level' || answer?.state === 'na' ? 'cd-answered' : ''}`} id={tool.id} tabIndex={-1} aria-labelledby={`${tool.id}-title`}>
        <div className="cd-section-title"><h3 id={`${tool.id}-title`}>{tool.n}</h3><Chip size="small" label={status} /></div>
        {checklist ? <>
            <div className="cd-question-actions"><Button variant={answer?.state === 'level' && checks === '000000000000' ? 'contained' : 'outlined'} disabled={readOnly} onClick={() => onChange({ state: 'level', checks: '000000000000' })}>No realizo ninguna · Inicial</Button><Button variant={answer?.state === 'na' ? 'contained' : 'outlined'} disabled={readOnly} onClick={() => onChange({ state: 'na' })}>No aplica</Button>{answer?.state === 'na' && <Button disabled={readOnly} onClick={() => onChange({ state: 'pending' })}>Responder la rúbrica</Button>}</div>
            <div className="cd-rubrics">{tool.lv.map((items, k) => <fieldset key={k} className={checks.slice(k * 4, k * 4 + 4).split('1').length - 1 >= 3 ? 'cd-rubric-met' : undefined} disabled={readOnly || answer?.state === 'na'}><legend>{levels[k + 1]} <span className="cd-rubric-count">{checks.slice(k * 4, k * 4 + 4).split('1').length - 1}/4</span></legend>{items.map((text, j) => <label className="cd-check" key={j}><input type="checkbox" checked={checks[k * 4 + j] === '1'} onChange={e => { const next = checks.split(''); next[k * 4 + j] = e.target.checked ? '1' : '0'; onChange(next.includes('1') ? { state: 'level', checks: next.join('') } : { state: 'pending' }); }} /><span>{text}</span></label>)}</fieldset>)}</div>
        </> : <fieldset className="cd-radio-question" disabled={readOnly}><legend className="cd-sr-only">Nivel de competencia en {tool.n}</legend>
            <div className="cd-question-actions">{['0', 'na'].map(v => <label className="cd-pill" key={v}><input type="radio" name={tool.id} value={v} checked={v === 'na' ? answer?.state === 'na' : answer?.state === 'level' && answer.level === 0} onChange={() => choose(v)} />{v === 'na' ? 'No aplica' : 'Inicial'}</label>)}</div>
            <div className="cd-rubrics">{tool.lv.map((items, k) => <label className="cd-option" key={k}><span><input type="radio" name={tool.id} value={k + 1} aria-label={levels[k + 1]} checked={answer?.state === 'level' && answer.level === k + 1} onChange={() => choose(String(k + 1))} /><b>{levels[k + 1]}</b></span><ul>{items.map((t, j) => <li key={j}>{t}</li>)}</ul></label>)}</div>
        </fieldset>}
        {tool.lic && <small className="cd-muted">{tool.lic}</small>}
    </article>;
}

export default function SurveyView({ initial, readOnly, canReport }: { initial: Survey; readOnly: boolean; canReport: boolean }) {
    const [survey, setSurvey] = useState(initial), [profile, setProfile] = useState(initial.profile), [answers, setAnswers] = useState(initial.answers);
    const [step, setStep] = useState(initial.navigation.current), [dirty, setDirty] = useState(false), [edit, setEdit] = useState(0);
    const [error, setError] = useState(''), [message, setMessage] = useState(''), [busy, setBusy] = useState(false);
    const [noticeOpen, setNoticeOpen] = useState(!initial.accepted && !initial.completedAt && !readOnly);
    const [finishOpen, setFinishOpen] = useState(false), [pending, setPending] = useState<string[]>([]);
    const progress = useRef<HTMLDivElement>(null);
    const current = useRef({ survey, profile, answers, dirty, step }); current.current = { survey, profile, answers, dirty, step };
    const saving = useRef<Promise<Survey | null> | null>(null);
    const disabled = readOnly || busy || !survey.accepted;
    const failed = (e: unknown) => { setError(errorText(e)); if (axios.isAxiosError(e) && e.response?.data?.reset) window.dispatchEvent(new Event('cd-generation-invalid')); };
    const change = () => { current.current.dirty = true; setDirty(true); setEdit(e => e + 1); setMessage(''); setError(''); setPending([]); };
    const showStep = (id: string) => {
        current.current.step = id; setStep(id); setPending([]);
        window.history.replaceState(null, '', `${base}/encuesta?paso=${id}`);
        requestAnimationFrame(() => document.getElementById('cd-step-content')?.scrollIntoView({ block: 'start' }));
    };
    const focusPending = (ids: string[]) => setPending(ids);
    useEffect(() => {
        if (busy || !pending.length) return;
        const frame = requestAnimationFrame(() => {
            // El orden de lectura del perfil puede diferir del orden histórico del instrumento.
            const element = Array.from(document.querySelectorAll<HTMLElement>('[id]')).find(e => pending.includes(e.id));
            const target = element?.matches('input,select,button') ? element : element?.querySelector<HTMLElement>('input:not(:disabled),select:not(:disabled),button:not(:disabled)') || element;
            target?.focus({ preventScroll: true }); target?.scrollIntoView({ block: 'center' });
        });
        return () => cancelAnimationFrame(frame);
    }, [pending, busy, step]);
    function persist(force = false): Promise<Survey | null> {
        if (saving.current) return saving.current;
        const draft = current.current;
        if (!draft.dirty && !force) return Promise.resolve(draft.survey);
        setBusy(true); setError(''); setMessage('');
        saving.current = (async () => {
            try {
                const saved = (await axios.put<Survey>(`${base}/api/encuesta`, { generation: draft.survey.generation, version: draft.survey.version, profile: draft.profile, answers: draft.answers })).data;
                current.current = { ...current.current, survey: saved, profile: saved.profile, answers: saved.answers, dirty: false };
                setSurvey(saved); setProfile(saved.profile); setAnswers(saved.answers); setDirty(false);
                setMessage('Avance guardado. Puedes continuar desde otro dispositivo.');
                if (!saved.navigation.steps.find(s => s.id === current.current.step)?.accessible) showStep(saved.navigation.resume);
                return saved;
            } catch (e) { failed(e); return null; }
            finally { saving.current = null; setBusy(false); }
        })();
        return saving.current;
    }
    const persistRef = useRef(persist); persistRef.current = persist;
    useEffect(() => {
        if (!edit || readOnly || survey.completedAt || !survey.accepted) return;
        const timer = window.setTimeout(() => { if (current.current.dirty) void persistRef.current(); }, 1500);
        return () => clearTimeout(timer);
    }, [edit, readOnly, survey.accepted, survey.completedAt]);
    useEffect(() => {
        const stop = router.on('before', e => {
            if (!current.current.dirty && !saving.current) return;
            e.preventDefault();
            void persistRef.current().then(saved => { if (saved) e.detail.visit.resume?.(); });
        });
        const unload = (e: BeforeUnloadEvent) => { if (current.current.dirty || saving.current) { e.preventDefault(); e.returnValue = ''; } };
        window.addEventListener('beforeunload', unload); return () => { stop(); window.removeEventListener('beforeunload', unload); };
    }, []);
    useEffect(() => {
        const element = progress.current, site = element?.closest<HTMLElement>('.cd-site');
        if (!element || !site) return;
        const observer = new ResizeObserver(() => site.style.setProperty('--cd-progress-height', `${element.getBoundingClientRect().height}px`));
        observer.observe(element); return () => observer.disconnect();
    }, [survey.completedAt]);
    async function navigate(target: string, continuing = false) {
        const saved = await persist(continuing); if (!saved) return;
        const thisStep = saved.navigation.steps.find(s => s.id === current.current.step)!;
        if (continuing && !thisStep.complete) { setMessage(''); setError('Completa los campos pendientes de esta sección para continuar.'); focusPending(thisStep.pending); return; }
        if (!saved.navigation.steps.find(s => s.id === target)?.accessible) {
            setError('Completa y guarda las secciones anteriores para abrir este bloque.');
            showStep(saved.navigation.resume); focusPending(saved.navigation.steps.find(s => s.id === saved.navigation.resume)!.pending); return;
        }
        showStep(target); setError('');
    }
    async function accept() {
        if (busy) return; setBusy(true); setError('');
        try { const saved = (await axios.post<Survey>(`${base}/api/aviso`, { generation: survey.generation, version: survey.instrument.notice.version })).data;
            setSurvey(saved); current.current.survey = saved; setNoticeOpen(false);
        } catch (e) { failed(e); } finally { setBusy(false); }
    }
    async function finish() {
        const saved = await persist(); if (!saved) { setFinishOpen(false); return; }
        setBusy(true); setError('');
        try { const final = (await axios.post<Survey>(`${base}/api/encuesta/finalizar`, { generation: saved.generation, version: saved.version })).data;
            setSurvey(final); current.current.survey = final; setMessage('Encuesta finalizada correctamente.');
        } catch (e) { failed(e); const id = saved.navigation.resume; showStep(id); focusPending(saved.navigation.steps.find(s => s.id === id)!.pending); }
        finally { setBusy(false); setFinishOpen(false); }
    }
    const steps = survey.navigation.steps, stepIndex = steps.findIndex(s => s.id === step), block = survey.instrument.modules.find(b => b.id === step);
    const applicable = block?.tools.filter(t => !t.doc || profile.puesto === 'Personal docente') || [];
    const visible = applicable.filter(t => !t.opt || answers[t.id] && answers[t.id].state !== 'unused');
    const categories = [...new Set(applicable.filter(t => !t.opt).map(t => t.cat))];
    const name = (id: string) => survey.instrument.modules.flatMap(m => m.tools).find(t => t.id === id)?.n || survey.instrument.profile.find(f => `perfil:${f.id}` === id)?.label || 'Información institucional pendiente';
    const answer = (id: string, a: Answer) => { setAnswers(prev => { const next = { ...prev, [id]: a }; current.current.answers = next; return next; }); change(); };
    return <>
        {survey.completedAt ? <section className="cd-card"><h2>Tu encuesta ya fue completada</h2><p>Finalizada el {new Date(survey.completedAt).toLocaleString('es-MX')}. La respuesta es definitiva.</p>{canReport && <Button variant="contained" onClick={() => router.visit(`${base}/mi-informe-ruta`)}>Ver mi informe</Button>}</section> : <>
            <nav className="cd-steps" aria-label="Bloques de la encuesta">{steps.map((s, i) => <button key={s.id} className={s.complete ? 'cd-done' : ''} aria-current={step === s.id ? 'step' : undefined} aria-label={s.name} disabled={busy || !s.accessible} title={!s.accessible ? 'Completa y guarda los pasos anteriores' : s.complete ? 'Sección completa' : 'Sección pendiente'} onClick={() => void navigate(s.id)}><span className="cd-dot" aria-hidden="true" />{i + 1}. {s.name}{s.complete && <span className="cd-sr-only"> · Completa</span>}</button>)}</nav>
            <div className="cd-progress" ref={progress}><span>Paso {stepIndex + 1} de 6 · {steps[stepIndex].name}</span><LinearProgress variant="determinate" value={survey.evaluation.progress} aria-label="Progreso de llenado guardado" /><strong>{percent(survey.evaluation.progress)}</strong><small>{survey.evaluation.answered}/{survey.evaluation.total} respuestas guardadas</small></div>
            <div className="cd-feedback" aria-live="polite">{error && <Alert severity="error">{error}</Alert>}{message && <p className="cd-saved" role="status">{message}</p>}{busy && <p role="status">Guardando…</p>}</div>
            {!survey.accepted && !readOnly && <Alert severity="info" action={<Button onClick={() => setNoticeOpen(true)}>Leer y confirmar</Button>}>Confirma el aviso de privacidad antes de comenzar.</Alert>}
            <div id="cd-step-content" tabIndex={-1} className={step === 'perfil' ? 'cd-card cd-profile-panel' : undefined}>
            {step === 'perfil' ? <section className="cd-profile-body"><span className="cd-eyebrow">Paso 1 de 6</span><h2>Comencemos por conocerte</h2><p className="cd-profile-intro">Con estos datos ubicamos tus resultados en tu dependencia y comparamos tu nivel con lo que requieren las funciones de tu puesto. Así tu ruta de capacitación será realmente tuya. Los campos con * son obligatorios.</p>
                <ProfileFields survey={survey} profile={profile} disabled={disabled} onChange={(id, value) => { setProfile(p => { const next = { ...p }; if (value) next[id] = value; else delete next[id]; current.current.profile = next; return next; }); change(); }} />
            </section> : block && <section className="cd-block"><div className="cd-block-intro"><h2>{block.n}</h2><p>{block.d}</p><p className="cd-instructions">{['ciber', 'datos', 'sii'].includes(block.id) ? 'Marca lo que puedes realizar. Cada nivel requiere al menos tres de sus cuatro indicadores y dominar los niveles anteriores. Si no realizas ninguno, confirma Inicial.' : 'Selecciona el nivel más alto que dominas de forma consistente. Cada nivel incluye las capacidades de los anteriores. Inicial es una respuesta válida.'}</p></div>
                {categories.map(category => <section key={category}><div className="cd-category-row"><h3>{category}</h3><Button variant="text" disabled={disabled} onClick={() => { setAnswers(previous => { const next = { ...previous }; for (const tool of applicable.filter(t => !t.opt && t.cat === category)) next[tool.id] = ['ciber', 'datos', 'sii'].includes(block.id) ? { state: 'level', checks: '000000000000' } : { state: 'level', level: 0 }; current.current.answers = next; return next; }); change(); }}>{['ciber', 'datos', 'sii'].includes(block.id) ? 'No realizo ninguna de estas' : 'No uso ninguna de estas · marcar Inicial'}</Button></div><div className="cd-questions">{applicable.filter(t => !t.opt && t.cat === category).map(t => <Question key={t.id} tool={t} block={block.id} answer={answers[t.id]} readOnly={disabled} onChange={a => answer(t.id, a)} />)}</div></section>)}
                {applicable.some(t => t.opt) && <section className="cd-card cd-optional"><h3>Herramientas complementarias</h3><p className="cd-muted">Selecciona únicamente las que utilizas. Las demás se registran como «No utiliza la herramienta».</p><div className="cd-choices">{applicable.filter(t => t.opt).map(t => <label className="cd-pill" key={t.id}><input type="checkbox" disabled={disabled} checked={!!answers[t.id] && answers[t.id].state !== 'unused'} onChange={e => answer(t.id, { state: e.target.checked ? 'pending' : 'unused' })} />{t.n}</label>)}</div></section>}
                {visible.some(t => t.opt) && <section><h3 className="cd-category">Complementarias que usas</h3><div className="cd-questions">{visible.filter(t => t.opt).map(t => <Question key={t.id} tool={t} block={block.id} answer={answers[t.id]} readOnly={disabled} onChange={a => answer(t.id, a)} />)}</div></section>}
            </section>}
            {pending.length > 0 && <Alert severity="warning"><p>Pendientes de esta sección:</p><div className="cd-pending">{pending.map(id => <Button key={id} onClick={() => focusPending([id, ...pending.filter(p => p !== id)])}>{name(id)}</Button>)}</div></Alert>}
            <div className="cd-savebar"><div>{stepIndex > 0 && <Button variant="outlined" disabled={busy} onClick={() => void navigate(steps[stepIndex - 1].id)}>← Anterior</Button>}</div><div className="cd-save-actions"><span role="status">{busy ? 'Guardando…' : dirty ? 'Cambios sin guardar' : 'Avance guardado'}<small>{steps[stepIndex].pending.length ? `${steps[stepIndex].pending.length} pendientes en el último guardado` : 'Sección completa'}</small></span>{!readOnly && <><Button variant="outlined" disabled={disabled} onClick={() => void persist(true)}>Guardar avance</Button>{stepIndex < 5 ? <Button variant="contained" disabled={disabled} onClick={() => void navigate(steps[stepIndex + 1].id, true)}>Guardar y continuar</Button> : <Button variant="contained" disabled={disabled} onClick={async () => { const saved = await persist(true); if (!saved) return; const incomplete = saved.navigation.steps.find(s => !s.complete); if (incomplete) { showStep(incomplete.id); setError('Completa los campos pendientes antes de finalizar.'); focusPending(incomplete.pending); } else setFinishOpen(true); }}>Finalizar encuesta</Button>}</>}</div></div>
            </div>
        </>}
        <PrivacyNotice key={noticeOpen ? 'accept-open' : 'accept-closed'} notice={survey.instrument.notice} open={noticeOpen} onClose={() => setNoticeOpen(false)} onAccept={!survey.accepted && !survey.completedAt && !readOnly ? () => void accept() : undefined} busy={busy} />
        <Dialog open={finishOpen} onClose={() => !busy && setFinishOpen(false)} aria-labelledby="cd-finish-title"><DialogTitle id="cd-finish-title">Finalizar tu única respuesta</DialogTitle><DialogContent><p>Después de finalizar no podrás editar, reiniciar, borrar ni responder nuevamente esta encuesta. Tu informe seguirá disponible.</p><p>Se comprobarán todas las preguntas aplicables de los cinco bloques.</p></DialogContent><DialogActions><Button disabled={busy} onClick={() => setFinishOpen(false)}>Continuar revisando</Button><Button variant="contained" disabled={busy} onClick={() => void finish()}>Confirmar finalización</Button></DialogActions></Dialog>
    </>;
}
