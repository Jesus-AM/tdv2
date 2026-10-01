import { Head, router } from '@/lib/navigation';
import { Box, Button, Typography } from '@mui/material';
import { SyncOutlined, ManageAccountsOutlined, ArrowForward } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
export default function Configuracion({
    secciones,
}: {
    secciones: { sincronizaciones: boolean; pruebas_acceso: boolean };
}) {
    const cards = [
        {
            visible: secciones.sincronizaciones,
            title: 'Sincronizaciones',
            icon: <SyncOutlined />,
            text: 'Actualiza las unidades responsables del SII y el inventario de ILDA. Administra la programación y consulta cada ejecución.',
            path: '/configuracion/sincronizaciones',
        },
        {
            visible: secciones.pruebas_acceso,
            title: 'Pruebas de acceso',
            icon: <ManageAccountsOutlined />,
            text: 'Revisa el alcance de un rol y área o actúa como una persona con la autorización de Nexo.',
            path: '/configuracion/pruebas-acceso',
        },
    ];
    return (
        <AuthenticatedLayout>
            <Head title="Configuración" />
            <div className="page">
                <PageHeading
                    title="Configuración"
                    description="Administra los catálogos y las herramientas de acceso de TDV2."
                />
                <Box
                    sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2,minmax(0,1fr))' }, gap: 3 }}
                >
                    {cards
                        .filter((c) => c.visible)
                        .map((c) => (
                            <Box
                                key={c.path}
                                className="surface"
                                sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 2 }}
                            >
                                <Box
                                    sx={{
                                        color: 'primary.main',
                                        bgcolor: '#edf3fd',
                                        p: 1.5,
                                        borderRadius: '12px',
                                        display: 'flex',
                                    }}
                                >
                                    {c.icon}
                                </Box>
                                <Typography component="h2" variant="h2">
                                    {c.title}
                                </Typography>
                                <Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
                                    {c.text}
                                </Typography>
                                <Button
                                    variant="outlined"
                                    endIcon={<ArrowForward />}
                                    onClick={() => router.visit(c.path)}
                                >
                                    Abrir {c.title.toLocaleLowerCase('es')}
                                </Button>
                            </Box>
                        ))}
                </Box>
                {!cards.some((c) => c.visible) && (
                    <Typography variant="body2" color="text.secondary">
                        No tienes submódulos habilitados. Revisa la asignación del rol Administrador en Nexo.
                    </Typography>
                )}
            </div>
        </AuthenticatedLayout>
    );
}
