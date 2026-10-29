import { Head, router, usePage } from '@/lib/navigation';
import { useState } from 'react';
import { Alert, Box, Button, Typography } from '@mui/material';
import InstitutionalFooter from '@/Components/InstitutionalFooter';
import AccessActions from '@/Components/AccessActions';

export default function Accesos() {
    const { props } = usePage();
    const [leaving, setLeaving] = useState(false), [error, setError] = useState('');
    const available = props.available ?? { options: [], unavailable: true };
    const title = 'Elige un sitio';
    return <>
        <Head title={title} />
        <Box sx={{ minHeight: '100dvh', display: 'flex', flexDirection: 'column' }}>
            <Box component="main" sx={{ flex: 1, display: 'grid', placeItems: 'center', p: 3 }}>
                <Box className="surface" sx={{ width: '100%', maxWidth: 640, p: '36px!important' }}>
                    <Typography variant="h1" component="h1">{title}</Typography>
                    {(props.representacion || props.simulacion) && <Alert severity="info" sx={{ my: 3 }} action={<Button disabled={leaving} onClick={() => {
                        setLeaving(true); setError('');
                        router.delete(props.representacion?.application === 'competencias-digitales' ? '/competencias-digitales/api/representacion' : props.representacion ? '/actuar-como-usuario' : '/vista-prueba', {
                            onFinish: () => setLeaving(false), onError: e => setError(e.general),
                        });
                    }}>Volver a mi usuario</Button>}>{props.representacion ? `Actuando como ${props.representacion.nombre}` : 'Vista de consulta activa'}</Alert>}
                    {error && <Alert severity="error" sx={{ my: 2 }}>{error}</Alert>}
                    {available.unavailable && <Alert severity="warning" sx={{ my: 3 }}>No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.</Alert>}
                    {!available.unavailable && !available.options.length && <Alert severity="info" sx={{ my: 3 }}>No hay sitios disponibles para tu cuenta.</Alert>}
                    {!!available.options.length && <>
                        <Typography sx={{ my: 3 }}>Selecciona el sitio que deseas abrir.</Typography>
                        <Box sx={{ display: 'grid', gap: 2, mb: 4, minWidth: 0 }}>{available.options.map(option => <Button variant="outlined" size="large" href={option.path} key={option.path} sx={{ minWidth: 0, overflowWrap: 'anywhere' }}>{option.name}</Button>)}</Box>
                    </>}
                    <AccessActions destination="/accesos" />
                </Box>
            </Box>
            <InstitutionalFooter variant="internal" />
        </Box>
    </>;
}
