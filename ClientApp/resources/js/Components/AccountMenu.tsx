import { useState } from 'react';
import { router, usePage } from '@/lib/navigation';
import { useAccountPhoto } from './AccountPhoto';
import { Avatar, Box, Button, Divider, ListItemIcon, ListItemText, Menu, MenuItem, Typography } from '@mui/material';
import { ArrowBack, ExpandMore, HomeOutlined, Logout, ManageAccountsOutlined } from '@mui/icons-material';

/** Cuenta e identidad efectiva compartidas por Procedimientos y Competencias. */
export default function AccountMenu({ onExit, onPrivacy, includeNameInLabel = false }: { onExit?: () => void; onPrivacy?: () => void; includeNameInLabel?: boolean }) {
    const { props } = usePage();
    const { auth, routes, representacion: rep, simulacion: preview } = props;
    const [account, setAccount] = useState<HTMLElement | null>(null), [busy, setBusy] = useState(false);
    const photo = useAccountPhoto();
    const visibleName = rep?.nombre || auth.user?.name;
    const exit = () => {
        if (busy) return;
        if (onExit) { setAccount(null); onExit(); return; }
        setBusy(true);
        router.delete(rep ? '/actuar-como-usuario' : '/vista-prueba', { onFinish: () => setBusy(false) });
    };
    const logout = () => {
        if (busy) return;
        setBusy(true); router.clearHistory();
        router.post(routes.logout, {}, { onFinish: () => setBusy(false) });
    };
    return <>
                    <Button
                        color="inherit"
                        aria-label={includeNameInLabel && visibleName ? `Abrir menú de usuario · ${visibleName}` : 'Abrir menú de usuario'}
                        aria-haspopup="menu"
                        aria-expanded={!!account}
                        aria-controls={account ? 'account-menu' : undefined}
                        onClick={(e) => setAccount(e.currentTarget)}
                        className="account-trigger"
                        title={visibleName}
                        sx={{ minWidth: 40, p: 0.5, gap: 1 }}
                    >
                        <Avatar
                            alt={visibleName}
                            key={props.photoContext || 'initials'}
                            src={photo || undefined}
                            sx={{ width: 32, height: 32, fontSize: 12, bgcolor: 'var(--uacj-soft)', color: 'primary.dark' }}
                        >
                            {visibleName
                                ?.split(/\s+/)
                                .slice(0, 2)
                                .map((n) => n[0])
                                .join('')}
                        </Avatar>
                        <Typography
                            className="account-visible-name"
                            sx={{ display: { xs: 'none', lg: 'block' }, maxWidth: 165, fontSize: 13, fontWeight: 500 }}
                            noWrap
                        >
                            {visibleName}
                        </Typography>
                        <ExpandMore fontSize="small" sx={{ display: { xs: 'none', sm: 'block' } }} />
                    </Button>
                    <Menu
                        id="account-menu"
                        disableScrollLock
                        anchorEl={account}
                        open={!!account}
                        onClose={() => setAccount(null)}
                        slotProps={{ paper: { sx: { width: 285, maxWidth: 'calc(100vw - 24px)', mt: 1, boxShadow: '0 6px 24px rgb(33 52 85 / .08)' } } }}
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
                        {rep && <Box sx={{ px: 2, py: 1 }}><Typography variant="caption">Identidad representada · {rep.escritura ? 'Cambios reales habilitados' : 'Solo consulta'}</Typography><Typography variant="body2" sx={{ fontWeight: 600 }}>{rep.nombre}</Typography><Typography variant="caption" sx={{ overflowWrap: 'anywhere' }}>{rep.email}</Typography></Box>}
                        {onPrivacy && <MenuItem onClick={() => { setAccount(null); onPrivacy(); }}><ListItemText>Aviso de privacidad</ListItemText></MenuItem>}
                        <Divider />
                        {(rep || preview) && (
                            <MenuItem onClick={exit} disabled={busy}>
                                <ListItemIcon>
                                    <ArrowBack />
                                </ListItemIcon>
                                <ListItemText>Volver a mi usuario</ListItemText>
                            </MenuItem>
                        )}
                        <MenuItem component="a" href={routes.home} onClick={event => { event.preventDefault(); router.visit(routes.home); }}>
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
                        <MenuItem disabled={busy} onClick={() => {
                            setBusy(true); router.clearHistory();
                            router.post('/session/use-another-account', {}, { onFinish: () => setBusy(false) });
                        }}><ListItemIcon><ManageAccountsOutlined /></ListItemIcon><ListItemText>Usar otra cuenta</ListItemText></MenuItem>
                    </Menu>
    </>;
}
