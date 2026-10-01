import { Head, router, usePage } from '@/lib/navigation';
import { Box, Button, Typography, Alert } from '@mui/material';
import { LockOutlined, HomeOutlined, Refresh } from '@mui/icons-material';
import { useState } from 'react';
export default function AccesoRestringido() {
    const { props } = usePage();
    const [leaving, setLeaving] = useState(false);
    return (
        <>
            <Head title="Acceso restringido" />
            <Box sx={{ minHeight: '100dvh', display: 'grid', placeItems: 'center', p: 3 }}>
                <Box className="surface" sx={{ maxWidth: 560, p: '36px!important' }}>
                    <LockOutlined color="primary" sx={{ fontSize: 36, mb: 2 }} />
                    <Typography variant="h1" component="h1">
                        Acceso no disponible
                    </Typography>
                    <Alert severity="warning" sx={{ my: 3 }}>
                        {props.access?.message || 'No tienes acceso a este recurso de Transformación Digital.'}
                    </Alert>
                    {props.representacion && (
                        <Typography variant="body2" sx={{ mb: 2 }}>
                            Contexto de representación: {props.representacion.nombre}. Vuelve a tu usuario para terminarlo.
                        </Typography>
                    )}
                    <div className="row-actions">
                        {(props.representacion || props.simulacion) && (
                            <Button variant="contained" disabled={leaving} onClick={() => {
                                setLeaving(true);
                                router.delete(props.representacion ? '/actuar-como-usuario' : '/vista-prueba', { onFinish: () => setLeaving(false) });
                            }}>
                                Volver a mi usuario
                            </Button>
                        )}
                        <Button variant="contained" startIcon={<HomeOutlined />} href={props.routes.home}>
                            Portada pública
                        </Button>
                        <Button
                            variant="outlined"
                            startIcon={<Refresh />}
                            onClick={() => router.visit(props.routes.inicio)}
                        >
                            Reintentar acceso
                        </Button>
                        <Button href={props.routes.connect}>Iniciar sesión</Button>
                    </div>
                </Box>
            </Box>
        </>
    );
}
