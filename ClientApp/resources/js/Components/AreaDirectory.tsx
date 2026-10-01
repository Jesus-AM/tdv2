import { useMemo, useState, useEffect } from 'react';
import { router } from '@/lib/navigation';
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
} from '@mui/material';
import { Search, ExpandMore, AccountTreeOutlined, SubdirectoryArrowRight, Clear } from '@mui/icons-material';
import { buildAreaDirectory, filterAreaTree, expandableAreaIds } from '@/lib/area-directory';
import type { AreaFilter, AreaNode } from '@/lib/area-directory';
import type { Unit, FormRow } from '@/types/tdv2';
export default function AreaDirectory({
    units,
    forms,
    readOnly,
    ownRoot,
}: {
    units: Unit[];
    forms: FormRow[];
    readOnly: boolean;
    ownRoot: string | null;
}) {
    const [q, setQ] = useState(''),
        [filter, setFilter] = useState<AreaFilter>('todos'),
        [page, setPage] = useState(1),
        [open, setOpen] = useState(new Set<string>());
    const tree = useMemo(() => buildAreaDirectory(units.length ? units : forms, forms), [units, forms]);
    const roots = useMemo(() => filterAreaTree(tree.roots, q, filter), [tree, q, filter]),
        others = useMemo(() => filterAreaTree(tree.others, q, filter), [tree, q, filter]);
    useEffect(() => {
        setPage(1);
        setOpen(q || filter !== 'todos' ? new Set(expandableAreaIds([...roots, ...others])) : new Set());
    }, [q, filter, roots, others]);
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
                sx={{
                    display: 'flex',
                    gap: 2,
                    alignItems: 'center',
                    py: 1.75,
                    borderBottom: '1px solid',
                    borderColor: 'divider',
                    flexWrap: { xs: 'wrap', md: 'nowrap' },
                }}
            >
                <SubdirectoryArrowRight sx={{ fontSize: 18, color: 'text.secondary' }} />
                <Box sx={{ flex: 1, minWidth: 150 }}>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {main ? 'Formato del área principal' : node.unit.desc_ur}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                        {node.unit.cve_ur} ·{' '}
                        {node.form.porcentaje === 100
                            ? 'Completo'
                            : node.form.porcentaje
                              ? 'En captura'
                              : 'Sin iniciar'}
                    </Typography>
                </Box>
                <Box sx={{ width: 85 }}>
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
                >
                    {node.form.editable && !readOnly ? 'Continuar' : 'Ver formato'}
                </Button>
            </Box>
        );
    const children = (nodes: AreaNode[]): React.ReactNode =>
        nodes.map((n) => (
            <Box key={n.unit.id_ur}>
                {formRow(n)}
                {children(n.children)}
            </Box>
        ));
    return (
        <section aria-labelledby="directory-title">
            <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, mb: 2, flexWrap: 'wrap' }}>
                <Box>
                    <Typography id="directory-title" component="h2" variant="h2">
                        Áreas de la universidad
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                        {tree.roots.length} áreas principales ·{' '}
                        {[...tree.roots, ...tree.others].reduce((s, n) => s + n.areaCount, 0)} áreas con formato
                    </Typography>
                </Box>
                <Typography variant="caption" color="text.secondary" sx={{ alignSelf: 'center' }}>
                    Abre un área para consultar sus áreas dependientes
                </Typography>
            </Box>
            <Box className="surface" sx={{ p: '14px!important', display: 'flex', gap: 2, mb: 1, flexWrap: 'wrap' }}>
                <TextField
                    label="Buscar área o clave"
                    value={q}
                    onChange={(e) => setQ(e.target.value)}
                    sx={{ flex: 1, minWidth: 180 }}
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
                    sx={{ minWidth: 180 }}
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
            </Box>
            <Box
                sx={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    mb: 1,
                    gap: 1,
                    flexWrap: 'wrap',
                }}
            >
                <Typography variant="caption" color="text.secondary">
                    {roots.length} áreas principales{q || filter !== 'todos' ? ' con coincidencias' : ''}
                </Typography>
                <Box>
                    <Button
                        disabled={!roots.length && !others.length}
                        onClick={() => setOpen(new Set(expandableAreaIds([...pageRoots, ...others])))}
                    >
                        Expandir visibles
                    </Button>
                    <Button disabled={!open.size} onClick={() => setOpen(new Set())}>
                        Contraer
                    </Button>
                </Box>
            </Box>
            <Box className="stack" sx={{ gap: '10px!important' }}>
                {pageRoots.map((node) => {
                    const avg = node.forms.length
                        ? Math.round(node.forms.reduce((s, f) => s + Number(f.porcentaje), 0) / node.forms.length)
                        : 0;
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
                                    p: 2.25,
                                    gap: 2,
                                    cursor: 'pointer',
                                    color: 'text.primary',
                                    '&:hover': { background: '#f5f8fd' },
                                }}
                            >
                                <Box
                                    sx={{
                                        p: 1.1,
                                        bgcolor: '#edf3fd',
                                        color: 'primary.main',
                                        borderRadius: 2,
                                        display: { xs: 'none', sm: 'flex' },
                                    }}
                                >
                                    <AccountTreeOutlined fontSize="small" />
                                </Box>
                                <Box sx={{ flex: 1, minWidth: 0 }}>
                                    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mb: 0.5 }}>
                                        <Typography variant="caption" color="text.secondary">
                                            {node.unit.cve_ur}
                                        </Typography>
                                        {ownRoot === node.unit.id_ur && (
                                            <Chip
                                                label="Tu área"
                                                color="primary"
                                                variant="outlined"
                                                sx={{ height: 19, fontSize: 10 }}
                                            />
                                        )}
                                    </Box>
                                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                        {node.unit.desc_ur}
                                    </Typography>
                                    <Typography variant="caption" color="text.secondary">
                                        {node.areaCount - 1} áreas dependientes ·{' '}
                                        {node.forms.filter((f) => Number(f.porcentaje) === 100).length}/
                                        {node.forms.length} completos
                                    </Typography>
                                </Box>
                                <Box sx={{ width: { xs: 58, sm: 115 }, flexShrink: 0 }}>
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
                                        fontSize: 20,
                                    }}
                                />
                            </Box>
                            <Collapse in={open.has(node.unit.id_ur)} unmountOnExit>
                                <Box
                                    id={`area-${node.unit.id_ur}`}
                                    sx={{ px: { xs: 2, md: 3 }, pb: 1, borderTop: '1px solid', borderColor: 'divider' }}
                                >
                                    {formRow(node, true)}
                                    {!!node.children.length && (
                                        <Typography className="eyebrow" sx={{ mt: 2, mb: 0 }}>
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
                <Box className="surface" sx={{ mt: 2 }}>
                    <Typography variant="h2">Otras áreas del catálogo</Typography>
                    {children(others)}
                </Box>
            )}
            {!roots.length && !others.length && (
                <div className="empty">
                    <Typography>No encontramos áreas con esos filtros</Typography>
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
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 2 }}>
                El avance del conjunto promedia el formato principal y los de sus áreas dependientes.
            </Typography>
        </section>
    );
}
