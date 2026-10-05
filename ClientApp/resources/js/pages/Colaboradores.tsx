import { useEffect, useRef, useState } from 'react';
import { Head, router, usePage } from '@/lib/navigation';
import axios from 'axios';
import {
    Alert,
    Box,
    Button,
    Chip,
    Typography,
    TextField,
    MenuItem,
    List,
    ListItemButton,
    ListItemText,
    Radio,
    RadioGroup,
    FormControlLabel,
    CircularProgress,
    InputAdornment,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
} from '@mui/material';
import {
    Search,
    PersonAddOutlined,
    ArrowBack,
    CheckCircleOutlined,
    DeleteOutlined,
    PeopleOutlined,
} from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import { errorText } from '@/lib/http';
import { displayUnitCode } from '@/lib/area-directory';
import type { Unit, Collaboration, CollaborationKind, EligiblePerson } from '@/types/tdv2';
export default function Colaboradores({
    unidades,
    tipos,
    colaboraciones,
    avisoDelegacion,
}: {
    unidades: Unit[];
    tipos: CollaborationKind[];
    colaboraciones: Collaboration[];
    avisoDelegacion: string | null;
}) {
    const { props } = usePage();
    const readOnly = !!props.simulacion || !!(props.representacion && !props.representacion.escritura);
    const [root, setRoot] = useState(unidades[0]?.id_ur || ''),
        [query, setQuery] = useState(''),
        [type, setType] = useState<CollaborationKind>(tipos[0] || 'local'),
        [results, setResults] = useState<EligiblePerson[]>([]),
        [selected, setSelected] = useState<EligiblePerson | null>(null),
        [searching, setSearching] = useState(false),
        [searched, setSearched] = useState(false),
        [searchError, setSearchError] = useState(''),
        [busy, setBusy] = useState(false),
        [page, setPage] = useState(1),
        [more, setMore] = useState(false),
        [message, setMessage] = useState(''),
        [kind, setKind] = useState<'error' | 'success' | 'warning'>('success'),
        [removing, setRemoving] = useState<Collaboration | null>(null);
    const id = useRef(0);
    const current = unidades.find((u) => u.id_ur === root);
    const available = tipos.filter((t) => t === 'local' || Number(current?.nivel_ur) === 2);
    useEffect(() => {
        if (!available.includes(type)) setType(available[0] || 'local');
    }, [root, type, available.join(',')]);
    async function search(number = 1) {
        if (readOnly || busy || !root || query.trim().length < 2) return;
        const token = ++id.current;
        setSearching(true);
        setSelected(null);
        setMessage('');
        setSearchError('');
        setSearched(false);
        setResults([]);
        setMore(false);
        try {
            const { data } = await axios.get<{ personas: EligiblePerson[]; hayMas: boolean }>(
                '/colaboradores/personas',
                { params: { ur: root, q: query.trim(), page: number } },
            );
            if (token !== id.current) return;
            setResults(data.personas);
            setPage(number);
            setMore(data.hayMas);
            setSearched(true);
        } catch (e) {
            if (token === id.current) {
                setSearchError(errorText(e));
                setMessage(errorText(e));
                setKind('error');
            }
        } finally {
            if (token === id.current) setSearching(false);
        }
    }
    useEffect(() => {
        id.current++;
        setResults([]);
        setSelected(null);
        setSearched(false);
        setSearchError('');
        setMore(false);
        setPage(1);
        setSearching(false);
        const timer = setTimeout(() => void search(), 400);
        return () => {
            clearTimeout(timer);
            id.current++;
        };
    }, [root, query, readOnly]);
    async function add() {
        if (readOnly || busy || !selected) return;
        setBusy(true);
        try {
            await axios.post('/colaboradores', { ur: root, email: selected.email, id_ur: selected.id_ur, tipo: type });
            setMessage('Colaborador agregado.');
            setKind('success');
            setSelected(null);
            setQuery('');
            router.reload({ only: ['colaboraciones'] });
        } catch (e) {
            setMessage(errorText(e));
            setKind('error');
        } finally {
            setBusy(false);
        }
    }
    async function remove() {
        if (readOnly || busy || !removing) return;
        setBusy(true);
        try {
            const { data, status } = await axios.delete(`/colaboradores/${removing.id}`);
            setMessage(data.message);
            setKind(status === 202 ? 'warning' : 'success');
            setRemoving(null);
            router.reload({ only: ['colaboraciones'] });
        } catch (e) {
            setMessage(errorText(e));
            setKind('error');
        } finally {
            setBusy(false);
        }
    }
    return (
        <AuthenticatedLayout>
            <Head title="Colaboradores" />
            <div className="page" style={{ maxWidth: 1330 }}>
                <Button startIcon={<ArrowBack />} onClick={() => router.visit('/inicio')} sx={{ mb: 2 }}>
                    Procesos operativos
                </Button>
                <PageHeading
                    title="Colaboradores"
                    description="Selecciona una persona y define en qué formatos podrá ayudar."
                />
                {readOnly && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                        Estás consultando esta vista. Las altas y los retiros de colaboradores están desactivados.
                    </Typography>
                )}
                {avisoDelegacion && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                        {avisoDelegacion}
                    </Alert>
                )}
                {message && (
                    <Alert severity={kind} sx={{ mb: 2 }}>
                        {message}
                    </Alert>
                )}
                {unidades.length > 0 && tipos.length > 0 && (
                    <Box className="surface">
                        <Typography variant="h2" component="h2" sx={{ mb: 2 }}>
                            Agregar colaborador
                        </Typography>
                        <div className="split">
                            <TextField
                                select
                                label="Área que autoriza"
                                value={root}
                                disabled={readOnly || busy}
                                onChange={(e) => setRoot(e.target.value)}
                            >
                                {unidades.map((u) => (
                                    <MenuItem value={u.id_ur} key={u.id_ur}>
                                        {displayUnitCode(u.cve_ur)} · {u.desc_ur}
                                    </MenuItem>
                                ))}
                            </TextField>
                            <TextField
                                label="Buscar persona"
                                value={query}
                                disabled={readOnly || busy}
                                onChange={(e) => setQuery(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter') void search();
                                }}
                                slotProps={{
                                    input: {
                                        startAdornment: (
                                            <InputAdornment position="start">
                                                <Search fontSize="small" />
                                            </InputAdornment>
                                        ),
                                        endAdornment: searching && <CircularProgress size={17} />,
                                    },
                                }}
                            />
                        </div>
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1, mb: 3 }}>
                            Busca por nombre o correo entre el personal del área seleccionada y sus áreas dependientes.
                            {Number(current?.nivel_ur) === 3 && ' La colaboración permite llenar únicamente el formato de esta área de nivel 3.'}
                        </Typography>
                        <div className="split">
                            <section>
                                <Typography variant="h3" component="h3" sx={{ mb: 1.5 }}>
                                    1. Selecciona una persona
                                </Typography>
                                {results.length > 0 ? (
                                    <List
                                        sx={{ p: 0, maxHeight: 350, overflow: 'auto' }}
                                        aria-label="Personas disponibles"
                                    >
                                        {results.map((p) => (
                                            <ListItemButton
                                                key={`${p.email}:${p.id_ur}`}
                                                selected={selected?.email === p.email && selected?.id_ur === p.id_ur}
                                                disabled={readOnly || busy}
                                                onClick={() => setSelected(p)}
                                                sx={{
                                                    borderRadius: 2,
                                                    mb: 0.5,
                                                    border: '1px solid',
                                                    borderColor:
                                                        selected?.email === p.email ? '#bfd1ed' : 'transparent',
                                                }}
                                            >
                                                <ListItemText
                                                    primary={
                                                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                                            {p.nombre}
                                                        </Typography>
                                                    }
                                                    secondary={
                                                        <>
                                                            <span
                                                                style={{ display: 'block', overflowWrap: 'anywhere' }}
                                                            >
                                                                {p.email}
                                                            </span>
                                                            <span style={{ display: 'block', marginTop: 4 }}>
                                                                {p.adscripcion}
                                                            </span>
                                                        </>
                                                    }
                                                />
                                                {selected?.email === p.email && (
                                                    <CheckCircleOutlined fontSize="small" color="primary" />
                                                )}
                                            </ListItemButton>
                                        ))}
                                    </List>
                                ) : (
                                    <div className="empty">
                                        <PeopleOutlined sx={{ mb: 1 }} />
                                        <Typography variant="body2">
                                            {searchError || (searching
                                                ? 'Buscando personas…'
                                                : searched
                                                  ? 'No encontramos personas disponibles.'
                                                  : 'Escribe al menos dos caracteres para buscar.')}
                                        </Typography>
                                    </div>
                                )}
                                {(more || page > 1) && (
                                    <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 1 }}>
                                        <Button
                                            disabled={page === 1 || searching}
                                            onClick={() => void search(page - 1)}
                                        >
                                            Anterior
                                        </Button>
                                        <Typography variant="caption">Página {page}</Typography>
                                        <Button disabled={!more || searching} onClick={() => void search(page + 1)}>
                                            Siguiente
                                        </Button>
                                    </Box>
                                )}
                            </section>
                            <section>
                                <Typography variant="h3" component="h3" sx={{ mb: 1.5 }}>
                                    2. Define el alcance
                                </Typography>
                                {selected ? (
                                    <>
                                        <Typography variant="caption" color="text.secondary">
                                            Acceso para
                                        </Typography>
                                        <Typography variant="body2" sx={{ fontWeight: 600, mb: 1.5 }}>
                                            {selected.nombre}
                                        </Typography>
                                        <RadioGroup
                                            value={type}
                                            onChange={(e) => setType(e.target.value as CollaborationKind)}
                                        >
                                            {available.map((t) => (
                                                <Box
                                                    key={t}
                                                    sx={{
                                                        border: '1px solid',
                                                        borderColor: type === t ? 'primary.main' : 'divider',
                                                        background: type === t ? '#f3f7fe' : '#fff',
                                                        borderRadius: 2,
                                                        mb: 1,
                                                        px: 1.5,
                                                        py: 0.5,
                                                    }}
                                                >
                                                    <FormControlLabel
                                                        disabled={readOnly || busy}
                                                        value={t}
                                                        control={<Radio size="small" />}
                                                        label={
                                                            <Box>
                                                                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                                                    {t === 'local'
                                                                        ? 'Colaborador local'
                                                                        : 'Colaborador de áreas dependientes'}
                                                                </Typography>
                                                                <Typography variant="caption" color="text.secondary">
                                                                    {t === 'local'
                                                                        ? 'Solo el formato de su área.'
                                                                        : 'El área que autoriza y sus áreas dependientes.'}
                                                                </Typography>
                                                            </Box>
                                                        }
                                                    />
                                                </Box>
                                            ))}
                                        </RadioGroup>
                                        <Typography variant="body2" color="text.secondary" sx={{ my: 1.5 }}>
                                            {type === 'local'
                                                ? `Podrá llenar únicamente el formato de ${selected.formato}.`
                                                : `Podrá ayudar a llenar el formato de ${current?.desc_ur} y los de sus áreas dependientes.`}
                                        </Typography>
                                        <Button
                                            variant="contained"
                                            startIcon={<PersonAddOutlined />}
                                            disabled={readOnly || busy}
                                            onClick={() => void add()}
                                        >
                                            {busy ? 'Guardando…' : 'Agregar colaborador'}
                                        </Button>
                                    </>
                                ) : (
                                    <div className="empty">
                                        <Typography variant="body2">
                                            Elige una persona para revisar su alcance antes de agregarla.
                                        </Typography>
                                    </div>
                                )}
                            </section>
                        </div>
                    </Box>
                )}
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mt: 4, mb: 2 }}>
                    <Typography component="h2" variant="h2">
                        Colaboradores asignados
                    </Typography>
                    <Chip label={colaboraciones.length} />
                </Box>
                {colaboraciones.length ? (
                    <Box className="surface" sx={{ p: '0!important' }}>
                        {colaboraciones.map((c) => (
                            <Box
                                key={c.id}
                                sx={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    flexWrap: 'wrap',
                                    gap: 2,
                                    p: 2,
                                    borderBottom: '1px solid',
                                    borderColor: 'divider',
                                }}
                            >
                                <Box sx={{ flex: 1, minWidth: 200 }}>
                                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                        {c.nombre}
                                    </Typography>
                                    <Typography
                                        variant="caption"
                                        color="text.secondary"
                                        sx={{ overflowWrap: 'anywhere' }}
                                    >
                                        {c.email}
                                    </Typography>
                                </Box>
                                <Box sx={{ flex: 1, minWidth: 180 }}>
                                    <Typography variant="body2">{c.alcance}</Typography>
                                    <Chip
                                        label={c.tipo === 'local' ? 'Local' : 'Áreas dependientes'}
                                        variant="outlined"
                                        sx={{ mt: 0.5 }}
                                    />
                                    {c.revocada && <Chip label="Revocada" color="warning" sx={{ ml: 1 }} />}
                                    {c.pendiente && (
                                        <Typography variant="caption" color="warning.main" sx={{ display: 'block' }}>
                                            Retiro pendiente de confirmar en Nexo
                                        </Typography>
                                    )}
                                </Box>
                                <Button
                                    color="error"
                                    startIcon={<DeleteOutlined />}
                                    disabled={readOnly || busy}
                                    onClick={() => setRemoving(c)}
                                    aria-label={`Retirar a ${c.nombre}`}
                                >
                                    {c.pendiente ? 'Reintentar retiro' : 'Retirar'}
                                </Button>
                            </Box>
                        ))}
                    </Box>
                ) : (
                    <div className="empty">
                        <Typography variant="body2">Aún no hay colaboradores asignados.</Typography>
                    </div>
                )}
                <Typography variant="caption" color="text.secondary" sx={{ mt: 2, display: 'block' }}>
                    El acceso depende de que la autorización en Nexo y la adscripción al área sigan vigentes.
                </Typography>
            </div>
            <Dialog open={!!removing} onClose={() => !busy && setRemoving(null)}>
                <DialogTitle>Retirar colaboración</DialogTitle>
                <DialogContent>
                    <Typography variant="body2">
                        ¿Retirar la colaboración de {removing?.nombre} en {removing?.alcance}?
                    </Typography>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setRemoving(null)} disabled={busy}>
                        Cancelar
                    </Button>
                    <Button color="error" variant="contained" disabled={busy} onClick={() => void remove()}>
                        Retirar colaboración
                    </Button>
                </DialogActions>
            </Dialog>
        </AuthenticatedLayout>
    );
}
