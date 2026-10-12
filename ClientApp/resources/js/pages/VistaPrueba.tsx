import { Head, useForm, usePage } from '@/lib/navigation';
import { Alert, Autocomplete, Box, Button, MenuItem, TextField, Typography } from '@mui/material';
import { VisibilityOutlined } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import AccessModeTabs from '@/Components/AccessModeTabs';
import type { Unit } from '@/types/tdv2';
import { displayUnitCode, normalizeAreaText, eligibleArea } from '@/lib/area-directory';
type Selection = { mode: 'escenario'; role: string; ur: string };
export default function VistaPrueba({
    unidades,
    roles,
    seleccion,
}: {
    unidades: Unit[];
    roles: { value: string; title: string }[];
    seleccion: Partial<Selection> | null;
}) {
    const { props } = usePage();
    const active = !!(props.simulacion || props.representacion);
    const form = useForm<Selection>({
        mode: 'escenario',
        role: seleccion?.role || 'responsable',
        ur: seleccion?.ur || '',
    });
    const units = unidades.filter((u) => eligibleArea(u) && (!['responsable', 'responsable_institucional', 'responsable_supervisor'].includes(form.data.role) || [2, 3].includes(Number(u.nivel_ur))));
    const selected = units.find(u => u.id_ur === form.data.ur);
    return (
        <AuthenticatedLayout>
            <Head title="Vista por rol y área" />
            <div className="page settings-page access-page">
                <PageHeading
                    breadcrumbs={[{ label: 'Configuración', href: '/configuracion' }, { label: 'Pruebas de acceso', href: '/configuracion/pruebas-acceso' }]}
                    title="Vista por rol y área"
                    description="Explora los formatos y el alcance de una combinación de rol y área."
                />
                <AccessModeTabs value="vista" />
                {active && <Typography role="status" sx={{ mb: 2 }}>Hay un contexto de prueba activo. Utiliza la salida del aviso superior antes de aplicar otra vista o actuar como usuario.</Typography>}
                <div className="access-columns">
                    <Box
                        component="form"
                        className="surface stack"
                        onSubmit={(e) => {
                            e.preventDefault();
                            if (!active && selected) form.post('/vista-prueba');
                        }}
                    >
                        <Typography component="h2" variant="h2">Selecciona el escenario</Typography>
                        <Box sx={{ display: 'grid', gridTemplateColumns: 'minmax(0,1fr)', gap: 2 }}>
                        <TextField
                            select
                            label="Rol"
                            value={form.data.role}
                            onChange={(e) => form.setData({ ...form.data, role: e.target.value, ur: '' })}
                            error={!!form.errors.role}
                            helperText={form.errors.role}
                            disabled={form.processing || active}
                        >
                            {roles.map((role) => (
                                <MenuItem key={role.value} value={role.value}>
                                    {role.title}
                                </MenuItem>
                            ))}
                        </TextField>
                        <Autocomplete
                            options={units}
                            getOptionLabel={(u) => `${displayUnitCode(u.cve_ur)} · ${u.desc_ur}`}
                            filterOptions={(options, state) => options.filter((u) => normalizeAreaText(`${u.cve_ur} ${u.desc_ur}`).includes(normalizeAreaText(state.inputValue)))}
                            isOptionEqualToValue={(a, b) => a.id_ur === b.id_ur}
                            value={units.find((u) => u.id_ur === form.data.ur) || null}
                            onChange={(_, u) => form.setData('ur', u?.id_ur || '')}
                            disabled={form.processing || active}
                            getOptionKey={u => u.id_ur}
                            renderOption={(attributes, unit) => <li {...attributes} key={unit.id_ur} style={{ whiteSpace: 'normal', overflowWrap: 'anywhere' }}>{displayUnitCode(unit.cve_ur)} · {unit.desc_ur}</li>}
                            renderInput={(params) => (
                                <TextField
                                    {...params}
                                    label="Área de pertenencia o responsabilidad"
                                    placeholder="Selecciona un área"
                                    slotProps={{ ...params.slotProps, inputLabel: { ...params.slotProps.inputLabel, shrink: true } }}
                                    error={!!form.errors.ur}
                                    helperText={form.errors.ur}
                                />
                            )}
                            noOptionsText="No hay áreas disponibles"
                        />
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                            El escenario supone una asignación vigente. Para probar los permisos reales de una persona,
                            utiliza Actuar como usuario.
                        </Typography>
                        <Button
                            type="submit"
                            variant="contained"
                            startIcon={<VisibilityOutlined />}
                            disabled={form.processing || active || !selected}
                            loading={form.processing}
                            sx={{ justifySelf: 'start', minHeight: 40 }}
                        >
                            Aplicar vista
                        </Button>
                        {form.errors.general && <Alert severity="error">{form.errors.general}</Alert>}
                    </Box>
                    <Box className="surface" role="status" aria-live="polite">
                        <Typography component="h2" variant="h2" sx={{ mb: 2 }}>Contexto por aplicar · Solo lectura</Typography>
                        <Typography variant="body2" sx={{ mb: 1 }}>Rol: {roles.find(role => role.value === form.data.role)?.title || 'Sin selección'}</Typography>
                        <Typography variant="body2" sx={{ overflowWrap: 'anywhere', mb: 2 }}>Área: {selected ? `${displayUnitCode(selected.cve_ur)} · ${selected.desc_ur}` : 'Selecciona un área'}</Typography>
                        <Typography component="h2" variant="h2" sx={{ mb: 2 }}>
                            Una prueba de solo lectura
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                            Cuenta autenticada: {props.auth.user?.email}. La franja superior identifica la vista activa y permite salir.
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                            La captura y los cambios de colaboradores están desactivados. La prueba dura 30 minutos;
                            puedes salir en cualquier momento.
                        </Typography>
                    </Box>
                </div>
            </div>
        </AuthenticatedLayout>
    );
}
