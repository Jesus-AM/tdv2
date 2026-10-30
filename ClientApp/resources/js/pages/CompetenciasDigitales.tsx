import { useEffect, useMemo, useRef, useState } from 'react';
import axios from 'axios';
import { Alert, Button, createTheme, ThemeProvider, useTheme } from '@mui/material';
import AccountMenu from '@/Components/AccountMenu';
import InstitutionalFooter from '@/Components/InstitutionalFooter';
import PrivacyNotice from '@/competencias/PrivacyNotice';
import { Head, router, usePage } from '@/lib/navigation';
import AggregateView from '@/competencias/Aggregates';
import SurveyView from '@/competencias/Survey';
import PersonalReport from '@/competencias/PersonalReport';
import Configuration from '@/competencias/Configuration';
import { base } from '@/competencias/types';
import type { Aggregates, Config, DigitalPage, Survey } from '@/competencias/types';
import '../../css/competencias-digitales.css';

export default function CompetenciasDigitales() {
    const { props, url } = usePage(); const data = props.digital as DigitalPage;
    const [exitError, setExitError] = useState(''); const [leaving, setLeaving] = useState(false);
    const [noticeOpen, setNoticeOpen] = useState(false);
    const [invalidated, setInvalidated] = useState(false);
    const ownReset = useRef(false);
    useEffect(() => {
        setInvalidated(false); ownReset.current = false; let alive = true;
        const invalidate = () => setInvalidated(true); window.addEventListener('cd-generation-invalid', invalidate);
        const completed = () => { ownReset.current = true; }; window.addEventListener('cd-reset-operation', completed);
        const check = async () => { try { const result = (await axios.get<{ generation: string }>(base + '/api/generacion')).data; if (alive && !ownReset.current && result.generation !== data.generation) setInvalidated(true); } catch { /* La siguiente consulta conserva la autorización del servidor. */ } };
        const released = () => { ownReset.current = false; void check(); }; window.addEventListener('cd-reset-release', released);
        const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel('tdv2-competencias-generation');
        if (channel) channel.onmessage = () => void check();
        const focus = () => void check(); window.addEventListener('focus', focus);
        const interval = window.setInterval(() => { if (document.visibilityState === 'visible') void check(); }, 30000);
        return () => { alive = false; window.removeEventListener('cd-reset-release', released); window.removeEventListener('cd-generation-invalid', invalidate); window.removeEventListener('cd-reset-operation', completed); channel?.close(); window.clearInterval(interval); window.removeEventListener('focus', focus); };
    }, [data.generation]);
    const header = useRef<HTMLElement>(null);
    const parentTheme = useTheme();
    const theme = useMemo(() => createTheme(parentTheme, { typography: { fontSize: 15 }, shape: { borderRadius: 8 }, components: { MuiButton: { styleOverrides: { root: { minHeight: 40, padding: '8px 14px', fontSize: 15, fontWeight: 500 } } } } }), [parentTheme]);
    useEffect(() => {
        const element = header.current, site = element?.closest<HTMLElement>('.cd-site');
        if (!element || !site) return;
        const observer = new ResizeObserver(() => site.style.setProperty('--cd-header-height', element.getBoundingClientRect().height + 'px'));
        observer.observe(element); return () => observer.disconnect();
    }, []);
    const exit = () => { if (leaving) return; setLeaving(true); router.delete(`${base}/api/representacion`, { onError: e => setExitError(Object.values(e)[0]), onFinish: () => setLeaving(false) }); };
    const names = ['Avance por dependencia', 'Resultados generales', 'Encuesta', 'Mi informe y ruta', 'Configuración'];
    const title = names[data.page] || 'Diagnóstico de competencias digitales';
    const canSurvey = data.navigation.some(n => n.path === `${base}/encuesta`);
    const canReport = data.navigation.some(n => n.path === `${base}/mi-informe-ruta`);
    return <ThemeProvider theme={theme}><div className="cd-site"><Head title={`${title} · Competencias digitales UACJ`} />
        <a className="cd-skip" href="#cd-main">Ir al contenido</a>
        <header className="cd-header" ref={header}><div className="cd-wrap cd-header-inner"><a className="cd-brandmark" href="/" onClick={event => { event.preventDefault(); router.visit("/"); }} aria-label="Volver al portal de Transformación Digital"><img src="/images/logos/transformacion-digital-blanco.svg" alt="Transformación Digital UACJ" /></a><div className="cd-account"><AccountMenu includeNameInLabel onExit={exit} onPrivacy={() => setNoticeOpen(true)} /></div><nav className="cd-nav" aria-label="Módulos de Competencias digitales">{data.navigation.map(n => <button key={n.path} aria-current={(url.split('?')[0] === n.path || (n.path === base + '/configuracion' && data.page === 4)) ? 'page' : undefined} onClick={() => router.visit(n.path)}>{n.name}</button>)}</nav></div>
        {data.readOnly && <div className="cd-representation" role="status"><div className="cd-wrap"><strong>Consulta como {data.user}</strong><span>Cuenta administradora: {data.actor} · Sin capacidad de escritura</span><Button variant="outlined" disabled={leaving} onClick={exit}>Volver a mi usuario</Button>{exitError && <Alert severity="error">{exitError}</Alert>}</div></div>}</header>
        <div className="cd-titleband"><div className="cd-wrap"><div><h1>Diagnóstico de competencias digitales</h1><p>Una iniciativa de la Transformación Digital UACJ</p></div><span className="cd-module-context">{names[data.page] || 'Competencias digitales UACJ'}</span></div></div>
        <main className="cd-wrap" id="cd-main" tabIndex={-1} key={url + data.generation}>
            {invalidated ? <Alert severity="warning" action={<Button onClick={() => window.location.reload()}>Volver a cargar</Button>}>Un administrador reinició las encuestas. Esta vista quedó invalidada; vuelve a cargar para consultar el estado vigente.</Alert> : <>
            {data.page === -1 && <section className="cd-card cd-welcome"><span className="cd-eyebrow">Tu diagnóstico personal</span><h2>{(data.content as Survey).completedAt ? 'Tu informe definitivo está disponible' : (data.content as Survey).version > 0 ? 'Continúa donde te quedaste' : 'Conoce tus competencias digitales'}</h2><p>Identifica tus fortalezas, las capacidades que requiere tu puesto y una ruta de capacitación en los cinco bloques.</p><p>Puedes guardar tu avance y continuar desde otro dispositivo. Al finalizar, tu respuesta será definitiva.</p>{(data.content as Survey).completedAt ? canReport && <Button variant="contained" onClick={() => router.visit(`${base}/mi-informe-ruta`)}>Consultar Mi informe y ruta</Button> : canSurvey && <Button variant="contained" onClick={() => router.visit(`${base}/encuesta`)}>{(data.content as Survey).version > 0 ? 'Continuar encuesta' : 'Comenzar encuesta'}</Button>}</section>}
            {(data.page === 0 || data.page === 1) && <AggregateView initial={data.content as Aggregates} results={data.page === 1} />}
            {data.page === 2 && <SurveyView initial={data.content as Survey} readOnly={data.readOnly} canReport={canReport} />}
            {data.page === 3 && <PersonalReport survey={data.content as Survey} canSurvey={canSurvey} />}
            {data.page === 4 && <Configuration data={data.content as Config} readOnly={data.readOnly} />}
            </>}
        </main>
        <InstitutionalFooter />
        <PrivacyNotice notice={data.notice} open={noticeOpen} onClose={() => setNoticeOpen(false)} />
    </div></ThemeProvider>;
}
