import { createContext, useContext, useEffect, useState } from 'react';
import axios from 'axios';
import type { Page } from '@/types/page';

export const PageContext = createContext<Page | null>(null);
export function usePage(): Page {
    const page = useContext(PageContext);
    if (!page) throw new Error('No hay una página cargada.');
    return page;
}
export function Head({ title }: { title: string }) {
    useEffect(() => { document.title = `${title} · Transformación Digital`; }, [title]);
    return null;
}
type Errors = Record<string, string>;
type Options = { only?: string[]; onError?: (errors: Errors) => void; onFinish?: () => void; onSuccess?: () => void };
type Before = CustomEvent<{ visit: { headers: Record<string, string> } }>;
const events = new EventTarget();
let current: Page | null = null;
let csrf = '';
let sequence = 0;
let publish: (page: Page) => void = () => {};

// Identity and permissions never come from this transport. Context only rejects stale writes.
axios.interceptors.request.use(config => {
    config.headers.set('X-TDV2-Context', current?.props.contextoEdicion || 'own');
    if (!['get', 'head', 'options'].includes(config.method || 'get')) config.headers.set('X-CSRF-TOKEN', csrf);
    return config;
});
function localPath(path: string) {
    const target = new URL(path, window.location.origin);
    if (target.origin !== window.location.origin) throw new Error('El destino de navegación no pertenece a TDV2.');
    return target.pathname + target.search + target.hash;
}
function setPage(page: Page) {
    current = page; csrf = page.props.csrfToken || csrf; publish(page);
}
function restricted(message: string, status: number, url: string, context?: Partial<Page['props']>): Page {
    return { component: 'AccesoRestringido', url, props: {
        name: 'Transformación Digital', auth: { user: null, roles: [], modules: [], canPreview: false, canRepresent: false },
        simulacion: context?.simulacion || null, representacion: context?.representacion || null,
        contextoEdicion: context?.contextoEdicion || 'own', csrfToken: csrf,
        session: { lifetime_ms: 7200000 }, routes: { inicio: '/inicio', logout: '/logout', home: '/', connect: '/connect' },
        access: { message, status },
    } };
}
function before(): boolean {
    return events.dispatchEvent(new CustomEvent('before', { cancelable: true, detail: { visit: { headers: {} } } }));
}
async function visit(path: string, options: Options = {}, historyMode: 'push' | 'replace' = 'push', check = true) {
    if (check && !before()) { options.onFinish?.(); return; }
    const ticket = ++sequence;
    try {
        const url = localPath(path);
        // También /connect?account=other requiere navegación completa; OAuth no es una página JSON.
        const pathname = new URL(url, window.location.origin).pathname;
        if (pathname === '/' || pathname === '/connect' || pathname === '/session/microsoft-logout') { window.location.assign(url); return; }
        const { data } = await axios.get<Page>(url, { headers: { 'X-TDV2-Page': '1', Accept: 'application/json' } });
        if (ticket !== sequence) return;
        if (!data.component || !data.props) throw new Error('Respuesta de página no válida.');
        // Reload uses the same page instance so form drafts and sync settings survive polling.
        const page = options.only && current?.component === data.component
            ? { ...data, props: { ...current.props, ...Object.fromEntries(Object.entries(data.props).filter(([key]) =>
                options.only!.includes(key) || ['auth', 'simulacion', 'representacion', 'contextoEdicion', 'photoContext', 'csrfToken', 'session', 'routes'].includes(key))) } }
            : data;
        window.history[historyMode === 'push' ? 'pushState' : 'replaceState'](null, '', url);
        setPage(page);
        options.onSuccess?.();
    } catch (error) {
        if (ticket !== sequence) return;
        const status = axios.isAxiosError(error) ? error.response?.status || 503 : 503;
        const message = axios.isAxiosError(error) ? error.response?.data?.message : null;
        const context = axios.isAxiosError(error) ? error.response?.data : undefined;
        // Un fallo temporal al refrescar conserva el borrador. La denegación de acceso sí sustituye la vista.
        if (options.only && options.onError && status >= 500) {
            options.onError({ general: message || 'No se pudo actualizar el estado. Inténtalo de nuevo.' });
            return;
        }
        setPage(restricted(message || 'No se pudo cargar la página. Inténtalo de nuevo.', status, path, context));
    } finally { options.onFinish?.(); }
}
async function mutate(method: 'post' | 'delete', path: string, data: unknown, options: Options) {
    if (!before()) { options.onFinish?.(); return; }
    if (['/logout', '/session/use-another-account', '/actuar-como-usuario', '/vista-prueba'].includes(localPath(path)))
        events.dispatchEvent(new Event('identity-changing'));
    try {
        if (!csrf) csrf = (await axios.get<{ token: string }>('/session/csrf')).data.token;
        const response = await axios.request({ method, url: localPath(path), data, headers: { Accept: 'application/json' } });
        if (response.data.redirect) await visit(response.data.redirect, {}, 'replace', false);
        else await visit(window.location.pathname, {}, 'replace', false);
    } catch (error) {
        const payload = axios.isAxiosError(error) ? error.response?.data : null;
        const errors: Record<string, string | string[]> = payload?.errors || payload?.details?.errors || { general: payload?.message || 'No se pudo completar la operación.' };
        const normalized = Object.fromEntries(Object.entries(errors).map(([key, value]) => [key, Array.isArray(value) ? value[0] : value]));
        if (options.onError) options.onError(normalized);
        else window.alert(Object.values(normalized)[0]);
    } finally { options.onFinish?.(); }
}
export const router = {
    visit: (path: string) => { void visit(path); },
    reload: (options: Options = {}) => { void visit(window.location.pathname + window.location.search, options, 'replace', !options.only); },
    post: (path: string, data: unknown, options: Options = {}) => { void mutate('post', path, data, options); },
    delete: (path: string, options: Options = {}) => { void mutate('delete', path, undefined, options); },
    // History contains URLs only, never cached page data, tokens or personal information.
    clearHistory: () => { window.history.replaceState(null, '', window.location.href); },
    on: (name: 'before' | 'identity-changing', callback: (event: Before) => void) => {
        const handler = callback as EventListener;
        events.addEventListener(name, handler);
        return () => events.removeEventListener(name, handler);
    },
};
export function startNavigation(onPage: (page: Page) => void) {
    publish = onPage;
    const pop = () => {
        if (before()) void visit(window.location.pathname + window.location.search, {}, 'replace', false);
        else if (current) window.history.pushState(null, '', current.url);
    };
    window.addEventListener('popstate', pop);
    void visit(window.location.pathname + window.location.search, {}, 'replace', false);
    return () => window.removeEventListener('popstate', pop);
}
export function useForm<T extends Record<string, unknown>>(initial: T) {
    const [data, set] = useState(initial);
    const [errors, setErrors] = useState<Errors>({});
    const [processing, setProcessing] = useState(false);
    function setData(value: T): void;
    function setData<K extends keyof T>(key: K, value: T[K]): void;
    function setData<K extends keyof T>(value: T | K, next?: T[K]) {
        if (typeof value === 'object') set(value);
        else set(previous => ({ ...previous, [value]: next }));
    }
    return { data, setData, errors, processing, post: (path: string) => {
        if (processing) return;
        setProcessing(true); setErrors({});
        router.post(path, data, { onError: setErrors, onFinish: () => setProcessing(false) });
    } };
}
