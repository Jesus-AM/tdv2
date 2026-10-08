import { lazy, Suspense, useEffect, useState, useMemo } from 'react';
import { createRoot } from 'react-dom/client';
import { Alert, CssBaseline, LinearProgress, ThemeProvider, useMediaQuery, createTheme } from '@mui/material';
import { theme } from '@/theme';
import type { ComponentType } from 'react';
import { PageContext, startNavigation } from '@/lib/navigation';
import type { Page, SharedProps } from '@/types/page';
import { AccountPhotoProvider } from '@/Components/AccountPhoto';
import '../css/app.css';

const sources = import.meta.glob<{ default: ComponentType<SharedProps> }>('./pages/*.tsx');
const pages = Object.fromEntries(Object.entries(sources).map(([path, load]) => [path, lazy(load)]));
function App() {
    const [page, setPage] = useState<Page | null>(null);
    useEffect(() => startNavigation(setPage), []);
    if (!page) return <LinearProgress />;
    const Component = pages[`./pages/${page.component}.tsx`];
    if (!Component) return <Alert severity="error">Vista no disponible.</Alert>;
    return <PageContext.Provider value={page}>
        <AccountPhotoProvider context={page.props.photoContext}>
            <Suspense fallback={<LinearProgress />}><Component {...page.props} /></Suspense>
        </AccountPhotoProvider>
    </PageContext.Provider>;
}
function ThemedApp() {
    const reduced = useMediaQuery('(prefers-reduced-motion: reduce)');
    const activeTheme = useMemo(() => reduced ? createTheme(theme, {
        transitions: { duration: Object.fromEntries(Object.keys(theme.transitions.duration).map((key) => [key, 0])) },
        components: {
            MuiCollapse: { defaultProps: { timeout: 0 } }, MuiMenu: { defaultProps: { transitionDuration: 0 } },
            MuiDialog: { defaultProps: { transitionDuration: 0 } }, MuiDrawer: { defaultProps: { transitionDuration: 0 } },
        },
    }) : theme, [reduced]);
    return <ThemeProvider theme={activeTheme}><CssBaseline /><App /></ThemeProvider>;
}
createRoot(document.getElementById('app')!).render(<ThemedApp />);
