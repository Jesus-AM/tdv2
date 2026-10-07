import { Head, router } from '@/lib/navigation';
import { Box, Button, Typography } from '@mui/material';
import { SyncOutlined, ManageAccountsOutlined, ArrowForward, Tune } from '@mui/icons-material';
import AuthenticatedLayout from '@/Layouts/AuthenticatedLayout';
import PageHeading from '@/Components/PageHeading';
import SettingsSection from '@/Components/SettingsSection';
export default function Configuracion({
    secciones,
}: {
    secciones: { configuracion_procesos?: boolean; sincronizaciones: boolean; pruebas_acceso: boolean };
}) {
    const cards = [
        { visible: secciones.configuracion_procesos, title: 'Configuración procesos', icon: <Tune />,
            text: 'Configura la participación de áreas y revisa su impacto antes de guardar.', path: '/configuracion/procesos' },
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
            <div className="page settings-page">
                <PageHeading
                    title="Configuración"
                    description="Administra los catálogos y las herramientas de acceso de TDV2."
                />
                <Box className="settings-grid">
                    {cards.filter(c => c.visible).map(c => (
                        <SettingsSection key={c.path} className="settings-access" title={c.title} icon={c.icon} description={c.text}>
                            <Button endIcon={<ArrowForward />} onClick={() => router.visit(c.path)}>
                                Abrir {c.title.toLocaleLowerCase('es')}
                            </Button>
                        </SettingsSection>
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
