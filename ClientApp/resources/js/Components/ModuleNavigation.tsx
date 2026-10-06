import { useState } from 'react';
import { Box, Button, Divider, ListItemIcon, ListItemText, Menu, MenuItem, useMediaQuery } from '@mui/material';
import { AccountTreeOutlined, SettingsOutlined, SyncOutlined, FactCheckOutlined, GridViewOutlined, ExpandMore } from '@mui/icons-material';
import type { NavigationModule } from '@/lib/module-navigation';
import { activeModule, moduleGroups } from '@/lib/module-navigation';
const icons: Record<string, React.ReactNode> = {
    procesos_operativos: <AccountTreeOutlined fontSize="small" />, configuracion: <SettingsOutlined fontSize="small" />,
    sincronizaciones: <SyncOutlined fontSize="small" />, pruebas_acceso: <FactCheckOutlined fontSize="small" />,
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
            aria-current={selected?.key === module.key ? 'page' : undefined} onClick={() => visit(module)}>
            <ListItemIcon>{icons[module.key] || <GridViewOutlined fontSize="small" />}</ListItemIcon>
            <ListItemText>{module.name}</ListItemText>
        </MenuItem>;
    }
    return <Box component="nav" id={mobile ? 'application-navigation' : 'desktop-navigation'} aria-label="Módulos"
        sx={{ display: 'flex', flexDirection: mobile ? 'column' : 'row', gap: .5, p: mobile ? 2 : 0 }}>
        {groups.map(({ module, children }) => {
            const active = selected?.key === module.key || children.some(child => child.key === selected?.key);
            return <Button key={module.key} startIcon={icons[module.key] || <GridViewOutlined fontSize="small" />}
                endIcon={children.length > 0 ? <ExpandMore /> : undefined}
                aria-current={active ? children.length ? 'location' : 'page' : undefined}
                aria-haspopup={children.length ? 'menu' : undefined}
                aria-expanded={children.length ? menu?.key === module.key : undefined}
                aria-controls={menu?.key === module.key ? 'module-submenu' : undefined}
                onClick={event => children.length ? setMenu({ element: event.currentTarget, key: module.key }) : visit(module)}
                sx={{ justifyContent: mobile ? 'flex-start' : 'center', fontSize: 13,
                    bgcolor: active ? '#eaf1fc' : 'transparent', color: active ? 'primary.main' : 'text.secondary' }}>{module.name}</Button>;
        })}
        <Menu id="module-submenu" anchorEl={menu?.element} open={!!expanded} onClose={() => setMenu(null)}
            transitionDuration={reduced ? 0 : 180} slotProps={{ list: { 'aria-label': expanded?.module.name || 'Submódulos' } }}>
            {expanded && item(expanded.module)}
            <Divider />
            {expanded?.children.map(item)}
        </Menu>
    </Box>;
}
