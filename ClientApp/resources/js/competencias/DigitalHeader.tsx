import { useLayoutEffect, useRef, useState } from 'react';
import { Button, Menu, MenuItem } from '@mui/material';
import { MenuOutlined } from '@mui/icons-material';
import AccountMenu from '@/Components/AccountMenu';
import { router, usePage } from '@/lib/navigation';
import { base } from './types';
import type { DigitalPage } from './types';

type Layout = 'full' | 'compact-account' | 'menu';

/** Sólo presenta los módulos ya autorizados y ordenados por el servidor. */
export default function DigitalHeader({ navigation, activePath, configurationActive, onExit, onPrivacy }: {
    navigation: DigitalPage['navigation']; activePath: string; configurationActive: boolean;
    onExit: () => void; onPrivacy: () => void;
}) {
    const { props } = usePage();
    const name = props.representacion?.nombre || props.auth.user?.name || '';
    const [layout, setLayout] = useState<Layout>('menu');
    const [anchor, setAnchor] = useState<HTMLElement | null>(null);
    const bar = useRef<HTMLDivElement>(null), logo = useRef<HTMLImageElement>(null);
    const links = useRef<HTMLDivElement>(null), account = useRef<HTMLDivElement>(null), nameMeasure = useRef<HTMLSpanElement>(null);
    const trigger = useRef<HTMLButtonElement>(null), mode = useRef<Layout>('menu');
    const selected = (path: string) => activePath === path || configurationActive && path === base + '/configuracion';

    useLayoutEffect(() => {
        let alive = true, frame = 0;
        const measure = () => {
            const container = bar.current, image = logo.current, list = links.current, owner = account.current, label = nameMeasure.current;
            if (!alive || !container || !image || !list || !owner || !label) return;
            const style = getComputedStyle(container), gap = parseFloat(style.columnGap) || 16;
            const available = container.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
            const nameWidth = label.getBoundingClientRect().width;
            const ownerWidth = owner.getBoundingClientRect().width;
            const compactWidth = mode.current === 'full' ? ownerWidth - nameWidth - 8 : ownerWidth;
            const required = image.getBoundingClientRect().width + list.getBoundingClientRect().width + compactWidth + 2 * gap + 12;
            const next: Layout = container.clientWidth < 960 || required > available ? 'menu'
                : required + nameWidth + 8 <= available ? 'full' : 'compact-account';
            if (next === mode.current) return;
            const focusedLink = next === 'menu' && list.contains(document.activeElement);
            mode.current = next; setLayout(next);
            if (focusedLink) requestAnimationFrame(() => trigger.current?.focus());
        };
        const schedule = () => { cancelAnimationFrame(frame); frame = requestAnimationFrame(measure); };
        const observer = new ResizeObserver(schedule);
        for (const element of [bar.current, logo.current, links.current, account.current, nameMeasure.current]) if (element) observer.observe(element);
        window.addEventListener('resize', schedule);
        void document.fonts.ready.then(() => { if (alive) schedule(); });
        measure();
        return () => { alive = false; observer.disconnect(); cancelAnimationFrame(frame); window.removeEventListener('resize', schedule); };
    }, [navigation, name]);

    useLayoutEffect(() => {
        if (layout !== 'menu' && anchor) {
            setAnchor(null);
            requestAnimationFrame(() => links.current?.querySelector<HTMLButtonElement>('button[aria-current],button')?.focus());
        }
    }, [layout, anchor]);

    const open = layout === 'menu' && !!anchor;
    return <div className="cd-wrap cd-header-inner" data-layout={layout} ref={bar}>
        <a className="cd-brandmark" href="/" onClick={event => { event.preventDefault(); router.visit('/'); }} aria-label="Volver al portal de Transformación Digital">
            <img ref={logo} src="/images/logos/transformacion-digital-blanco.svg" alt="Transformación Digital UACJ" />
        </a>
        <nav className="cd-nav" aria-label="Módulos de Competencias digitales" aria-hidden={layout === 'menu' || undefined} inert={layout === 'menu' || undefined}>
            <div className="cd-nav-links" ref={links}>{navigation.map(item => <button key={item.path} tabIndex={layout === 'menu' ? -1 : undefined}
                aria-current={selected(item.path) ? 'page' : undefined} onClick={() => router.visit(item.path)}>{item.name}</button>)}</div>
        </nav>
        <Button ref={trigger} id="cd-module-trigger" className="cd-module-trigger" color="inherit" aria-label="Abrir navegación de Competencias digitales"
            aria-haspopup="menu" aria-expanded={open} aria-controls={open ? 'cd-module-menu' : undefined} onClick={event => setAnchor(event.currentTarget)}>
            <MenuOutlined />
        </Button>
        <div className="cd-account" ref={account}><AccountMenu includeNameInLabel onExit={onExit} onPrivacy={onPrivacy} /></div>
        <div className="cd-header-measure" aria-hidden="true"><span className="cd-account-name-measure" ref={nameMeasure}>{name}</span></div>
        <Menu id="cd-module-menu" className="cd-module-menu" anchorEl={anchor} open={open} onClose={() => setAnchor(null)} disableScrollLock
            slotProps={{ list: { 'aria-labelledby': 'cd-module-trigger' }, paper: { sx: { width: 340, maxWidth: 'calc(100vw - 24px)' } } }}>
            {navigation.map(item => <MenuItem key={item.path} selected={selected(item.path)} aria-current={selected(item.path) ? 'page' : undefined}
                onClick={() => { setAnchor(null); router.visit(item.path); }}>{item.name}</MenuItem>)}
        </Menu>
    </div>;
}
