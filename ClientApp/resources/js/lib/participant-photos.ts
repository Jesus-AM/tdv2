export type PhotoReply = { photo: string | null; renuevaEn: string; vigenteHasta?: string | null; servidorEn?: string | null;
    estado?: 'actual' | 'conservada' | 'ausente' | 'temporal' | 'no_disponible'; contexto?: string | null };
type Entry = { photo: string | null; until: number; validUntil: number; failures: number; generation: number;
    listeners: Set<() => void>; pending?: Promise<void>; controller?: AbortController;
    timer?: ReturnType<typeof setTimeout>; expiry?: ReturnType<typeof setTimeout> };

/** Sólo memoria: una consulta por identidad/contexto; nunca fotos o credenciales en almacenamiento web. */
export class ParticipantPhotoStore {
    private entries = new Map<string, Entry>();
    private disposed = false;
    private invalidated = false;
    constructor(private fetch: (url: string, signal?: AbortSignal) => Promise<PhotoReply>, private accountContext?: string) {}
    photo(url?: string | null) {
        const entry = url ? this.entries.get(url) : undefined;
        return entry && entry.validUntil > Date.now() ? entry.photo : null;
    }
    subscribe(url: string | null | undefined, listener: () => void) {
        if (!url || this.disposed || !(this.accountContext && url === '/user/photo') && !/^\/formatos\/[^/]+\/participantes\/[A-F0-9]{64}\/foto$/.test(url)) return () => {};
        if (this.entries.size >= 128) for (const [key, old] of this.entries) {
            if (!old.listeners.size && key !== url) { this.stop(old); this.entries.delete(key); }
        }
        let entry = this.entries.get(url);
        if (!entry) { entry = { photo: null, until: 0, validUntil: 0, failures: 0, generation: 0, listeners: new Set() }; this.entries.set(url, entry); }
        entry.listeners.add(listener); this.schedule(url, entry);
        return () => {
            entry.listeners.delete(listener);
            if (!entry.listeners.size) { clearTimeout(entry.timer); clearTimeout(entry.expiry); }
        };
    }
    private notify(entry: Entry) { for (const listener of entry.listeners) listener(); }
    private schedule(url: string, entry: Entry) {
        clearTimeout(entry.timer); clearTimeout(entry.expiry);
        if (this.disposed || this.invalidated || !entry.listeners.size) return;
        if (entry.photo && entry.validUntil <= Date.now()) { entry.photo = null; this.notify(entry); }
        if (entry.photo) entry.expiry = setTimeout(() => {
            entry.photo = null; this.notify(entry);
        }, Math.max(0, entry.validUntil - Date.now()));
        if (entry.pending) return;
        if (entry.until <= Date.now()) { void this.load(url, entry); return; }
        entry.timer = setTimeout(() => { entry.timer = undefined; void this.load(url, entry); }, entry.until - Date.now());
    }
    private async load(url: string, entry: Entry) {
        if (entry.pending || this.disposed || !entry.listeners.size) return;
        const generation = entry.generation;
        entry.controller = new AbortController();
        entry.pending = (async () => {
            try {
                const response = await this.fetch(url, entry.controller!.signal);
                if (this.disposed || generation !== entry.generation) return;
                if (this.accountContext && response.contexto !== this.accountContext) throw { response: { status: 409 } };
                const now = Date.now(), server = Date.parse(response.servidorEn || '') || now;
                const remaining = response.vigenteHasta ? Date.parse(response.vigenteHasta) - server : 900000;
                entry.validUntil = now + Math.max(0, Math.min(3600000, remaining));
                entry.until = now + Math.max(1000, Math.min(900000, Date.parse(response.renuevaEn) - server || 120000));
                entry.failures = 0;
                const photo = entry.validUntil > now ? response.photo : null;
                if (entry.photo !== photo) { entry.photo = photo; this.notify(entry); }
            } catch (error) {
                if (this.disposed || generation !== entry.generation) return;
                const status = (error as { response?: { status?: number } })?.response?.status;
                // Microsoft comunica fallos temporales en un 200 tipado. Un rechazo de TDV2
                // (también Nexo/servidor sin autorización comprobable) nunca reutiliza la imagen.
                const temporary = !status || status === 408 || status === 429;
                if (!temporary || entry.validUntil <= Date.now()) {
                    if (entry.photo) { entry.photo = null; this.notify(entry); }
                }
                entry.failures = Math.min(10, entry.failures + 1);
                entry.until = Date.now() + (temporary ? Math.min(900000, 30000 * 2 ** (entry.failures - 1)) : 120000);
            }
        })();
        try { await entry.pending; } finally {
            if (generation === entry.generation) { entry.pending = undefined; this.schedule(url, entry); }
        }
    }
    private stop(entry: Entry) {
        entry.generation++; entry.controller?.abort(); entry.pending = undefined;
        clearTimeout(entry.timer); clearTimeout(entry.expiry);
    }
    clear() {
        // Un cambio de identidad invalida este almacén definitivamente; sólo un contexto nuevo
        // puede volver a consultar. Una redirección lenta no debe resucitar la cuenta anterior.
        this.invalidated = true;
        for (const [url, entry] of this.entries) {
            this.stop(entry); entry.photo = null; entry.validUntil = 0; entry.until = Date.now() + 30000;
            this.notify(entry); this.schedule(url, entry);
        }
    }
    dispose() { this.disposed = true; for (const entry of this.entries.values()) this.stop(entry); this.entries.clear(); }
}
