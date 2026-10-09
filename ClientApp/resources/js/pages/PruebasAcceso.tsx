import { Head, router } from '@/lib/navigation';
import { Box, Button, Chip, Typography } from '@mui/material';
import { VisibilityOutlined, ManageAccountsOutlined } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import SettingsSection from '@/Components/SettingsSection';
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
            <div className="page settings-page">
                <PageHeading
                    breadcrumbs={[{ label: 'Configuración', href: '/configuracion' }]}
                    title="Pruebas de acceso"
                    description="Elige cómo quieres comprobar la experiencia y los permisos en la plataforma."
                />
                <Box className="settings-grid">
                    <SettingsSection title="Rol y área" icon={<VisibilityOutlined />} className="settings-access"
                        description="Simula un rol y una adscripción para revisar los formatos y el avance disponibles. No requiere seleccionar a una persona.">
                        <Box>
                            <Chip label="Solo lectura" variant="outlined" />
                        </Box>
                        {!puedeVistaPrueba && (
                            <Typography variant="body2">Necesitas acceso a Procedimientos Institucionales en Nexo.</Typography>
                        )}
                        <Box sx={{ mt: 2 }}>
                            <Button
                                variant="contained"
                                disabled={!puedeVistaPrueba}
                                onClick={() => router.visit('/configuracion/pruebas-acceso/rol-area')}
                            >
                                Probar rol y área
                            </Button>
                        </Box>
                    </SettingsSection>
                    <SettingsSection title="Actuar como usuario" icon={<ManageAccountsOutlined />} className="settings-access"
                        description="Utiliza el acceso real de una persona para comprobar sus funciones, incluida la administración delegada cuando Nexo permita cambios.">
                        <Box>
                            <Chip label="Autorización de Nexo" variant="outlined" />
                        </Box>
                        {capacidad.escritura && <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                            Puede habilitar escritura real con los permisos de la persona representada.
                        </Typography>}
                        {!capacidad.permitido && (
                            <Typography variant="body2">
                                {capacidad.no_disponible
                                    ? 'No fue posible comprobar esta función en Nexo. Revisa su disponibilidad.'
                                    : 'Habilita Actuar como usuario para tu cuenta o rol en Nexo.'}
                            </Typography>
                        )}
                        <Box sx={{ mt: 2 }}>
                            <Button
                                variant="outlined"
                                disabled={!capacidad.permitido}
                                onClick={() => router.visit('/configuracion/pruebas-acceso/actuar-como-usuario')}
                            >
                                Seleccionar usuario
                            </Button>
                        </Box>
                    </SettingsSection>
                </Box>
            </div>
        </AuthenticatedLayout>
    );
}
