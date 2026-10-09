import { useEffect, useRef, useState } from 'react';
import { Head, router } from '@/lib/navigation';
import axios from 'axios';
import {
    Alert,
    Box,
    Button,
    Checkbox,
    Chip,
    FormControlLabel,
    LinearProgress,
    MenuItem,
    Snackbar,
    Switch,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    TextField,
    Typography,
} from '@mui/material';
import { Refresh, SaveOutlined, StorageOutlined, SyncOutlined, ScheduleOutlined } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import SettingsSection from '@/Components/SettingsSection';
import { errorResponse, errorText } from '@/lib/http';
type Settings = {
    version: number;
    activa: boolean;
    intervalo_minutos: number;
    hora: string;
    zona_horaria: string;
    incluir_ilda: boolean;
};
type Status = {
    ejecucion_activa: string | null;
    proxima_en: string | null;
    procesador_visto_en: string | null;
    procesador_reciente: boolean;
    ilda_habilitada: boolean;
    procesamiento_manual?: { disponible: boolean; error: string | null };
    inicio_demorado?: boolean;
};
type Catalog = { registros: number | null; completada_en: string | null; error?: string };
type Result = {
    estado: string;
    mensaje?: string;
    resumen?: { unidades?: number; registros?: number; columnas?: number; ejercicio?: number };
};
type Run = {
    id: string;
    fuentes: string;
    origen: string;
    solicitado_por: string;
    estado: string;
    etapa: string | null;
    solicitada_en: string;
    iniciada_en: string | null;
    terminada_en: string | null;
    resultado: Record<string, Result>;
};
const labels: Record<string, string> = {
    pendiente: 'Pendiente',
    ejecutando: 'En proceso',
    completada: 'Completada',
    parcial: 'Parcial',
    fallida: 'Con error',
};
const catalogLabels: Record<string, string> = { sii: 'SII · Unidades responsables', sii_modulos: 'SII · Módulos de SIIv2', ilda: 'ILDA' };
const intervals = [
    [15, 'Cada 15 minutos'],
    [30, 'Cada 30 minutos'],
    [60, 'Cada hora'],
    [180, 'Cada 3 horas'],
    [360, 'Cada 6 horas'],
    [1440, 'Diariamente'],
] as const;
export default function Sincronizaciones({
    configuracion,
    estado,
    catalogos,
    historial,
}: {
    configuracion: Settings;
    estado: Status;
    catalogos: { sii: Catalog; sii_modulos: Catalog; ilda: Catalog };
    historial: Run[];
}) {
    const requesting = useRef(false);
    const refreshFlight = useRef<Promise<boolean> | null>(null);
    const [starting, setStarting] = useState<string | null>(null);
    const [unconfirmed, setUnconfirmed] = useState(false);
    const [draft, setDraft] = useState(configuracion),
        [busy, setBusy] = useState(false),
        [refreshing, setRefreshing] = useState(false),
        [error, setError] = useState(''),
        [statusError, setStatusError] = useState(''),
        [message, setMessage] = useState(''),
        [conflict, setConflict] = useState(false);
    useEffect(() => {
        setDraft(configuracion);
    }, [configuracion.version]);
    useEffect(() => {
        const timer = setInterval(
            () => {
                if (document.visibilityState === 'visible' && !refreshing && !busy) refresh();
            },
            estado.ejecucion_activa || unconfirmed ? 1000 : 15000,
        );
        const visible = () => { if (document.visibilityState === 'visible' && !busy) void refresh(); };
        document.addEventListener('visibilitychange', visible);
        return () => { clearInterval(timer); document.removeEventListener('visibilitychange', visible); };
    }, [estado.ejecucion_activa, refreshing, busy, unconfirmed]);
    function refresh(manual = false): Promise<boolean> {
        if (refreshFlight.current) return refreshFlight.current;
        setRefreshing(true);
        let finish!: (confirmed: boolean) => void;
        let confirmed = false;
        const flight = new Promise<boolean>(resolve => { finish = resolve; });
        refreshFlight.current = flight;
        router.reload({ only: ['estado', 'historial', 'catalogos'],
            onError: (errors) => setStatusError(errors.general),
            onSuccess: () => { confirmed = true; setUnconfirmed(false); setStatusError(''); if (manual) setMessage('Estado actualizado.'); },
            onFinish: () => { refreshFlight.current = null; setRefreshing(false); finish(confirmed); },
        });
        return flight;
    }
    const date = (value: string | null) =>
        value
            ? new Intl.DateTimeFormat('es-MX', {
                  timeZone: configuracion.zona_horaria,
                  dateStyle: 'short',
                  timeStyle: 'short',
              }).format(new Date(value))
            : 'Sin registro';
    async function execute(fuentes: 'sii' | 'ilda' | 'ambas') {
        if (requesting.current || busy || unconfirmed || estado.ejecucion_activa) return;
        requesting.current = true;
        setStarting(fuentes);
        setBusy(true);
        setError('');
        setConflict(false);
        try {
            const response = await axios.post('/configuracion/sincronizaciones/ejecutar', { fuentes });
            setMessage(response.data.message);
        } catch (e) {
            setError(errorText(e));
        } finally {
            // Incluso si se pierde el ACK, consultar la cola antes de permitir otra solicitud.
            if (refreshFlight.current) await refreshFlight.current;
            if (!await refresh()) setUnconfirmed(true);
            setBusy(false);
            setStarting(null);
            requesting.current = false;
        }
    }
    async function save() {
        if (busy) return;
        setBusy(true);
        setError('');
        setConflict(false);
        try {
            const response = await axios.put('/configuracion/sincronizaciones/programacion', draft);
            setMessage(response.data.message);
            router.reload({ only: ['configuracion', 'estado'] });
        } catch (e) {
            setError(errorText(e));
            setConflict(errorResponse(e)?.status === 409);
        } finally {
            setBusy(false);
        }
    }
    const active = historial.find((r) => r.id === estado.ejecucion_activa);
    const actionLabel = (source: string) => {
        const matches = active && (active.fuentes === source || active.fuentes === 'ambas');
        if (matches) return active.estado === 'ejecutando' ? 'Sincronizando…' : 'Iniciando…';
        return starting === source || starting === 'ambas' ? 'Iniciando…' : null;
    };
    const dirty = JSON.stringify(draft) !== JSON.stringify(configuracion);
    return (
        <AuthenticatedLayout>
            <Head title="Sincronizaciones" />
            <div className="page settings-page">
                <PageHeading
                    breadcrumbs={[{ label: 'Configuración', href: '/configuracion' }]}
                    title="Sincronizaciones"
                    description="Mantén actualizadas las áreas y el inventario que utilizan los formatos."
                    actions={
                        <Button variant="text" startIcon={<Refresh />} loading={refreshing} loadingPosition="start" onClick={() => refresh(true)}>
                            Actualizar estado
                        </Button>
                    }
                />
                {configuracion.activa && !estado.procesador_reciente && (
                    <Alert severity="warning" sx={{ mb: 3 }}>
                        El procesador de horarios no registra actividad reciente. Revisa el servicio de programación
                        automática. Las solicitudes manuales se atienden desde la aplicación.
                    </Alert>
                )}
                {estado.procesamiento_manual?.error && <Alert severity="error" sx={{ mb: 2 }}>
                    {estado.procesamiento_manual.error}
                </Alert>}
                {unconfirmed && <Alert severity="warning" sx={{ mb: 2 }}>
                    No se pudo confirmar el estado de la solicitud. Actualiza el estado antes de reintentar para evitar duplicados.
                </Alert>}
                {active && (
                    <Box className="surface" sx={{ mb: 3 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {active.estado === 'pendiente'
                                    ? 'Iniciando…'
                                    : `Sincronizando… ${catalogLabels[active.etapa || ''] || 'catálogos'}`}
                            </Typography>
                            <Chip label={labels[active.estado]} variant="outlined" />
                        </Box>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                            {active.estado === 'pendiente'
                                ? 'Solicitud aceptada; esperando la confirmación del servidor. Puedes seguir usando TDV2.'
                                : 'Puedes salir de esta pantalla; la ejecución continuará en el servidor.'}
                        </Typography>
                        {estado.inicio_demorado && <Alert severity="warning" sx={{ mt: 1 }}>
                            El inicio está demorando más de lo esperado. El servidor volverá a intentar procesar esta misma
                            solicitud; no se creará otra. Revisa el acceso a la cola local si el problema continúa.
                        </Alert>}
                        {active.estado === 'ejecutando' && <LinearProgress sx={{ mt: 2 }} />}
                    </Box>
                )}
                <Box
                    className="settings-grid"
                    sx={{
                        mb: 2,
                    }}
                >
                    {(['sii', 'ilda'] as const).map((source) => (
                        <SettingsSection key={source} icon={<StorageOutlined />}
                            title={source === 'sii' ? 'SII · Unidades responsables y módulos de SIIv2' : 'ILDA · Información de las áreas'}
                            description={source === 'sii'
                                    ? 'Unidades responsables del ejercicio más reciente y módulos de SIIv2 sin filtro de ejercicio ni de UR.'
                                    : 'Inventario completo de ILDA. Los formatos consultan los registros por clave de UR.'}>
                            <Typography sx={{ fontSize: 26, fontWeight: 600 }}>
                                {catalogos[source].registros?.toLocaleString('es-MX') ?? 'No disponible'}{' '}
                                <Typography component="span" variant="body2" color="text.secondary">
                                    {source === 'sii' ? 'unidades' : 'registros'}
                                </Typography>
                            </Typography>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 2 }}>
                                Última copia correcta: {date(catalogos[source].completada_en)}
                            </Typography>
                            <Button
                                variant="outlined"
                                startIcon={<SyncOutlined />}
                                loading={!!actionLabel(source)}
                                loadingPosition="start"
                                disabled={
                                    busy || unconfirmed || !!estado.ejecucion_activa || (source === 'ilda' && !estado.ilda_habilitada)
                                }
                                onClick={() => void execute(source)}
                            >
                                {actionLabel(source) || `Sincronizar ${source.toUpperCase()}`}
                            </Button>
                            {source === 'sii' && <Box sx={{ mt: 2 }}>
                                <Typography variant="body2">Módulos de SIIv2: {catalogos.sii_modulos.registros?.toLocaleString('es-MX') ?? 'No disponible'}</Typography>
                                {catalogos.sii_modulos.error && <Alert severity="error" sx={{ my: 1 }}>{catalogos.sii_modulos.error}</Alert>}
                                <Typography variant="caption" color="text.secondary">Última copia correcta: {date(catalogos.sii_modulos.completada_en)}</Typography>
                            </Box>}
                            {source === 'ilda' && !estado.ilda_habilitada && (
                                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                                    La conexión de ILDA está deshabilitada en el servidor.
                                </Typography>
                            )}
                        </SettingsSection>
                    ))}
                </Box>
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 3 }}>
                    <Button startIcon={<SyncOutlined />}
                        loading={!!actionLabel('ambas')} loadingPosition="start"
                        disabled={busy || unconfirmed || !!estado.ejecucion_activa || !estado.ilda_habilitada}
                        onClick={() => void execute('ambas')}>{actionLabel('ambas') || 'Sincronizar SII e ILDA ahora'}</Button>
                </Box>
                <SettingsSection title="Programación automática" icon={<ScheduleOutlined />}
                    description="Todas las ejecuciones automáticas de SII incluyen unidades responsables y módulos de SIIv2. Incluir ILDA es independiente.">
                    <Box
                        component="form"
                        onSubmit={(e) => {
                            e.preventDefault();
                            void save();
                        }}
                    >
                        <Box
                            sx={{
                                display: 'flex',
                                gap: 2,
                                alignItems: 'center',
                                justifyContent: 'space-between',
                                flexWrap: 'wrap',
                                mb: 2,
                            }}
                        >
                            <FormControlLabel
                                control={
                                    <Switch
                                        checked={draft.activa}
                                        disabled={busy}
                                        onChange={(e) => setDraft({ ...draft, activa: e.target.checked })}
                                    />
                                }
                                label="Activar sincronización automática"
                            />
                        </Box>
                        <Box
                            sx={{
                                display: 'grid',
                                gridTemplateColumns: { xs: '1fr', md: 'repeat(3,minmax(0,1fr))' },
                                gap: 2,
                            }}
                        >
                            <TextField
                                select
                                label="Frecuencia"
                                value={draft.intervalo_minutos}
                                disabled={busy}
                                onChange={(e) => setDraft({ ...draft, intervalo_minutos: Number(e.target.value) })}
                            >
                                {intervals.map(([value, label]) => (
                                    <MenuItem value={value} key={value}>
                                        {label}
                                    </MenuItem>
                                ))}
                            </TextField>
                            <TextField
                                label="Hora local"
                                type="time"
                                value={draft.hora}
                                disabled={busy || draft.intervalo_minutos !== 1440}
                                onChange={(e) => setDraft({ ...draft, hora: e.target.value })}
                                helperText={
                                    draft.intervalo_minutos === 1440
                                        ? 'Para la ejecución diaria.'
                                        : 'Se utiliza con la frecuencia diaria.'
                                }
                                slotProps={{ inputLabel: { shrink: true } }}
                            />
                            <TextField
                                label="Zona horaria"
                                value={draft.zona_horaria}
                                disabled={busy}
                                onChange={(e) => setDraft({ ...draft, zona_horaria: e.target.value })}
                                helperText="America/Ciudad_Juarez"
                            />
                        </Box>
                        <FormControlLabel
                            sx={{ my: 1 }}
                            control={
                                <Checkbox
                                    checked={draft.incluir_ilda}
                                        disabled={busy || (!estado.ilda_habilitada && !draft.incluir_ilda)}
                                    onChange={(e) => setDraft({ ...draft, incluir_ilda: e.target.checked })}
                                />
                            }
                            label="Incluir ILDA en la sincronización automática"
                        />
                        <Box
                            className="settings-section-actions"
                        >
                            <Button type="submit" variant="contained" startIcon={<SaveOutlined />} disabled={busy}>
                                Guardar programación
                            </Button>
                            <Typography variant="body2" color="text.secondary" role="status">{dirty ? 'Cambios pendientes' : ''}</Typography>
                            <Typography variant="body2" color="text.secondary">
                                {configuracion.activa
                                    ? `Próxima ejecución: ${date(estado.proxima_en)}`
                                    : 'La programación está pausada.'}
                            </Typography>
                        </Box>
                    </Box>
                </SettingsSection>
                <Box
                    sx={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        gap: 2,
                        alignItems: 'center',
                        flexWrap: 'wrap',
                        mb: 2,
                        mt: 3,
                    }}
                >
                    <Box>
                        <Typography component="h2" variant="h2">
                            Historial de ejecuciones
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                            Últimas 50 ejecuciones · Horarios de {configuracion.zona_horaria}
                        </Typography>
                    </Box>
                </Box>
                {!historial.length ? <Box className="empty" role="status">Todavía no hay ejecuciones registradas.</Box> :
                <TableContainer className="surface" sx={{ p: '0!important' }} tabIndex={0} role="region" aria-label="Historial de ejecuciones: tabla desplazable">
                    <Table size="small" aria-label="Historial de sincronizaciones" sx={{ minWidth: 700 }}>
                        <TableHead>
                            <TableRow>
                                {['Fecha', 'Origen y fuentes', 'Estado', 'Resultado', 'Solicitado por'].map((h) => (
                                    <TableCell key={h}>{h}</TableCell>
                                ))}
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {historial.map((run) => (
                                <TableRow key={run.id}>
                                    <TableCell sx={{ verticalAlign: 'top' }}>{date(run.solicitada_en)}</TableCell>
                                    <TableCell sx={{ verticalAlign: 'top' }}>
                                        <Typography variant="body2">
                                            {run.fuentes === 'ambas' ? 'SII + ILDA' : run.fuentes.toUpperCase()}
                                        </Typography>
                                        <Typography variant="caption" color="text.secondary">
                                            {run.origen === 'programada'
                                                ? 'Automática'
                                                : run.origen === 'manual'
                                                  ? 'Manual'
                                                  : 'Comando'}
                                        </Typography>
                                    </TableCell>
                                    <TableCell sx={{ verticalAlign: 'top' }}>
                                        <Chip
                                            label={labels[run.estado] || run.estado}
                                            size="small"
                                            variant="outlined"
                                            color={
                                                run.estado === 'completada'
                                                    ? 'success'
                                                    : run.estado === 'fallida'
                                                      ? 'error'
                                                      : run.estado === 'parcial'
                                                        ? 'warning'
                                                        : 'default'
                                            }
                                        />
                                    </TableCell>
                                    <TableCell>
                                        {Object.entries(run.resultado).map(([source, result]) => (
                                            <Typography
                                                key={source}
                                                variant="body2"
                                                color={result.estado === 'fallida' ? 'error.main' : 'text.secondary'}
                                                sx={{ mb: 0.5 }}
                                            >
                                                {catalogLabels[source] || source.toUpperCase()}:{' '}
                                                {result.mensaje ||
                                                    `${result.resumen?.unidades ?? result.resumen?.registros ?? 0} registros`}
                                            </Typography>
                                        ))}
                                        {!Object.keys(run.resultado).length && (
                                            <Typography variant="body2" color="text.secondary">
                                                {run.estado === 'pendiente'
                                                    ? 'Solicitud aceptada; inicio por confirmar'
                                                    : 'Leyendo el origen'}
                                            </Typography>
                                        )}
                                    </TableCell>
                                    <TableCell sx={{ verticalAlign: 'top', overflowWrap: 'anywhere' }}>
                                        {run.solicitado_por}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </TableContainer>}
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>
                    Última actividad del procesador de horarios: {date(estado.procesador_visto_en)}
                </Typography>
                {/* El mismo aviso comunica fallos sin desplazar campos ni perder el borrador. Un error no caduca solo. */}
                <Snackbar open={!!(message || error || statusError)} autoHideDuration={error || statusError ? null : 5000}
                    message={error || statusError ? undefined : message}
                    onClose={(_, reason) => { if (reason !== 'clickaway') { setMessage(''); setError(''); setStatusError(''); } }}>
                    {error || statusError ? <Alert severity="error" variant="outlined" sx={{ bgcolor: 'background.paper', maxWidth: 560 }}
                        onClose={() => { setError(''); setStatusError(''); setMessage(''); }}>
                        {error || statusError}
                        {conflict && <Button sx={{ display: 'flex', mt: 1 }} onClick={() => {
                            router.reload({ only: ['configuracion', 'estado'] }); setError(''); setConflict(false);
                        }}>Cargar programación vigente</Button>}
                    </Alert> : undefined}
                </Snackbar>
            </div>
        </AuthenticatedLayout>
    );
}
