export type PhotoReply = { photo: string | null; renuevaEn: string };
type Entry = { photo: string | null; until: number; listeners: Set<() => void>; pending?: Promise<void>; timer?: ReturnType<typeof setTimeout> };

/** Una descarga por participante/contexto, sin almacenar fotografías en disco ni cachear autorización. */
export class ParticipantPhotoStore {
    private entries = new Map<string, Entry>();
    private disposed = false;
    constructor(private fetch: (url: string) => Promise<PhotoReply>) {}
    photo(url?: string | null) { return url ? this.entries.get(url)?.photo ?? null : null; }
    subscribe(url: string | null | undefined, listener: () => void) {
        if (!url || this.disposed || !/^\/formatos\/[^/]+\/participantes\/[A-F0-9]{64}\/foto$/.test(url)) return () => {};
        let entry = this.entries.get(url);
        if (!entry) { entry = { photo: null, until: 0, listeners: new Set() }; this.entries.set(url, entry); }
        entry.listeners.add(listener); this.schedule(url, entry);
        return () => {
            entry.listeners.delete(listener);
            if (!entry.listeners.size) { clearTimeout(entry.timer); entry.timer = undefined; }
        };
    }
    private schedule(url: string, entry: Entry) {
        clearTimeout(entry.timer);
        if (this.disposed || !entry.listeners.size || entry.pending) return;
        if (entry.until <= Date.now()) { void this.load(url, entry); return; }
        entry.timer = setTimeout(() => { entry.timer = undefined; void this.load(url, entry); }, entry.until - Date.now());
    }
    private async load(url: string, entry: Entry) {
        if (entry.pending || this.disposed || !entry.listeners.size) return;
        entry.pending = (async () => {
            let photo: string | null = null;
            try {
                const response = await this.fetch(url);
                photo = response.photo;
                entry.until = Math.max(Date.now() + 30000, Math.min(Date.now() + 900000, Date.parse(response.renuevaEn) || 0));
            } catch { entry.until = Date.now() + 120000; }
            if (this.disposed) return;
            if (entry.photo !== photo) { entry.photo = photo; for (const notify of entry.listeners) notify(); }
        })();
        try { await entry.pending; } finally { entry.pending = undefined; this.schedule(url, entry); }
    }
    dispose() { this.disposed = true; for (const entry of this.entries.values()) clearTimeout(entry.timer); this.entries.clear(); }
}
