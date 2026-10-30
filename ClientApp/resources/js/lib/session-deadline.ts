export interface SessionTiming { lifetime_ms: number; id?: string | null; started_at?: number; expires_at?: number; server_now?: number; idle_ms?: number; absolute_ms?: number; }
// Usa diferencias del reloj del servidor y un reloj monotónico local. Nunca una
// selección, un polling ni una actualización de página reinician el límite.
export class SessionDeadline {
    id = ''; private until = Infinity;
    configure(info: SessionTiming, monotonicNow: number): 'ok' | 'changed' {
        if (!info.id || info.expires_at === undefined || info.server_now === undefined) return 'ok';
        if (this.id && this.id !== info.id) return 'changed';
        const proposed = monotonicNow + Math.max(0, info.expires_at - info.server_now);
        this.until = this.id ? Math.min(this.until, proposed) : proposed;
        this.id = info.id;
        return 'ok';
    }
    remaining(now: number) { return Math.max(0, this.until - now); }
}
