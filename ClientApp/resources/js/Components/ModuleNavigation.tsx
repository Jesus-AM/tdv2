import { useState } from 'react';
import { Box, Button, Divider, ListItemIcon, ListItemText, Menu, MenuItem, useMediaQuery } from '@mui/material';
import { AccountTreeOutlined, SettingsOutlined, SyncOutlined, FactCheckOutlined, GridViewOutlined, ExpandMore, Tune } from '@mui/icons-material';
import type { NavigationModule } from '@/lib/module-navigation';
import { activeModule, moduleGroups } from '@/lib/module-navigation';
const icons: Record<string, React.ReactNode> = {
    procedimientos_institucionales: <AccountTreeOutlined fontSize="small" />, procesos_operativos: <AccountTreeOutlined fontSize="small" />, configuracion: <SettingsOutlined fontSize="small" />,
    sincronizaciones: <SyncOutlined fontSize="small" />, pruebas_acceso: <FactCheckOutlined fontSize="small" />,
    configuracion_procesos: <Tune fontSize="small" />,
};
export default function ModuleNavigation({ modules, url, mobile, onVisit }: {
    modules: NavigationModule[]; url: string; mobile: boolean; onVisit: (route: string) => void;
}) {
    const [menu, setMenu] = useState<{ element: HTMLElement; key: string } | null>(null);
    const reduced = useMediaQuery('(prefers-reduced-motion: reduce)');
    const groups = moduleGroups(modules, window.location.origin);
    const selected = activeModule(groups.flatMap(group => [group.module, ...group.children]), url, window.location.origin);
    const expanded = groups.find(group => group.module.key === menu?.key);
    function visit(module: NavigationModule) { setMenu(null); onVisit(module.route); }
    function item(module: NavigationModule) {
        return <MenuItem key={module.key} selected={selected?.key === module.key}
            sx={{ minHeight: 44, mx: .75, my: .25, borderRadius: 1, borderLeft: '3px solid',
                borderColor: selected?.key === module.key ? 'primary.main' : 'transparent', whiteSpace: 'normal',
                color: selected?.key === module.key ? 'primary.dark' : 'text.primary',
                '& .MuiListItemText-primary': { fontSize: 14, fontWeight: selected?.key === module.key ? 600 : 400 },
                '& .MuiListItemIcon-root': { color: 'inherit' } }}
            aria-current={selected?.key === module.key ? 'page' : undefined} onClick={() => visit(module)}>
            <ListItemIcon>{icons[module.key] || <GridViewOutlined fontSize="small" />}</ListItemIcon>
            <ListItemText>{module.name}</ListItemText>
        </MenuItem>;
    }
    return <Box component="nav" id={mobile ? 'application-navigation' : 'desktop-navigation'} aria-label="Módulos"
        sx={{ display: 'flex', flexDirection: mobile ? 'column' : 'row', gap: 1, p: mobile ? 2 : 0 }}>
        {groups.map(({ module, children }) => {
            const active = selected?.key === module.key || children.some(child => child.key === selected?.key);
            return <Button key={module.key} startIcon={icons[module.key] || <GridViewOutlined fontSize="small" />}
                endIcon={children.length > 0 ? <ExpandMore /> : undefined}
                aria-current={active ? children.length ? 'location' : 'page' : undefined}
                aria-haspopup={children.length ? 'menu' : undefined}
                aria-expanded={children.length ? menu?.key === module.key : undefined}
                aria-controls={menu?.key === module.key ? 'module-submenu' : undefined}
                onClick={event => children.length ? setMenu({ element: event.currentTarget, key: module.key }) : visit(module)}
                sx={{ justifyContent: mobile ? 'flex-start' : 'center', fontSize: 13, minHeight: 40, px: 1.5,
                    border: '1px solid', borderColor: active ? '#d5e3f9' : 'transparent',
                    '& .MuiButton-endIcon': { ml: mobile ? 'auto' : 1 },
                    bgcolor: active ? '#eaf1fc' : 'transparent', color: active ? 'primary.main' : 'text.secondary' }}>{module.name}</Button>;
        })}
        <Menu id="module-submenu" anchorEl={menu?.element} open={!!expanded} onClose={() => setMenu(null)}
            disableScrollLock transitionDuration={reduced ? 0 : 180}
            slotProps={{ list: { 'aria-label': expanded?.module.name || 'Submódulos' },
                paper: { sx: { mt: .75, minWidth: 260, maxWidth: 'calc(100vw - 24px)', boxShadow: '0 6px 24px #20314d14' } } }}>
            {expanded && item(expanded.module)}
            <Divider />
            {expanded?.children.map(item)}
        </Menu>
    </Box>;
}
