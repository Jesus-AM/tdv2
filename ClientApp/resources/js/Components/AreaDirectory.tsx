import { countLabel } from '@/lib/form-labels';
import { useMemo, useState, useEffect, useLayoutEffect, useRef } from 'react';
import { router, usePage } from '@/lib/navigation';
import {
    Box,
    TextField,
    MenuItem,
    Button,
    Typography,
    Chip,
    LinearProgress,
    Pagination,
    Collapse,
    InputAdornment,
    IconButton,
    Tabs,
    Tab,
    useMediaQuery,
} from '@mui/material';
import { Search, ExpandMore, AccountTreeOutlined, SubdirectoryArrowRight, Clear } from '@mui/icons-material';
import { buildAreaDirectory, filterAreaTree, expandableAreaIds, displayUnitCode, averageProgress } from '@/lib/area-directory';
import type { AreaFilter, AreaNode } from '@/lib/area-directory';
import { defaultParticipation, type Participation } from '@/lib/area-directory';
import type { Unit, FormRow } from '@/types/tdv2';
// Sólo preferencias de vista en memoria; nunca permisos, respuestas o datos de página.
type DirectoryView = { q: string; scope: 'todas' | 'mis'; filter: AreaFilter; page: number; open: string[]; scroll: number };
let remembered: { context: string; view: DirectoryView } | undefined;
router.on('identity-changing', () => { remembered = undefined; });
export default function AreaDirectory({
    units,
    forms,
    readOnly,
    ownRoot,
    participation = defaultParticipation,
}: {
    units: Unit[];
    forms: FormRow[];
    readOnly: boolean;
    ownRoot: string | null;
    participation?: Participation;
}) {
    const { props } = usePage();
    const context = `${props.auth.user?.email || ''}:${props.contextoEdicion || 'own'}`;
    const [initial] = useState(() => remembered?.context === context ? remembered.view : undefined);
    const [q, setQ] = useState(initial?.q || ''),
        [scope, setScope] = useState<'todas' | 'mis'>(initial?.scope || 'todas'),
        [filter, setFilter] = useState<AreaFilter>(initial?.filter || 'todos'),
        [page, setPage] = useState(initial?.page || 1),
        [open, setOpen] = useState(new Set<string>(initial?.open));
    const view = useRef<DirectoryView>({ q, scope, filter, page, open: [...open], scroll: initial?.scroll || 0 });
    view.current = { ...view.current, q, scope, filter, page, open: [...open] };
    useLayoutEffect(() => { if (initial) window.scrollTo({ top: initial.scroll, behavior: 'instant' }); }, [initial]);
    useEffect(() => {
        const remember = () => { view.current.scroll = window.scrollY; remembered = { context, view: { ...view.current } }; };
        window.addEventListener('scroll', remember, { passive: true });
        const off = router.on('before', remember);
        return () => { off(); window.removeEventListener('scroll', remember); };
    }, [context]);
    const reducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)');
    const visibleForms = useMemo(() => scope === 'mis' ? forms.filter((f) => f.propia ?? f.editable) : forms, [scope, forms]);
    const tree = useMemo(() => {
        const directory = units.length ? units : forms;
        if (scope === 'todas') return buildAreaDirectory(directory, forms, participation);
        const own = new Set(visibleForms.map((f) => f.id_ur));
        // Conserva todos los ascendientes participantes como contexto, también al habilitar más niveles.
        const byId = new Map(directory.map(u => [u.id_ur, u]));
        for (const form of visibleForms) {
            const seen = new Set([form.id_ur]);
            let parent = form.id_ur_principal ?? form.id_ur_pertenece;
            while (parent && !seen.has(parent)) {
                seen.add(parent); own.add(parent);
                const ancestor = byId.get(parent);
                parent = ancestor?.id_ur_principal ?? ancestor?.id_ur_pertenece ?? null;
            }
        }
        return buildAreaDirectory(directory.filter((u) => own.has(u.id_ur)), visibleForms, participation);
    }, [units, forms, visibleForms, scope, participation]);
    const roots = useMemo(() => filterAreaTree(tree.roots, q, filter), [tree, q, filter]),
        others = useMemo(() => filterAreaTree(tree.others, q, filter), [tree, q, filter]);
    const criteria = `${scope}:${filter}:${q}`, previousCriteria = useRef(criteria);
    useEffect(() => {
        if (criteria === previousCriteria.current) return;
        previousCriteria.current = criteria; setPage(1);
        setOpen(q || filter !== 'todos' ? new Set(expandableAreaIds([...roots, ...others])) : new Set());
    }, [criteria, q, filter, roots, others]);
    useEffect(() => { setPage(p => Math.min(p, Math.max(1, Math.ceil(roots.length / 8)))); }, [roots.length]);
    const pageRoots = roots.slice((page - 1) * 8, page * 8);
    const toggle = (id: string) =>
        setOpen((prev) => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    const formRow = (node: AreaNode, main = false) =>
        node.form &&
        !node.contextOnly && (
            <Box
                key={node.unit.id_ur}
                className="area-format-row"
                sx={{
                    display: 'grid',
                    gridTemplateColumns: { xs: 'minmax(0, 1fr) auto', md: '18px minmax(0, 1fr) 120px 100px 116px' },
                    gap: 1,
                    alignItems: 'center',
                    minHeight: 60,
                    py: .75,
                    borderBottom: '1px solid',
                    borderColor: 'divider',
                    '&:last-child': { borderBottom: 0 },
                }}
            >
                <SubdirectoryArrowRight sx={{ fontSize: 18, color: 'text.secondary', display: { xs: 'none', md: 'block' } }} />
                <Box sx={{ minWidth: 0, overflowWrap: 'anywhere' }}>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {main ? 'Formato del área principal' : node.unit.desc_ur}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                        {displayUnitCode(node.unit.cve_ur)} ·{' '}
                        {node.form.enviado_en ? 'Enviado' : node.form.porcentaje === 100
                            ? 'Captura completa'
                            : node.form.porcentaje
                              ? 'En captura'
                              : 'Sin iniciar'}
                    </Typography>
                </Box>
                <Chip label={node.form.editable && !readOnly ? 'Puedes llenar' : 'Solo consulta'} variant="outlined" sx={{ justifySelf: 'start', gridColumn: { xs: 1, md: 3 }, gridRow: { xs: 2, md: 1 } }} />
                <Box sx={{ width: { xs: 85, md: 100 }, textAlign: 'right', justifySelf: 'end', gridColumn: { xs: 2, md: 4 }, gridRow: 1 }}>
                    <Typography variant="caption" color="secondary" sx={{ fontWeight: 600 }}>
                        {node.form.porcentaje}%
                    </Typography>
                    <LinearProgress
                        variant="determinate"
                        value={Number(node.form.porcentaje)}
                        color="secondary"
                        sx={{ height: 4, borderRadius: 2, mt: 0.5 }}
                    />
                </Box>
                <Button
                    variant={node.form.editable && !readOnly ? 'contained' : 'outlined'}
                    aria-label={`Abrir formato de ${node.unit.desc_ur}`}
                    onClick={() => router.visit(node.form!.url)}
                    sx={{ gridColumn: { xs: 2, md: 5 }, gridRow: { xs: 2, md: 1 }, justifySelf: 'end', width: 116 }}
                >
                    Ver formato
                </Button>
            </Box>
        );
    const children = (nodes: AreaNode[]): React.ReactNode =>
        nodes.map((n) => (
            <Box key={n.unit.id_ur}>
                {formRow(n)}
                {!!n.children.length && <Box sx={{ pl: { xs: 1, sm: 2 }, borderLeft: '1px solid', borderColor: 'divider' }}>{children(n.children)}</Box>}
            </Box>
        ));
    return (
        <section aria-label="Directorio de áreas">
            <Tabs value={scope} onChange={(_, value) => setScope(value)} aria-label="Ámbito de consulta"
                className="area-scope-tabs"
                sx={{ mb: 2, minHeight: 40, '& .MuiTab-root': { minHeight: 40, py: 1 }, '& .Mui-selected': { bgcolor: '#edf3fd', fontWeight: 700 } }}>
                <Tab value="mis" label="Mis áreas" />
                <Tab value="todas" label="Todas las áreas" />
            </Tabs>
            <Box className="area-toolbar" sx={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 1, mb: 1.5 }}>
                <TextField
                    label="Buscar área o clave"
                    value={q}
                    onChange={(e) => setQ(e.target.value)}
                    sx={{ flex: '1 1 260px', minWidth: 0 }}
                    slotProps={{
                        input: {
                            startAdornment: (
                                <InputAdornment position="start">
                                    <Search fontSize="small" />
                                </InputAdornment>
                            ),
                            endAdornment: q && (
                                <IconButton aria-label="Limpiar búsqueda" onClick={() => setQ('')}>
                                    <Clear fontSize="small" />
                                </IconButton>
                            ),
                        },
                    }}
                />
                <TextField
                    select
                    label="Mostrar"
                    value={filter}
                    onChange={(e) => setFilter(e.target.value as AreaFilter)}
                    sx={{ width: { xs: '100%', sm: 200 }, flexShrink: 0 }}
                >
                    {[
                        ['todos', 'Todas las áreas'],
                        ['edicion', 'Puedo llenar'],
                        ['sin_iniciar', 'Sin iniciar'],
                        ['en_proceso', 'En captura'],
                        ['completos', 'Completos'],
                    ].map(([v, l]) => (
                        <MenuItem key={v} value={v}>
                            {l}
                        </MenuItem>
                    ))}
                </TextField>
                <Box sx={{ display: 'flex', gap: .5, flexWrap: 'wrap' }}>
                    <Button disabled={!roots.length && !others.length}
                        onClick={() => setOpen(new Set(expandableAreaIds([...pageRoots, ...others])))}>Expandir visibles</Button>
                    <Button disabled={!open.size} onClick={() => setOpen(new Set())}>Contraer</Button>
                </Box>
            </Box>
            <Box className="stack" sx={{ gap: '8px' }}>
                {pageRoots.map((node) => {
                    const avg = averageProgress(node.forms);
                    return (
                        <Box
                            key={node.unit.id_ur}
                            sx={{
                                border: '1px solid',
                                borderColor: open.has(node.unit.id_ur) ? '#bfd0eb' : 'divider',
                                borderRadius: '12px',
                                background: '#fff',
                                overflow: 'hidden',
                            }}
                        >
                            <Box
                                component="button"
                                className="area-root-trigger"
                                aria-label={`${displayUnitCode(node.unit.cve_ur)} · ${node.unit.desc_ur}`}
                                aria-describedby={`area-summary-${node.unit.id_ur} area-progress-${node.unit.id_ur}`}
                                aria-expanded={open.has(node.unit.id_ur)}
                                aria-controls={`area-${node.unit.id_ur}`}
                                onClick={() => toggle(node.unit.id_ur)}
                                sx={{
                                    width: '100%',
                                    background: open.has(node.unit.id_ur) ? '#f8faff' : '#fff',
                                    border: 0,
                                    display: 'flex',
                                    alignItems: 'center',
                                    textAlign: 'left',
                                    minHeight: 64,
                                    py: 1,
                                    px: { xs: 1.5, sm: 2 },
                                    gap: 1.5,
                                    cursor: 'pointer',
                                    color: 'text.primary',
                                    '&:hover': { background: '#f5f8fd' },
                                }}
                            >
                                <Box
                                    sx={{
                                        width: 34,
                                        height: 34,
                                        flexShrink: 0,
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        bgcolor: '#edf3fd',
                                        color: 'primary.main',
                                        borderRadius: 2,
                                        display: { xs: 'none', sm: 'flex' },
                                    }}
                                >
                                    <AccountTreeOutlined fontSize="small" />
                                </Box>
                                <Box sx={{ flex: 1, minWidth: 0, overflowWrap: 'anywhere' }}>
                                    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                                        <Typography variant="body2" sx={{ fontWeight: 600, fontSize: 15 }}>
                                            {node.unit.desc_ur}
                                        </Typography>
                                        {ownRoot === node.unit.id_ur && (
                                            <Chip
                                                label="Tu área"
                                                color="primary"
                                                variant="outlined"
                                                sx={{ height: 20, fontSize: 11 }}
                                            />
                                        )}
                                    </Box>
                                    <Typography id={`area-summary-${node.unit.id_ur}`} variant="caption" color="text.secondary">
                                        {displayUnitCode(node.unit.cve_ur)} ·{' '}
                                        {countLabel(node.areaCount - 1, 'área dependiente', 'áreas dependientes')} ·{' '}
                                        {node.forms.filter((f) => Number(f.porcentaje) === 100).length}/
                                        {node.forms.length} completos
                                    </Typography>
                                </Box>
                                <Box id={`area-progress-${node.unit.id_ur}`} sx={{ width: { xs: 58, sm: 115 }, flexShrink: 0, textAlign: 'right' }}>
                                    <Typography color="secondary" sx={{ fontWeight: 600, fontSize: 17 }}>
                                        {avg}%
                                    </Typography>
                                    <LinearProgress
                                        variant="determinate"
                                        value={avg}
                                        color="secondary"
                                        sx={{ height: 4, borderRadius: 2, my: 0.5 }}
                                    />
                                    <Typography
                                        variant="caption"
                                        sx={{ fontSize: 10, display: { xs: 'none', sm: 'block' } }}
                                        color="text.secondary"
                                    >
                                        Avance del conjunto
                                    </Typography>
                                </Box>
                                <ExpandMore
                                    sx={{
                                        transform: open.has(node.unit.id_ur) ? 'rotate(180deg)' : 'none',
                                        transition: reducedMotion ? 'none' : 'transform 180ms ease',
                                        fontSize: 20,
                                    }}
                                />
                            </Box>
                            <Collapse in={open.has(node.unit.id_ur)} timeout={reducedMotion ? 0 : 180}>
                                <Box
                                    id={`area-${node.unit.id_ur}`}
                                    sx={{ px: { xs: 1.5, sm: 2 }, borderTop: '1px solid', borderColor: 'divider' }}
                                >
                                    {formRow(node, true)}
                                    {!!node.children.length && (
                                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1, mb: 0, fontWeight: 600 }}>
                                            Áreas dependientes
                                        </Typography>
                                    )}
                                    {children(node.children)}
                                </Box>
                            </Collapse>
                        </Box>
                    );
                })}
            </Box>
            {others.length > 0 && (
                <Box className="surface" sx={{ mt: 1.5, p: '12px 16px' }}>
                    <Typography variant="h2">Otras áreas del catálogo</Typography>
                    {children(others)}
                </Box>
            )}
            {!roots.length && !others.length && (
                <div className="empty">
                    <Typography>{scope === 'mis' && !visibleForms.length ? 'No tienes áreas asignadas para este contexto.' : 'No encontramos áreas con esos filtros'}</Typography>
                    <Button
                        onClick={() => {
                            setQ('');
                            setFilter('todos');
                        }}
                    >
                        Limpiar filtros
                    </Button>
                </div>
            )}
            {roots.length > 8 && (
                <Pagination
                    count={Math.ceil(roots.length / 8)}
                    page={page}
                    onChange={(_, p) => setPage(p)}
                    color="primary"
                    sx={{ mt: 2, display: 'flex', justifyContent: 'center' }}
                />
            )}
        </section>
    );
}
