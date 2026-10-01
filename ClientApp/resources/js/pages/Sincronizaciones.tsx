import { useEffect, useState } from 'react';
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
import { ArrowBack, Refresh, SaveOutlined, StorageOutlined, SyncOutlined, ScheduleOutlined } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
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
};
type Catalog = { registros: number; completada_en: string | null };
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
    catalogos: { sii: Catalog; ilda: Catalog };
    historial: Run[];
}) {
    const [draft, setDraft] = useState(configuracion),
        [busy, setBusy] = useState(false),
        [error, setError] = useState(''),
        [message, setMessage] = useState(''),
        [conflict, setConflict] = useState(false);
    useEffect(() => {
        setDraft(configuracion);
    }, [configuracion.version]);
    useEffect(() => {
        const timer = setInterval(
            () => {
                if (document.visibilityState === 'visible')
                    router.reload({ only: ['estado', 'historial', 'catalogos'] });
            },
            estado.ejecucion_activa ? 5000 : 15000,
        );
        return () => clearInterval(timer);
    }, [estado.ejecucion_activa]);
    const date = (value: string | null) =>
        value
            ? new Intl.DateTimeFormat('es-MX', {
                  timeZone: configuracion.zona_horaria,
                  dateStyle: 'short',
                  timeStyle: 'short',
              }).format(new Date(value))
            : 'Sin registro';
    async function execute(fuentes: 'sii' | 'ilda' | 'ambas') {
        if (busy || estado.ejecucion_activa) return;
        setBusy(true);
        setError('');
        setConflict(false);
        try {
            const response = await axios.post('/configuracion/sincronizaciones/ejecutar', { fuentes });
            setMessage(response.data.message);
            router.reload({ only: ['estado', 'historial', 'catalogos'] });
        } catch (e) {
            setError(errorText(e));
            router.reload({ only: ['estado', 'historial', 'catalogos'] });
        } finally {
            setBusy(false);
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
    return (
        <AuthenticatedLayout>
            <Head title="Sincronizaciones" />
            <div className="page">
                <Button startIcon={<ArrowBack />} sx={{ mb: 2 }} onClick={() => router.visit('/configuracion')}>
                    Configuración
                </Button>
                <PageHeading
                    title="Sincronizaciones"
                    description="Mantén actualizadas las áreas y el inventario que utilizan los formatos."
                    actions={
                        <Button variant="outlined" startIcon={<Refresh />} onClick={() => router.reload()}>
                            Actualizar estado
                        </Button>
                    }
                />
                {error && (
                    <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError('')}>
                        {error}
                        {conflict && (
                            <Button
                                sx={{ ml: 1 }}
                                onClick={() => {
                                    router.reload({ only: ['configuracion', 'estado'] });
                                    setError('');
                                    setConflict(false);
                                }}
                            >
                                Cargar programación vigente
                            </Button>
                        )}
                    </Alert>
                )}
                {(configuracion.activa || estado.ejecucion_activa) && !estado.procesador_reciente && (
                    <Alert severity="warning" sx={{ mb: 3 }}>
                        El procesador no registra actividad reciente. Las solicitudes quedarán pendientes hasta que el
                        servicio de programación esté ejecutándose en el servidor.
                    </Alert>
                )}
                {active && (
                    <Box className="surface" sx={{ mb: 3 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {active.estado === 'pendiente'
                                    ? 'Sincronización en cola'
                                    : `Sincronizando ${active.etapa?.toUpperCase() || 'catálogos'}`}
                            </Typography>
                            <Chip label={labels[active.estado]} variant="outlined" />
                        </Box>
                        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                            {active.estado === 'pendiente'
                                ? 'El procesador revisa las solicitudes cada minuto. Puedes seguir usando TDV2.'
                                : 'Puedes salir de esta pantalla; la ejecución continuará en el servidor.'}
                        </Typography>
                        {active.estado === 'ejecutando' && <LinearProgress sx={{ mt: 2 }} />}
                    </Box>
                )}
                <Box
                    sx={{
                        display: 'grid',
                        gridTemplateColumns: { xs: '1fr', md: 'repeat(2,minmax(0,1fr))' },
                        gap: 3,
                        mb: 3,
                    }}
                >
                    {(['sii', 'ilda'] as const).map((source) => (
                        <Box className="surface" key={source}>
                            <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center', mb: 1 }}>
                                <StorageOutlined color="primary" />
                                <Typography component="h2" variant="h2">
                                    {source === 'sii'
                                        ? 'SII · Unidades responsables'
                                        : 'ILDA · Información de las áreas'}
                                </Typography>
                            </Box>
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                                {source === 'sii'
                                    ? 'Catálogo completo de UR del ejercicio más reciente, con jerarquías y encargados.'
                                    : 'Inventario completo de ILDA. Los formatos consultan los registros por clave de UR.'}
                            </Typography>
                            <Typography sx={{ fontSize: 30, fontWeight: 600 }}>
                                {catalogos[source].registros.toLocaleString('es-MX')}{' '}
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
                                disabled={
                                    busy || !!estado.ejecucion_activa || (source === 'ilda' && !estado.ilda_habilitada)
                                }
                                onClick={() => void execute(source)}
                            >
                                Sincronizar {source.toUpperCase()}
                            </Button>
                            {source === 'ilda' && !estado.ilda_habilitada && (
                                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
                                    La conexión de ILDA está deshabilitada en el servidor.
                                </Typography>
                            )}
                        </Box>
                    ))}
                </Box>
                <Box className="surface" sx={{ mb: 3 }}>
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
                            <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center' }}>
                                <ScheduleOutlined color="primary" />
                                <Typography component="h2" variant="h2">
                                    Programación automática
                                </Typography>
                            </Box>
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
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                            SII se incluye en todas las ejecuciones automáticas. Puedes incorporar ILDA cuando también
                            necesites mantener actualizado su inventario.
                        </Typography>
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
                            sx={{
                                display: 'flex',
                                gap: 2,
                                alignItems: 'center',
                                justifyContent: 'space-between',
                                flexWrap: 'wrap',
                                mt: 1,
                            }}
                        >
                            <Button type="submit" variant="contained" startIcon={<SaveOutlined />} disabled={busy}>
                                Guardar programación
                            </Button>
                            <Typography variant="body2" color="text.secondary">
                                {configuracion.activa
                                    ? `Próxima ejecución: ${date(estado.proxima_en)}`
                                    : 'La programación está pausada.'}
                            </Typography>
                        </Box>
                    </Box>
                </Box>
                <Box
                    sx={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        gap: 2,
                        alignItems: 'center',
                        flexWrap: 'wrap',
                        mb: 2,
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
                    <Button
                        startIcon={<SyncOutlined />}
                        disabled={busy || !!estado.ejecucion_activa || !estado.ilda_habilitada}
                        onClick={() => void execute('ambas')}
                    >
                        Sincronizar SII e ILDA ahora
                    </Button>
                </Box>
                <TableContainer className="surface" sx={{ p: '0!important' }}>
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
                                                {source.toUpperCase()}:{' '}
                                                {result.mensaje ||
                                                    `${result.resumen?.unidades ?? result.resumen?.registros ?? 0} registros`}
                                            </Typography>
                                        ))}
                                        {!Object.keys(run.resultado).length && (
                                            <Typography variant="body2" color="text.secondary">
                                                {run.estado === 'pendiente'
                                                    ? 'En espera del procesador'
                                                    : 'Leyendo el origen'}
                                            </Typography>
                                        )}
                                    </TableCell>
                                    <TableCell sx={{ verticalAlign: 'top', overflowWrap: 'anywhere' }}>
                                        {run.solicitado_por}
                                    </TableCell>
                                </TableRow>
                            ))}
                            {!historial.length && (
                                <TableRow>
                                    <TableCell colSpan={5} sx={{ py: 4, textAlign: 'center', color: 'text.secondary' }}>
                                        Todavía no hay ejecuciones registradas.
                                    </TableCell>
                                </TableRow>
                            )}
                        </TableBody>
                    </Table>
                </TableContainer>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>
                    Última actividad del procesador: {date(estado.procesador_visto_en)}
                </Typography>
                <Snackbar open={!!message} autoHideDuration={5000} message={message} onClose={() => setMessage('')} />
            </div>
        </AuthenticatedLayout>
    );
}
