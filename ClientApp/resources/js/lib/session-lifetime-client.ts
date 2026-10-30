import { SessionDeadline, type SessionTiming } from './session-deadline';
let ending = false;
export const sessionIsEnding = () => ending;

export function sessionLifetimeClient(end: () => void, token: () => string) {
    const deadline = new SessionDeadline(); let timer: ReturnType<typeof setTimeout> | undefined;
    let checking = false, lastCheck = -Infinity;
    let controller = new AbortController();
    const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel('tdv2-session-expiry');
    const finish = (broadcast = true) => {
        if (ending) return;
        ending = true; clearTimeout(timer); controller.abort();
        if (broadcast && deadline.id) {
            channel?.postMessage({ ended: deadline.id });
            try { localStorage.setItem('tdv2-ended-session', deadline.id); } catch { /* Canal efímero alternativo. */ }
        }
        end();
    };
    const configure = (info: SessionTiming) => {
        if (ending) return;
        const first = !deadline.id;
        if (deadline.configure(info, performance.now()) === 'changed') { finish(); return; }
        if (first && deadline.id) channel?.postMessage({ active: deadline.id });
        clearTimeout(timer);
        if (deadline.id) timer = setTimeout(() => void check(true), Math.min(2147483647, deadline.remaining(performance.now())));
    };
    const check = async (boundary = false) => {
        if (ending || checking || !deadline.id) return;
        if (!boundary && performance.now() - lastCheck < 30000) return;
        checking = true; lastCheck = performance.now();
        try {
            const response = await fetch(boundary ? '/session/expire' : '/session/status', {
                method: boundary ? 'POST' : 'GET', credentials: 'same-origin', cache: 'no-store',
                headers: { Accept: 'application/json', ...(boundary ? { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token() } : {}) },
                body: boundary ? JSON.stringify({ id: deadline.id }) : undefined,
                signal: AbortSignal.timeout(5000), keepalive: boundary,
            });
            if (response.status === 401 || response.status === 409) { finish(); return; }
            if (response.ok) {
                configure(await response.json());
                // Una confirmación en el borde no puede crear un ciclo de timers
                // a cero ni extender el plazo original recibido.
                if (boundary && deadline.remaining(performance.now()) === 0) finish();
            }
            else if (boundary) finish();
        } catch { if (boundary || deadline.remaining(performance.now()) === 0) finish(); }
        finally { checking = false; }
    };
    const visible = () => { if (document.visibilityState === 'visible') void check(deadline.remaining(performance.now()) === 0); };
    const pageshow = () => void check(deadline.remaining(performance.now()) === 0);
    const storage = (event: StorageEvent) => { if (deadline.id && event.key === 'tdv2-ended-session' && event.newValue === deadline.id) finish(false); };
    if (channel) channel.onmessage = event => { if (deadline.id && (event.data?.ended === deadline.id || event.data?.active && event.data.active !== deadline.id)) finish(false); };
    return {
        configure, finish,
        signal: () => controller.signal,
        id: () => deadline.id,
        blocked: () => {
            if (ending) return true;
            if (deadline.id && deadline.remaining(performance.now()) === 0) { void check(true); return true; }
            return false;
        },
        start: () => {
            document.addEventListener('visibilitychange', visible); window.addEventListener('pageshow', pageshow); window.addEventListener('storage', storage);
            return () => { clearTimeout(timer); channel?.close(); document.removeEventListener('visibilitychange', visible); window.removeEventListener('pageshow', pageshow); window.removeEventListener('storage', storage); };
        },
    };
}
