import type { FormContent, SaveResponse } from '../types/tdv2';
import { normalizeContent } from './form-editor';
import { applyBlocks, changedBlocks, sameBlock, splitBlocks } from './form-blocks';
export type Lease = { id: string | null; propia: boolean; titular: string; venceEn: string };
export type Block = { key: string; version: number; reserva: Lease | null; value?: unknown };
export type LiveForm = SaveResponse & { contenido: FormContent; bloques: Block[]; editable: boolean; puedeEnviar: boolean; enviadoEn: string | null; seccion: string; servidorEn: string };
export type Mutation = { tabId: string; operationId: string; blocks: { key: string; version: number; leaseId?: string | null; value?: unknown }[]; section: string; release: boolean };
export interface EditingTransport {
    read(): Promise<LiveForm>;
    reserve(body: Mutation): Promise<{ bloques: Block[]; servidorEn: string }>;
    renew(body: Mutation): Promise<{ bloques: Block[]; servidorEn: string }>;
    save(body: Mutation): Promise<SaveResponse & { bloques: Block[] }>;
    release(body: Mutation): Promise<unknown>;
    submit(body: { tabId: string; operationId: string; version: number }): Promise<{ version: number; enviadoEn: string }>;
}
export interface EditorState {
    content: FormContent; version: number; progress: number; dirty: boolean; saving: boolean; locked: boolean;
    error: string; updatedBy: string | null; updatedAt: string | null; conflict: boolean;
    blocks: Record<string, Block>; initialized: boolean; submitted: string | null; canSubmit: boolean;
    section: string; conflicts: string[];
}

/** Mantiene una propuesta por bloque. Una respuesta antigua nunca reemplaza la escritura posterior del usuario. */
export class BlockEditor {
    readonly tabId = crypto.randomUUID(); // No sessionStorage: duplicar una pestaña debe crear otra identidad.
    state: EditorState;
    private base: FormContent;
    private latest?: LiveForm;
    private timer?: ReturnType<typeof setTimeout>;
    private disposed = false;
    private running?: Promise<boolean>;
    private acquiring?: Promise<void>;
    private pending?: Mutation;
    private submitting?: { tabId: string; operationId: string; version: number };
    private serverOffset = 0;
    private serverTimestamp = 0;
    private connectionIssue = false;
    private sectionChosen = false;
    private lastActivity = new Map<string, number>();
    constructor(content: FormContent, version: number, progress: number, editable: boolean,
        private transport: EditingTransport, private notify: (state: EditorState) => void) {
        this.base = normalizeContent(content);
        this.state = { content: structuredClone(this.base), version, progress, dirty: false, saving: false, locked: !editable,
            error: '', updatedBy: null, updatedAt: null, conflict: false, blocks: {}, initialized: false, submitted: null,
            canSubmit: false, section: 'contexto', conflicts: [] };
    }
    private emit() {
        this.state.dirty = changedBlocks(this.base, this.state.content).length > 0;
        this.state.conflict = this.state.conflicts.length > 0;
        if (!this.disposed) this.notify({ ...this.state, blocks: { ...this.state.blocks } });
    }
    private request(keys: string[], release = false): Mutation {
        return { tabId: this.tabId, operationId: crypto.randomUUID(), section: this.state.section, release,
            blocks: keys.map(key => ({ key, version: this.state.blocks[key]?.version ?? 0, leaseId: this.state.blocks[key]?.reserva?.id })) };
    }
    private active(lease?: Lease | null) { return !!lease && Date.parse(lease.venceEn) > Date.now() + this.serverOffset; }
    busy(key: string) { const lease = this.state.blocks[key]?.reserva; return this.active(lease) && !lease!.propia; }
    async refresh() {
        try { this.receive(await this.transport.read()); }
        catch (error) { this.failure(error); }
    }
    receive(data: LiveForm) {
        if (this.disposed || data.version < this.state.version || Date.parse(data.servidorEn) < this.serverTimestamp) return;
        this.latest = data;
        if (this.state.saving) return;
        const dirty = changedBlocks(this.base, this.state.content), proposed = splitBlocks(this.state.content), before = splitBlocks(this.base);
        const remote = normalizeContent(data.contenido), blocks = Object.fromEntries(data.bloques.map(block => [block.key, block]));
        for (const key of dirty) {
            if ((blocks[key]?.version ?? 0) !== (this.state.blocks[key]?.version ?? 0) && !this.state.conflicts.includes(key)) this.state.conflicts.push(key);
            if (blocks[key]) blocks[key].version = this.state.blocks[key]?.version ?? 0;
        }
        this.base = applyBlocks(remote, Object.fromEntries(dirty.map(key => [key, before[key] ?? null])));
        this.state.content = applyBlocks(remote, Object.fromEntries(dirty.map(key => [key, proposed[key] ?? null])));
        this.state.blocks = blocks; this.state.version = data.version; this.state.progress = data.porcentaje;
        this.state.updatedAt = data.actualizadoEn; this.state.updatedBy = data.actualizadoPor;
        this.state.locked = !data.editable; this.state.canSubmit = data.puedeEnviar; this.state.submitted = data.enviadoEn;
        this.serverOffset = Date.parse(data.servidorEn) - Date.now();
        this.serverTimestamp = Date.parse(data.servidorEn);
        if (!this.state.initialized && !this.sectionChosen) this.state.section = data.seccion || 'contexto';
        this.state.initialized = true;
        if (this.connectionIssue) { this.state.error = ''; this.connectionIssue = false; }
        if (this.state.conflicts.length) this.state.error = 'Un bloque cambió en otra sesión. Tu propuesta se conserva; compárala con la versión compartida antes de recuperarla.';
        this.emit();
    }
    connectionLost() {
        if (!this.disposed && (!this.state.error || this.connectionIssue)) {
            this.connectionIssue = true;
            this.state.error = 'Se perdió la conexión de colaboración. Tus cambios pendientes se conservan. Actualiza el estado para continuar.'; this.emit();
        }
    }
    private failure(error: unknown, keys: string[] = []) {
        this.connectionIssue = false;
        const response = (error as { response?: { status: number; data?: { message?: string; errors?: Record<string, string[]> } } }).response;
        if ([401, 403, 419].includes(response?.status ?? 0)) this.state.locked = true;
        if (response?.status === 409) this.state.conflicts = [...new Set([...this.state.conflicts, ...keys])];
        this.state.error = response?.data?.message || Object.values(response?.data?.errors || {}).flat()[0] || 'No se pudo guardar. Tu propuesta permanece en esta página; puedes reintentar o descargarla.';
        this.emit();
    }
    private async ensure(keys: string[]) {
        if (this.acquiring) await this.acquiring;
        if (this.disposed) throw new Error('El editor terminó.');
        const needed = keys.filter(key => !this.active(this.state.blocks[key]?.reserva) || !this.state.blocks[key]?.reserva?.propia);
        if (!needed.length) return;
        this.acquiring = (async () => {
            const result = await this.transport.reserve(this.request(needed));
            this.serverOffset = Date.parse(result.servidorEn) - Date.now();
            this.serverTimestamp = Date.parse(result.servidorEn);
            for (const block of result.bloques) this.state.blocks[block.key] = block;
            this.emit();
        })();
        try { await this.acquiring; } finally { this.acquiring = undefined; }
    }
    async focus(key: string) {
        if (this.disposed || this.state.locked || !this.state.initialized || this.state.conflicts.includes(key) || this.busy(key)) return;
        try { await this.ensure([key]); } catch (error) { this.failure(error, [key]); }
    }
    change(edit: (content: FormContent) => void) {
        if (this.state.locked || !this.state.initialized || this.disposed) return;
        const next = structuredClone(this.state.content); edit(next);
        const keys = changedBlocks(this.state.content, next);
        if (keys.some(key => this.busy(key))) { this.state.error = 'Otra sesión está editando un bloque necesario para este cambio.'; this.emit(); return; }
        this.state.content = next; this.emit();
        // Sólo una interacción que cambia respuestas renueva la reserva. No hay latido de pestaña abierta.
        for (const key of keys) {
            const lease = this.state.blocks[key]?.reserva;
            if (lease?.propia && this.active(lease) && Date.now() - (this.lastActivity.get(key) || 0) > 15000) {
                this.lastActivity.set(key, Date.now());
                const renewal = this.request([key]);
                void this.transport.renew(renewal).then(result => {
                    for (const block of result.bloques) {
                        const current = this.state.blocks[block.key];
                        if (current?.reserva?.id === block.reserva?.id && current.version === block.version) current.reserva = block.reserva;
                    }
                    this.emit();
                }).catch(error => {
                    const current = this.state.blocks[key];
                    // Una renovación anterior al guardado no puede convertir su confirmación en conflicto.
                    if (current?.version === renewal.blocks[0].version && current.reserva?.id === renewal.blocks[0].leaseId)
                        this.failure(error, [key]);
                });
            }
        }
        clearTimeout(this.timer);
        if (!this.state.conflicts.length && !this.pending) this.timer = setTimeout(() => void this.flush(), 1000);
    }
    setSection(section: string) { this.sectionChosen = true; this.state.section = section; this.emit(); }
    async endBlock(key: string) {
        if (this.state.locked) return;
        if (changedBlocks(this.base, this.state.content).includes(key) && !await this.flush()) return;
        if (changedBlocks(this.base, this.state.content).includes(key)) return;
        const block = this.state.blocks[key];
        if (block?.reserva?.propia) {
            try { await this.transport.release(this.request([key])); block.reserva = null; this.emit(); }
            catch (error) { this.failure(error); }
        }
    }
    async flush(release = false): Promise<boolean> {
        if (this.running) { const result = await this.running; return result && this.state.dirty ? this.flush(release) : result; }
        if (this.state.locked || !this.state.initialized || this.disposed) return !this.state.dirty;
        clearTimeout(this.timer);
        this.running = this.save(release);
        try {
            const saved = await this.running;
            if (saved && release) await this.releaseClean();
            return saved;
        } finally { this.running = undefined; }
    }
    async releaseClean() {
        const dirty = changedBlocks(this.base, this.state.content);
        const keys = Object.values(this.state.blocks).filter(b => b.reserva?.propia && !dirty.includes(b.key)).map(b => b.key);
        if (!keys.length) return;
        const request = this.request(keys);
        try {
            await this.transport.release(request);
            for (const block of request.blocks) if (this.state.blocks[block.key]?.reserva?.id === block.leaseId) this.state.blocks[block.key].reserva = null;
            this.emit();
        } catch { /* El vencimiento resuelve cierres o desconexiones; no descartar propuestas. */ }
    }
    private async save(release: boolean): Promise<boolean> {
        const section = this.state.section;
        const keys = changedBlocks(this.base, this.state.content).filter(key => !this.state.conflicts.includes(key));
        if (!keys.length && !this.pending) return !this.state.dirty;
        this.state.saving = true; this.emit();
        try {
            if (!this.pending) {
                await this.ensure(keys);
                // Una navegación/cambio de representación puede terminar el editor mientras espera la reserva.
                // No enviar su propuesta con el contexto de la página que lo sustituyó.
                if (this.disposed) { void this.releaseClean(); return false; }
                const snapshot = splitBlocks(this.state.content);
                this.pending = this.request(keys, release);
                this.pending.section = section;
                for (const block of this.pending.blocks) block.value = snapshot[block.key] ?? null;
            }
            const sent = this.pending;
            const saved = await this.transport.save(sent);
            const current = splitBlocks(this.state.content), confirmed = Object.fromEntries(saved.bloques.map(block => [block.key, block.value ?? null]));
            this.base = applyBlocks(this.base, confirmed);
            this.state.content = applyBlocks(this.state.content, Object.fromEntries(saved.bloques
                .filter(block => sameBlock(current[block.key], sent.blocks.find(b => b.key === block.key)?.value))
                .map(block => [block.key, block.value ?? null])));
            for (const block of saved.bloques) this.state.blocks[block.key] = block;
            this.state.version = Math.max(this.state.version, saved.version); this.state.progress = saved.porcentaje;
            this.state.updatedAt = saved.actualizadoEn; this.state.updatedBy = saved.actualizadoPor;
            this.pending = undefined; if (!this.state.conflicts.length) this.state.error = '';
            return true;
        } catch (error) {
            const status = (error as { response?: { status: number } }).response?.status;
            if (status && status < 500) this.pending = undefined;
            this.failure(error, keys); return false;
        } finally {
            this.state.saving = false; this.emit();
            const latest = this.latest;
            if (latest && latest.version > this.state.version) this.receive(latest);
            if (this.state.dirty && !this.state.error && !this.disposed) this.timer = setTimeout(() => void this.flush(), 300);
        }
    }
    /** Recuperación explícita: el usuario ya comparó su propuesta con el contenido compartido. */
    async recover(comparedVersion: number) {
        const proposal = splitBlocks(this.state.content), keys = changedBlocks(this.base, this.state.content);
        const latest = await this.transport.read();
        if (latest.version !== comparedVersion) throw new Error('La versión compartida volvió a cambiar. Compara de nuevo.');
        if (!latest.editable) { this.receive(latest); return; }
        this.pending = undefined; this.state.conflicts = []; this.state.error = '';
        this.base = normalizeContent(latest.contenido); this.state.content = applyBlocks(this.base, Object.fromEntries(keys.map(key => [key, proposal[key] ?? null])));
        this.state.blocks = Object.fromEntries(latest.bloques.map(block => [block.key, block])); this.state.version = latest.version;
        // Recuperar prepara una propuesta, nunca la guarda automáticamente.
        this.emit();
    }
    async submit(): Promise<boolean> {
        if (this.disposed || !this.state.canSubmit || !await this.flush(true) || this.disposed || this.state.dirty) return false;
        this.submitting ||= { tabId: this.tabId, operationId: crypto.randomUUID(), version: this.state.version };
        try {
            const result = await this.transport.submit(this.submitting);
            this.state.submitted = result.enviadoEn; this.state.version = result.version; this.state.locked = true; this.state.canSubmit = false;
            this.state.error = ''; this.emit(); return true;
        } catch (error) {
            if ((error as { response?: { status: number } }).response?.status! < 500) this.submitting = undefined;
            this.failure(error); return false;
        }
    }
    dispose() { this.disposed = true; clearTimeout(this.timer); void this.releaseClean(); }
}
