import { Head, router, useForm } from '@/lib/navigation';
import { Autocomplete, Box, Button, MenuItem, TextField, Typography } from '@mui/material';
import { ArrowBack, VisibilityOutlined } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
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
    const form = useForm<Selection>({
        mode: 'escenario',
        role: seleccion?.role || 'responsable',
        ur: seleccion?.ur || '',
    });
    const units = unidades.filter((u) => eligibleArea(u) && (!['responsable', 'responsable_institucional', 'responsable_supervisor'].includes(form.data.role) || [2, 3].includes(Number(u.nivel_ur))));
    return (
        <AuthenticatedLayout>
            <Head title="Vista por rol y área" />
            <div className="page" style={{ maxWidth: 1150 }}>
                <Button
                    startIcon={<ArrowBack />}
                    sx={{ mb: 2 }}
                    onClick={() => router.visit('/configuracion/pruebas-acceso')}
                >
                    Pruebas de acceso
                </Button>
                <PageHeading
                    title="Vista por rol y área"
                    description="Explora los formatos y el alcance de una combinación de rol y área."
                />
                <div className="split">
                    <Box
                        component="form"
                        className="surface stack"
                        onSubmit={(e) => {
                            e.preventDefault();
                            form.post('/vista-prueba');
                        }}
                    >
                        <TextField
                            select
                            label="Rol"
                            value={form.data.role}
                            onChange={(e) => form.setData({ ...form.data, role: e.target.value, ur: '' })}
                            error={!!form.errors.role}
                            helperText={form.errors.role}
                            disabled={form.processing}
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
                            disabled={form.processing}
                            renderInput={(params) => (
                                <TextField
                                    {...params}
                                    label="Área de pertenencia o responsabilidad"
                                    error={!!form.errors.ur}
                                    helperText={form.errors.ur}
                                />
                            )}
                            noOptionsText="No hay áreas disponibles"
                        />
                        <Typography variant="body2" color="text.secondary">
                            El escenario supone una asignación vigente. Para probar los permisos reales de una persona,
                            utiliza Actuar como usuario.
                        </Typography>
                        <Button
                            type="submit"
                            variant="contained"
                            startIcon={<VisibilityOutlined />}
                            disabled={form.processing || !form.data.ur}
                        >
                            Iniciar vista de prueba
                        </Button>
                    </Box>
                    <Box sx={{ p: 2 }}>
                        <Typography variant="h2" sx={{ mb: 2 }}>
                            Una prueba de solo lectura
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                            Tu cuenta permanece identificada como administrador. Una franja muestra el rol y el área que
                            estás consultando.
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
