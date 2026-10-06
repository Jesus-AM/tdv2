import { Head, router, usePage } from '@/lib/navigation';
import { useState } from 'react';
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
    const [refreshing, setRefreshing] = useState(false);
    const readOnly = !!props.simulacion || !!(props.representacion && !props.representacion.escritura);
    const avg = formatos.length
        ? Math.round(formatos.reduce((s, f) => s + Number(f.porcentaje), 0) / formatos.length)
        : 0;
    return (
        <AuthenticatedLayout>
            <Head title="Procesos operativos" />
            <div className="page">
                <PageHeading
                    className="operational-heading"
                    title="Procesos operativos"
                    description="Consulta las áreas y da seguimiento al llenado de sus formatos"
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
                            <Button variant="text" color="primary" startIcon={<Refresh />}
                                loading={refreshing} loadingPosition="start" aria-busy={refreshing}
                                onClick={() => {
                                    if (refreshing) return;
                                    setRefreshing(true);
                                    router.reload({ onFinish: () => setRefreshing(false) });
                                }}>
                                Actualizar
                            </Button>
                        </>
                    }
                />
                <Box className="operational-stats">
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
                            className="surface operational-stat"
                            key={s.label}
                        >
                            <Box className="operational-stat-icon">
                                {s.icon}
                            </Box>
                            <Box className="operational-stat-copy">
                                <Typography variant="body2" color="text.secondary">
                                    {s.label}
                                </Typography>
                                <Typography className="operational-stat-value">
                                    {s.value}
                                </Typography>
                            </Box>
                        </Box>
                    ))}
                </Box>
                {administrador && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
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
                    <Box className="stack" sx={{ gap: '8px' }}>
                        <Typography component="h2" variant="h2">
                            Formatos de mis áreas
                        </Typography>
                        {formatos.map((f) => (
                            <Box
                                key={f.id_ur}
                                className="surface personal-area-row"
                            >
                                <Box sx={{ flex: 1, minWidth: { xs: '100%', sm: 180 }, overflowWrap: 'anywhere' }}>
                                    <Typography variant="h3">{f.desc_ur}</Typography>
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.5 }}>
                                        <Typography variant="caption" color="text.secondary">
                                            {displayUnitCode(f.cve_ur)}
                                        </Typography>
                                        <Chip label={f.editable && !readOnly ? 'Puedes llenar' : 'Solo consulta'} variant="outlined" />
                                    </Box>
                                </Box>
                                <Box sx={{ width: 100 }}>
                                    <Typography variant="body2" sx={{ textAlign: 'right' }}>{f.porcentaje}%</Typography>
                                    <LinearProgress
                                        variant="determinate"
                                        value={Number(f.porcentaje)}
                                        color="secondary"
                                        sx={{ mt: 0.5 }}
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
