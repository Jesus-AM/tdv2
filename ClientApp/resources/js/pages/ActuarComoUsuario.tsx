import { useRef, useState } from 'react';
import { Head, router, usePage } from '@/lib/navigation';
import axios from 'axios';
import {
    Box,
    Button,
    TextField,
    Typography,
    Alert,
    List,
    ListItemButton,
    ListItemText,
    Checkbox,
    FormControlLabel,
    Chip,
} from '@mui/material';
import { Search, ManageAccountsOutlined } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import AccessModeTabs from '@/Components/AccessModeTabs';
import { errorText } from '@/lib/http';
type Person = {
    usuario_id: number;
    nombre: string;
    email: string;
    id_ur: string | null;
    desc_ur: string | null;
    roles: { nombre: string; clave: string }[];
};
export default function ActuarComoUsuario({
    capacidad,
}: {
    capacidad: {
        permitido: boolean;
        escritura: boolean;
        alcance?: string;
        duracion_minutos?: number;
        no_disponible?: boolean;
    };
}) {
    const { props } = usePage();
    const active = props.representacion || props.simulacion;
    const [query, setQuery] = useState(''),
        [people, setPeople] = useState<Person[]>([]),
        [selected, setSelected] = useState<Person | null>(null),
        [reason, setReason] = useState(''),
        [write, setWrite] = useState(false),
        [busy, setBusy] = useState(false),
        [activating, setActivating] = useState(false),
        [error, setError] = useState(''),
        [searched, setSearched] = useState(false),
        [more, setMore] = useState(false),
        [page, setPage] = useState(1);
    const id = useRef(0);
    async function search(next = false) {
        if (busy) return;
        const token = ++id.current;
        setBusy(true);
        setError('');
        setSelected(null);
        setSearched(false);
        setMore(false);
        try {
            const n = next ? page + 1 : 1;
            const { data } = await axios.get<{ personas: Person[]; hayMas: boolean }>('/actuar-como-usuario/personas', {
                params: { q: query.trim(), pagina: n },
            });
            if (token !== id.current) return;
            setPeople(next ? [...people, ...data.personas] : data.personas);
            setMore(data.hayMas);
            setPage(n);
            setSearched(true);
        } catch (e) {
            setPeople([]);
            setSearched(false);
            setError(errorText(e));
        } finally {
            setBusy(false);
        }
    }
    function start() {
        if (!selected || busy) return;
        setActivating(true);
        setBusy(true);
        setError('');
        router.post(
            '/actuar-como-usuario',
            { email: selected.email, motivo: reason.trim(), escritura: write },
            {
                onError: (errors) => setError(Object.values(errors)[0] || 'No se pudo iniciar.'),
                onFinish: () => { setBusy(false); setActivating(false); },
            },
        );
    }
    return (
        <AuthenticatedLayout>
            <Head title="Actuar como usuario" />
            <div className="page settings-page access-page">
                <PageHeading
                    breadcrumbs={[{ label: 'Configuración', href: '/configuracion' }, { label: 'Pruebas de acceso', href: '/configuracion/pruebas-acceso' }]}
                    title="Actuar como usuario"
                    description="Revisa la plataforma con el acceso autorizado de una persona. Tu cuenta queda identificada en las acciones."
                />
                <AccessModeTabs value="usuario" />
                {active ? (
                    <Typography variant="body2" color="text.secondary">
                        Vuelve a tu usuario desde el aviso superior antes de iniciar otra representación.
                    </Typography>
                ) : !capacidad.permitido ? (
                    <Alert severity="warning">
                        {capacidad.no_disponible
                            ? 'Actuar como usuario no está disponible. Revisa su configuración en Nexo.'
                            : 'Nexo no autorizó esta función para tus roles.'}
                    </Alert>
                ) : (
                    <div className="access-columns">
                        <Box className="surface">
                            <Typography component="h2" variant="h2" sx={{ mb: 2 }}>Selecciona una persona</Typography>
                            <Box
                                className="settings-search"
                                component="form"
                                onSubmit={(e) => {
                                    e.preventDefault();
                                    void search();
                                }}
                            >
                                <TextField
                                    label="Nombre o correo institucional"
                                    fullWidth
                                    value={query}
                                    disabled={busy}
                                    onChange={(e) => {
                                        setQuery(e.target.value);
                                        setSelected(null);
                                        setPeople([]);
                                        setSearched(false);
                                    }}
                                />
                                <Button
                                    type="submit"
                                    variant="contained"
                                    startIcon={<Search />}
                                    disabled={busy || query.trim().length < 2}
                                    loading={busy}
                                >
                                    Buscar
                                </Button>
                            </Box>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.5 }}>
                                {capacidad.alcance === 'rama_ur'
                                    ? 'Se muestran personas de tu área y sus áreas dependientes.'
                                    : 'Se muestran personas con acceso vigente a TDV2.'}{' '}
                                {capacidad.duracion_minutos ? `Duración: hasta ${capacidad.duracion_minutos} minutos.` : 'Nexo confirmará la duración al activar.'}
                            </Typography>
                            <List aria-label="Personas disponibles">
                                {people.map((p) => (
                                    <ListItemButton
                                        role="button"
                                        aria-pressed={selected?.usuario_id === p.usuario_id}
                                        key={p.usuario_id}
                                        disabled={busy}
                                        selected={selected?.usuario_id === p.usuario_id}
                                        sx={{ borderRadius: 2, mb: 1, overflowWrap: 'anywhere' }}
                                        onClick={() => setSelected(p)}
                                    >
                                        <ListItemText
                                            primary={
                                                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                                    {p.nombre}
                                                </Typography>
                                            }
                                            secondary={
                                                <>
                                                    {p.email}
                                                    <br />
                                                    {p.desc_ur || 'Sin UR única'}
                                                    <br />
                                                    {p.roles.map((r) => r.nombre).join(', ')}
                                                </>
                                            }
                                        />
                                    </ListItemButton>
                                ))}
                            </List>
                            {busy && <Typography variant="body2" role="status">{activating ? 'Activando…' : 'Consultando…'}</Typography>}
                            {searched && !people.length && !error && (
                                <Typography variant="body2" role="status">
                                    No hay personas que coincidan dentro del alcance autorizado.
                                </Typography>
                            )}
                            {more && (
                                <Button disabled={busy} onClick={() => void search(true)}>
                                    Mostrar más
                                </Button>
                            )}
                        {selected && (
                            <Box sx={{ borderTop: '1px solid', borderColor: 'divider', mt: 2, pt: 2 }}>
                                <TextField
                                    fullWidth
                                    multiline
                                    minRows={2}
                                    maxRows={4}
                                    label="Motivo de la representación"
                                    value={reason}
                                    onChange={(e) => setReason(e.target.value)}
                                    disabled={busy}
                                    slotProps={{ htmlInput: { maxLength: 300 } }}
                                />
                                {capacidad.escritura && (
                                    <FormControlLabel
                                        control={
                                            <Checkbox
                                                checked={write}
                                                onChange={(e) => setWrite(e.target.checked)}
                                                disabled={busy}
                                            />
                                        }
                                        label={
                                            <Typography variant="body2">
                                                Permitir cambios reales con los permisos de esta persona
                                            </Typography>
                                        }
                                    />
                                )}
                                {write ? (
                                    <Alert severity="warning" sx={{ my: 2 }}>
                                        Los cambios se guardarán en los datos actuales. Quedarán registrados tu usuario
                                        y la persona representada.
                                    </Alert>
                                ) : (
                                    <Typography variant="body2" color="text.secondary" sx={{ my: 2 }}>
                                        La representación será de solo lectura.
                                    </Typography>
                                )}
                                <Box sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 1 }}><Button
                                    variant="contained"
                                    startIcon={<ManageAccountsOutlined />}
                                    sx={{ minHeight: 40 }}
                                    disabled={busy || reason.trim().length < 5}
                                    onClick={start}
                                    loading={busy}
                                >
                                    Activar representación
                                </Button>
                                <Chip label={write ? 'Con escritura' : 'Solo lectura'} /></Box>
                            </Box>
                        )}
                        {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
                        </Box>
                        <Box className="surface" role="status" aria-live="polite">
                            <Typography component="h2" variant="h2" sx={{ mb: 2 }}>Contexto por activar</Typography>
                            {selected ? <Box sx={{ overflowWrap: 'anywhere' }}>
                                <Typography variant="body2" sx={{ fontWeight: 600 }}>{selected.nombre}</Typography>
                                <Typography variant="body2">{selected.email}</Typography>
                                <Typography variant="body2" sx={{ mt: 1 }}>Área: {selected.desc_ur || 'Sin UR única'}</Typography>
                                <Typography variant="body2">Roles disponibles: {selected.roles.map(role => role.nombre).join(', ') || 'Nexo confirmará el acceso'}</Typography>
                                <Typography variant="body2" sx={{ mt: 1 }}>{write ? 'Se solicitará escritura con los permisos de esta persona.' : 'Se solicitará acceso de solo lectura.'}</Typography>
                            </Box> : <Typography variant="body2">Busca y selecciona una persona autorizada para revisar su contexto.</Typography>}
                            <Typography variant="body2" sx={{ mt: 2, overflowWrap: 'anywhere' }}>Cuenta autenticada: {props.auth.user?.email}.</Typography>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>Nexo comprobará de nuevo los permisos al activar. La selección no modifica asignaciones; la franja superior permite volver a tu usuario.</Typography>
                        </Box>
                    </div>
                )}
            </div>
        </AuthenticatedLayout>
    );
}
