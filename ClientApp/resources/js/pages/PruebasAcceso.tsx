import { Head, router } from '@/lib/navigation';
import { Box, Button, Chip, Typography } from '@mui/material';
import { ArrowBack, VisibilityOutlined, ManageAccountsOutlined } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
export default function PruebasAcceso({
    puedeVistaPrueba,
    capacidad,
}: {
    puedeVistaPrueba: boolean;
    capacidad: { permitido: boolean; escritura?: boolean; no_disponible?: boolean };
}) {
    return (
        <AuthenticatedLayout>
            <Head title="Pruebas de acceso" />
            <div className="page">
                <Button startIcon={<ArrowBack />} sx={{ mb: 2 }} onClick={() => router.visit('/configuracion')}>
                    Configuración
                </Button>
                <PageHeading
                    title="Pruebas de acceso"
                    description="Elige cómo quieres comprobar la experiencia y los permisos en la plataforma."
                />
                <Box
                    sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2,minmax(0,1fr))' }, gap: 3 }}
                >
                    <Box className="surface stack" sx={{ alignContent: 'start' }}>
                        <VisibilityOutlined color="primary" sx={{ fontSize: 30 }} />
                        <Typography component="h2" variant="h2">
                            Rol y área
                        </Typography>
                        <Box>
                            <Chip label="Solo lectura" variant="outlined" />
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                            Simula un rol y una adscripción para revisar los formatos y el avance que tendría
                            disponibles. No requiere seleccionar a una persona.
                        </Typography>
                        {!puedeVistaPrueba && (
                            <Typography variant="body2">Necesitas acceso a Procesos operativos en Nexo.</Typography>
                        )}
                        <Box>
                            <Button
                                variant="contained"
                                disabled={!puedeVistaPrueba}
                                onClick={() => router.visit('/configuracion/pruebas-acceso/rol-area')}
                            >
                                Probar rol y área
                            </Button>
                        </Box>
                    </Box>
                    <Box className="surface stack" sx={{ alignContent: 'start' }}>
                        <ManageAccountsOutlined color="primary" sx={{ fontSize: 30 }} />
                        <Typography component="h2" variant="h2">
                            Actuar como usuario
                        </Typography>
                        <Box>
                            <Chip label="Autorización de Nexo" variant="outlined" />
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                            Utiliza el acceso real de una persona para comprobar sus funciones, incluida la
                            administración delegada cuando Nexo permita cambios.
                        </Typography>
                        {!capacidad.permitido && (
                            <Typography variant="body2">
                                {capacidad.no_disponible
                                    ? 'No fue posible comprobar esta función en Nexo. Revisa su disponibilidad.'
                                    : 'Habilita Actuar como usuario para tu cuenta o rol en Nexo.'}
                            </Typography>
                        )}
                        <Box>
                            <Button
                                variant="outlined"
                                disabled={!capacidad.permitido}
                                onClick={() => router.visit('/configuracion/pruebas-acceso/actuar-como-usuario')}
                            >
                                Seleccionar usuario
                            </Button>
                        </Box>
                    </Box>
                </Box>
            </div>
        </AuthenticatedLayout>
    );
}
