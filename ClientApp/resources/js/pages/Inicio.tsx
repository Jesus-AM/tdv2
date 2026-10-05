import { Head, router, usePage } from '@/lib/navigation';
import { Box, Button, Typography, LinearProgress, Chip } from '@mui/material';
import { PeopleOutlined, Refresh, DescriptionOutlined, DonutLarge, TaskAlt, ArrowForward } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import AreaDirectory from '@/Components/AreaDirectory';
import PageHeading from '@/Components/PageHeading';
import { displayUnitCode } from '@/lib/area-directory';
import type { Unit, FormRow } from '@/types/tdv2';
export default function Inicio({
    formatos,
    administrador,
    consultaInstitucional = false,
    directorio = [],
    urAdministracion,
    puedeColaboradores,
}: {
    formatos: FormRow[];
    administrador: boolean;
    consultaInstitucional?: boolean;
    directorio?: Unit[];
    urAdministracion: Unit | null;
    puedeColaboradores: boolean;
    sincronizadoEn: string | null;
}) {
    const { props } = usePage();
    const readOnly = !!props.simulacion || !!(props.representacion && !props.representacion.escritura);
    const avg = formatos.length
        ? Math.round(formatos.reduce((s, f) => s + Number(f.porcentaje), 0) / formatos.length)
        : 0;
    return (
        <AuthenticatedLayout>
            <Head title="Procesos operativos" />
            <div className="page">
                <PageHeading
                    title="Procesos operativos"
                    description="Identifica los procesos de tu área y da seguimiento al llenado de sus formatos."
                    actions={
                        <>
                            {puedeColaboradores && (
                                <Button
                                    variant="contained"
                                    startIcon={<PeopleOutlined />}
                                    onClick={() => router.visit('/colaboradores')}
                                >
                                    Colaboradores
                                </Button>
                            )}
                            <Button variant="outlined" startIcon={<Refresh />} onClick={() => router.reload()}>
                                Actualizar
                            </Button>
                        </>
                    }
                />
                <Box
                    sx={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(3,minmax(0,1fr))',
                        gap: { xs: 1, md: 2 },
                        mb: 3,
                    }}
                >
                    {[
                        { label: 'Formatos disponibles', value: formatos.length, icon: <DescriptionOutlined /> },
                        { label: 'Avance promedio', value: `${avg}%`, icon: <DonutLarge /> },
                        {
                            label: 'Formatos completos',
                            value: formatos.filter((f) => Number(f.porcentaje) === 100).length,
                            icon: <TaskAlt />,
                        },
                    ].map((s) => (
                        <Box
                            className="surface"
                            key={s.label}
                            sx={{
                                display: 'flex',
                                alignItems: 'center',
                                gap: 2,
                                p: { xs: '12px!important', md: '20px!important' },
                            }}
                        >
                            <Box
                                sx={{
                                    display: { xs: 'none', sm: 'flex' },
                                    color: 'primary.main',
                                    bgcolor: '#edf3fd',
                                    p: 1.2,
                                    borderRadius: 2,
                                }}
                            >
                                {s.icon}
                            </Box>
                            <Box>
                                <Typography variant="caption" color="text.secondary">
                                    {s.label}
                                </Typography>
                                <Typography
                                    sx={{ fontSize: { xs: 24, md: 28 }, fontWeight: 600, lineHeight: 1.3, mt: 0.4 }}
                                >
                                    {s.value}
                                </Typography>
                            </Box>
                        </Box>
                    ))}
                </Box>
                {administrador && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                        {urAdministracion
                            ? `Puedes consultar todas las áreas y llenar los formatos de ${urAdministracion.desc_ur} y sus áreas dependientes.`
                            : 'Puedes consultar los formatos de todas las áreas. Para habilitar el llenado necesitas una adscripción vigente en Nexo.'}
                    </Typography>
                )}
                {consultaInstitucional || administrador ? (
                    <AreaDirectory
                        units={directorio}
                        forms={formatos}
                        ownRoot={urAdministracion?.id_ur || null}
                        readOnly={readOnly}
                    />
                ) : (
                    <Box className="stack">
                        <Typography component="h2" variant="h2">
                            Formatos de mis áreas
                        </Typography>
                        {formatos.map((f) => (
                            <Box
                                key={f.id_ur}
                                className="surface"
                                sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}
                            >
                                <Box sx={{ flex: 1, minWidth: 220 }}>
                                    <Typography variant="caption" color="text.secondary">
                                        {displayUnitCode(f.cve_ur)}
                                    </Typography>
                                    <Typography variant="h3">{f.desc_ur}</Typography>
                                    <Chip
                                        sx={{ mt: 1 }}
                                        label={f.editable && !readOnly ? 'Puedes llenar' : 'Solo consulta'}
                                        variant="outlined"
                                    />
                                </Box>
                                <Box sx={{ width: 100 }}>
                                    <Typography variant="body2">{f.porcentaje}%</Typography>
                                    <LinearProgress
                                        variant="determinate"
                                        value={Number(f.porcentaje)}
                                        color="secondary"
                                        sx={{ mt: 1 }}
                                    />
                                </Box>
                                <Button
                                    variant="contained"
                                    endIcon={<ArrowForward />}
                                    onClick={() => router.visit(f.url)}
                                >
                                    {f.editable && !readOnly ? 'Continuar llenado' : 'Ver formato'}
                                </Button>
                            </Box>
                        ))}
                        {!formatos.length && (
                            <div className="empty">
                                No tienes formatos asignados. Consulta tu acceso con el responsable del área.
                            </div>
                        )}
                    </Box>
                )}
            </div>
        </AuthenticatedLayout>
    );
}
