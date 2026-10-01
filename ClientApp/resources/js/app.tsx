import { lazy, Suspense, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { Alert, CssBaseline, LinearProgress, ThemeProvider } from '@mui/material';
import { theme } from '@/theme';
import type { ComponentType } from 'react';
import { PageContext, startNavigation } from '@/lib/navigation';
import type { Page, SharedProps } from '@/types/page';
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
        <Suspense fallback={<LinearProgress />}><Component {...page.props} /></Suspense>
    </PageContext.Provider>;
}
createRoot(document.getElementById('app')!).render(
    <ThemeProvider theme={theme}><CssBaseline /><App /></ThemeProvider>,
);
