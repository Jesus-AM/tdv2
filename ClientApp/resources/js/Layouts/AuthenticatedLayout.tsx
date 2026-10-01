import { router, usePage } from '@/lib/navigation';
import { useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import axios from 'axios';
import {
    AppBar,
    Toolbar,
    Box,
    Drawer,
    IconButton,
    Button,
    Avatar,
    Menu,
    MenuItem,
    ListItemIcon,
    ListItemText,
    Divider,
    Typography,
    Chip,
    Alert,
    Tooltip,
    useMediaQuery,
} from '@mui/material';
import {
    Menu as MenuIcon,
    Close,
    Logout,
    HomeOutlined,
    AccountTreeOutlined,
    ExpandMore,
    ManageAccountsOutlined,
    ArrowBack,
    AppsOutlined,
    SettingsOutlined,
    SyncOutlined,
} from '@mui/icons-material';
import { errorResponse } from '@/lib/http';

function AccessNotice({ children, writing }: { children: ReactNode; writing: boolean }) {
    return writing ? (
        <Alert
            className="no-print"
            severity="warning"
            icon={<ManageAccountsOutlined />}
            sx={{ borderRadius: 0, py: 0.75, px: { xs: 2, md: 4 }, '& .MuiAlert-message': { width: '100%' } }}
        >
            {children}
        </Alert>
    ) : (
        <Box
            component="aside"
            role="status"
            aria-label="Contexto de acceso"
            className="no-print"
            sx={{
                bgcolor: 'background.paper',
                borderBottom: '1px solid',
                borderColor: 'divider',
                py: 1.5,
                px: { xs: 2, md: 4 },
            }}
        >
            {children}
        </Box>
    );
}

export default function AuthenticatedLayout({ children }: { children: ReactNode }) {
    const { props, url } = usePage();
    const { auth, routes, representacion: rep, simulacion: preview } = props;
    const mobile = useMediaQuery('(max-width:900px)');
    const [open, setOpen] = useState(false),
        [collapsed, setCollapsed] = useState(() => {
            try {
                return localStorage.getItem('tdv2.navigation.collapsed') === 'true';
            } catch {
                return false;
            }
        }),
        [photo, setPhoto] = useState<string | null>(null),
        [account, setAccount] = useState<HTMLElement | null>(null),
        [busy, setBusy] = useState(false);
    const mini = !mobile && collapsed;
    const width = mini ? 76 : 240;
    useEffect(() => {
        try {
            localStorage.setItem('tdv2.navigation.collapsed', String(collapsed));
        } catch {
            // Navigation remains usable when browser storage is disabled.
        }
    }, [collapsed]);
    const last = useRef(Date.now());
    useEffect(() => {
        let active = true;
        const activity = () => {
            last.current = Date.now();
        };
        const check = () => {
            if (Date.now() - last.current >= props.session.lifetime_ms) window.location.assign(routes.inicio);
        };
        const events = ['pointerdown', 'keydown', 'touchstart', 'wheel'];
        events.forEach((e) => window.addEventListener(e, activity, { passive: true }));
        document.addEventListener('visibilitychange', check);
        const timer = setInterval(check, 10000);
        axios
            .get<{ photo: string | null }>('/user/photo')
            .then(({ data }) => {
                if (active) setPhoto(data.photo);
            })
            .catch((e) => {
                const r = errorResponse(e);
                if (r?.data.redirect && [401, 403, 503].includes(r.status)) window.location.assign(r.data.redirect);
            });
        return () => {
            active = false;
            clearInterval(timer);
            events.forEach((e) => window.removeEventListener(e, activity));
            document.removeEventListener('visibilitychange', check);
        };
    }, [props.session.lifetime_ms, routes.inicio]);
    const exit = () => {
        if (busy) return;
        setBusy(true);
        router.delete(rep ? '/actuar-como-usuario' : '/vista-prueba', { onFinish: () => setBusy(false) });
    };
    const logout = () => {
        if (busy) return;
        setBusy(true);
        router.clearHistory();
        router.post(routes.logout, {}, { onFinish: () => setBusy(false) });
    };
    const visit = (path: string) => {
        setOpen(false);
        router.visit(path);
    };
    const modules = auth.modules.filter(
        (m) =>
            m.route &&
            ((m.route.startsWith('/') && !m.route.startsWith('//')) ||
                (() => {
                    try {
                        return new URL(m.route).origin === window.location.origin;
                    } catch {
                        return false;
                    }
                })()),
    );
    const currentPath = new URL(url, window.location.origin).pathname;
    const activeModule =
        [...modules]
            .sort((a, b) => b.route.length - a.route.length)
            .find((module) => {
                const path = new URL(module.route, window.location.origin).pathname;
                return currentPath === path || (path !== '/' && currentPath.startsWith(path + '/'));
            }) ||
        (/^\/(formatos|colaboradores|vista-prueba|actuar-como-usuario)(\/|$)/.test(currentPath)
            ? modules.find((module) => module.key === 'procesos_operativos')
            : undefined);
    const moduleTitle =
        activeModule?.key === 'procesos_operativos'
            ? 'Procesos operativos'
            : activeModule?.name || 'Transformación Digital';
    const navigation = (
        <Box sx={{ height: '100%', minHeight: 0, display: 'flex', flexDirection: 'column', background: '#fff' }}>
            <Box sx={{ height: 76, flexShrink: 0, display: 'flex', alignItems: 'center', px: mini ? 2 : 3, gap: 1 }}>
                <a
                    href={routes.inicio}
                    onClick={(e) => {
                        e.preventDefault();
                        visit(routes.inicio);
                    }}
                    style={{ display: 'flex', alignItems: 'center', textDecoration: 'none' }}
                >
                    {mini ? (
                        <AppsOutlined color="primary" fontSize="large" />
                    ) : (
                        <img
                            src="/images/logos/TRANSFORMACION_DIGITAL_logo.svg"
                            onError={(e) => {
                                e.currentTarget.style.display = 'none';
                            }}
                            alt="Transformación Digital"
                            style={{ width: 167, maxHeight: 48 }}
                        />
                    )}
                </a>
                {mobile && (
                    <IconButton
                        aria-label="Cerrar navegación"
                        onClick={() => setOpen(false)}
                        sx={{ ml: 'auto', mr: -2 }}
                    >
                        <Close />
                    </IconButton>
                )}
            </Box>
            <Box
                component="nav"
                id="application-navigation"
                aria-label="Módulos"
                sx={{ px: mini ? 1.2 : 2, pt: 2, flex: 1, minHeight: 0, overflowY: 'auto', overflowX: 'hidden' }}
            >
                {!mini && (
                    <div className="eyebrow" style={{ paddingLeft: 12 }}>
                        Módulos
                    </div>
                )}
                {modules
                    .filter((module) => !module.parent)
                    .map((module) => (
                        <Box key={module.key}>
                            <Tooltip title={mini ? module.name : ''} placement="right">
                                <Button
                                    fullWidth
                                    aria-label={module.name}
                                    aria-current={module.key === activeModule?.key ? 'page' : undefined}
                                    startIcon={
                                        module.key === 'configuracion' ? (
                                            <SettingsOutlined />
                                        ) : module.key === 'procesos_operativos' ? (
                                            <AccountTreeOutlined />
                                        ) : (
                                            <AppsOutlined />
                                        )
                                    }
                                    onClick={() => visit(module.route)}
                                    sx={{
                                        justifyContent: mini ? 'center' : 'flex-start',
                                        minWidth: 0,
                                        px: mini ? 1 : 1.5,
                                        py: 1.25,
                                        mb: 1,
                                        whiteSpace: 'normal',
                                        textAlign: 'left',
                                        background: module.key === activeModule?.key ? '#eaf1fc' : 'transparent',
                                        color:
                                            module.key === activeModule?.key || module.key === activeModule?.parent
                                                ? 'primary.main'
                                                : 'text.secondary',
                                        '& .MuiButton-startIcon': { mr: mini ? 0 : 1.4, ml: 0 },
                                    }}
                                >
                                    {!mini && module.name}
                                </Button>
                            </Tooltip>
                            {!mini &&
                                modules
                                    .filter((child) => child.parent === module.key)
                                    .map((child) => (
                                        <Button
                                            key={child.key}
                                            fullWidth
                                            aria-label={child.name}
                                            aria-current={child.key === activeModule?.key ? 'page' : undefined}
                                            startIcon={
                                                child.key === 'sincronizaciones' ? (
                                                    <SyncOutlined />
                                                ) : (
                                                    <ManageAccountsOutlined />
                                                )
                                            }
                                            onClick={() => visit(child.route)}
                                            sx={{
                                                justifyContent: 'flex-start',
                                                textAlign: 'left',
                                                minWidth: 0,
                                                pl: 3,
                                                py: 1,
                                                mb: 0.5,
                                                fontSize: 12,
                                                whiteSpace: 'normal',
                                                color:
                                                    child.key === activeModule?.key ? 'primary.main' : 'text.secondary',
                                                background: child.key === activeModule?.key ? '#eaf1fc' : 'transparent',
                                            }}
                                        >
                                            {child.name}
                                        </Button>
                                    ))}
                        </Box>
                    ))}
            </Box>
            {!mini && (
                <Box sx={{ flexShrink: 0, p: 2, borderTop: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="caption" color="text.secondary">
                        Universidad Autónoma
                        <br />
                        de Ciudad Juárez
                    </Typography>
                </Box>
            )}
        </Box>
    );
    return (
        <>
            <a href="#main-content" className="skip-link">
                Ir al contenido
            </a>
            <Drawer
                className="no-print"
                variant={mobile ? 'temporary' : 'permanent'}
                open={mobile ? open : true}
                onClose={() => setOpen(false)}
                sx={{ '& .MuiDrawer-paper': { width: mobile ? 240 : width, borderColor: 'divider' } }}
            >
                {navigation}
            </Drawer>
            <AppBar
                className="no-print"
                position="fixed"
                color="inherit"
                elevation={0}
                sx={{
                    width: mobile ? '100%' : `calc(100% - ${width}px)`,
                    ml: mobile ? 0 : `${width}px`,
                    background: '#fff',
                    borderBottom: '1px solid',
                    borderColor: 'divider',
                }}
            >
                <Toolbar sx={{ minHeight: '68px!important', gap: 1.5, px: { xs: 1.5, md: 3 } }}>
                    <Tooltip title={mobile ? 'Abrir navegación' : mini ? 'Expandir navegación' : 'Contraer navegación'}>
                        <IconButton
                            aria-label={
                                mobile ? 'Abrir navegación' : mini ? 'Expandir navegación' : 'Contraer navegación'
                            }
                            aria-controls="application-navigation"
                            aria-expanded={mobile ? open : !mini}
                            onClick={() => (mobile ? setOpen(true) : setCollapsed((value) => !value))}
                        >
                            <MenuIcon />
                        </IconButton>
                    </Tooltip>
                    <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography
                            sx={{ fontSize: 11, color: 'text.secondary', display: { xs: 'none', sm: 'block' } }}
                        >
                            Transformación Digital
                        </Typography>
                        <Typography noWrap sx={{ fontSize: 14, fontWeight: 600 }}>
                            {url.startsWith('/colaboradores')
                                ? 'Colaboradores'
                                : url.startsWith('/actuar')
                                  ? 'Actuar como usuario'
                                  : url.startsWith('/vista')
                                    ? 'Vista de prueba'
                                    : moduleTitle}
                        </Typography>
                    </Box>
                    <Button
                        color="inherit"
                        aria-label="Abrir menú de usuario"
                        onClick={(e) => setAccount(e.currentTarget)}
                        sx={{ minWidth: 40, p: 0.5, gap: 1 }}
                    >
                        <Avatar
                            src={photo || undefined}
                            sx={{ width: 32, height: 32, fontSize: 12, bgcolor: '#e6eefb', color: 'primary.dark' }}
                        >
                            {auth.user?.name
                                .split(/\s+/)
                                .slice(0, 2)
                                .map((n) => n[0])
                                .join('')}
                        </Avatar>
                        <Typography
                            sx={{ display: { xs: 'none', lg: 'block' }, maxWidth: 165, fontSize: 13, fontWeight: 500 }}
                            noWrap
                        >
                            {auth.user?.name}
                        </Typography>
                        <ExpandMore fontSize="small" sx={{ display: { xs: 'none', sm: 'block' } }} />
                    </Button>
                    <Menu
                        anchorEl={account}
                        open={!!account}
                        onClose={() => setAccount(null)}
                        slotProps={{ paper: { sx: { width: 285, maxWidth: 'calc(100vw - 24px)' } } }}
                    >
                        <Box sx={{ px: 2, py: 1 }}>
                            <div className="eyebrow">Tu cuenta</div>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                {auth.user?.name}
                            </Typography>
                            <Typography variant="caption" sx={{ overflowWrap: 'anywhere' }}>
                                {auth.user?.email}
                            </Typography>
                        </Box>
                        <Divider />
                        {(rep || preview) && (
                            <MenuItem onClick={exit} disabled={busy}>
                                <ListItemIcon>
                                    <ArrowBack />
                                </ListItemIcon>
                                <ListItemText>Volver a mi usuario</ListItemText>
                            </MenuItem>
                        )}
                        <MenuItem component="a" href={routes.home}>
                            <ListItemIcon>
                                <HomeOutlined />
                            </ListItemIcon>
                            <ListItemText>Portada pública</ListItemText>
                        </MenuItem>
                        <MenuItem disabled={busy} onClick={logout}>
                            <ListItemIcon>
                                <Logout />
                            </ListItemIcon>
                            <ListItemText>Cerrar sesión</ListItemText>
                        </MenuItem>
                    </Menu>
                </Toolbar>
            </AppBar>
            <Box
                className="app-content"
                sx={{
                    ml: mobile ? 0 : `${width}px`,
                    pt: '68px',
                    minHeight: '100dvh',
                    display: 'flex',
                    flexDirection: 'column',
                }}
            >
                {(rep || preview) && (
                    <AccessNotice writing={!!rep?.escritura}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
                            <Box sx={{ flex: 1, minWidth: 220 }}>
                                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                                    {rep ? `Actuando como ${rep.nombre}` : `Vista de prueba · ${preview?.label}`}
                                </Typography>
                                <Typography variant="caption">
                                    {rep
                                        ? `${rep.email} · Tu cuenta: ${auth.user?.name} · Hasta ${new Date(rep.expira_en).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit' })}`
                                        : preview?.unit?.desc_ur || preview?.roleLabel}
                                </Typography>
                            </Box>
                            <Chip
                                label={rep?.escritura ? 'Cambios reales habilitados' : 'Solo lectura'}
                                variant="outlined"
                            />
                            <Button
                                color="inherit"
                                variant="outlined"
                                disabled={busy}
                                startIcon={<ArrowBack />}
                                onClick={exit}
                            >
                                {rep ? 'Volver a mi usuario' : 'Salir de la prueba'}
                            </Button>
                        </Box>
                    </AccessNotice>
                )}
                <main id="main-content" tabIndex={-1} style={{ flex: 1, minWidth: 0, outline: 'none' }}>
                    {children}
                </main>
                <Box
                    className="app-footer"
                    sx={{
                        px: 3,
                        py: 2,
                        display: 'flex',
                        justifyContent: 'space-between',
                        gap: 2,
                        color: 'text.secondary',
                        fontSize: 11,
                        borderTop: '1px solid',
                        borderColor: 'divider',
                    }}
                >
                    <span>UACJ · Transformación Digital</span>
                    <span>Subdirección de Inteligencia de Datos</span>
                </Box>
            </Box>
        </>
    );
}
