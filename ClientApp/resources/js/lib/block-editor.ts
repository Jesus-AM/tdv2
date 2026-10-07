import type { FormContent, SaveResponse } from '../types/tdv2';
import { normalizeContent } from './form-editor';
import { applyBlocks, blockValue, changedBlocks, sameBlock, splitBlocks } from './form-blocks';
import type { SubmissionReview } from '../Components/FormSubmissionReview';
export type Lease = { id: string | null; propia: boolean; titular: string; venceEn: string; otraPestana?: boolean; color?: number; foto?: string | null };
export type Block = { key: string; version: number; reserva: Lease | null; value?: unknown };
export type LiveForm = SaveResponse & { contenido: FormContent; bloques: Block[]; editable: boolean; puedeEnviar: boolean; enviadoEn: string | null; enviadoPor: string | null; revisionEnvio: SubmissionReview | null; seccion: string; servidorEn: string };
export type Mutation = { tabId: string; operationId: string; blocks: { key: string; version: number; leaseId?: string | null; value?: unknown }[]; section: string; release: boolean };
export interface EditingTransport {
    read(): Promise<LiveForm>;
    reserve(body: Mutation): Promise<{ bloques: Block[]; servidorEn: string }>;
    renew(body: Mutation): Promise<{ bloques: Block[]; servidorEn: string }>;
    save(body: Mutation): Promise<SaveResponse & { bloques: Block[]; revisionEnvio: SubmissionReview }>;
    release(body: Mutation): Promise<unknown>;
    submit(body: { tabId: string; operationId: string; version: number }): Promise<{ version: number; enviadoEn: string; enviadoPor: string }>;
}
export interface EditorState {
    content: FormContent; version: number; progress: number; dirty: boolean; saving: boolean; locked: boolean;
    error: string; updatedBy: string | null; updatedAt: string | null; conflict: boolean;
    blocks: Record<string, Block>; initialized: boolean; submitted: string | null; canSubmit: boolean;
    section: string; conflicts: string[];
    review: SubmissionReview | null; submittedBy: string | null;
    preparing: string[]; issues: Record<string, string>;
}

/** Mantiene una propuesta por bloque. Una respuesta antigua nunca reemplaza la escritura posterior del usuario. */
export class BlockEditor {
    readonly tabId = crypto.randomUUID(); // No sessionStorage: duplicar una pestaña debe crear otra identidad.
    state: EditorState;
    private base: FormContent;
    private latest?: LiveForm;
    private timer?: ReturnType<typeof setTimeout>;
    private expiration?: ReturnType<typeof setTimeout>;
    // SignalR informa; sólo una adquisición confirmada habilita la primera escritura.
    private confirmed = new Map<string, string>();
    private groups = new Map<string, string[]>();
    private disposed = false;
    private running?: Promise<boolean>;
    private acquiring?: Promise<void>;
    private releasing = new Map<string, Promise<void>>();
    private pending?: Mutation;
    private submitting?: { tabId: string; operationId: string; version: number };
    private serverOffset = 0;
    private serverTimestamp = 0;
    private connectionIssue = false;
    private connectionEpoch = 0;
    private submissionIssue = false;
    private sectionChosen = false;
    private lastActivity = new Map<string, number>();
    private focusEpoch = new Map<string, number>();
    private focused = new Set<string>();
    private dirtyBase?: FormContent;
    private dirtyContent?: FormContent;
    private dirtyBlocks: string[] = [];
    private publishedBlocks: Record<string, Block> = {};
    constructor(content: FormContent, version: number, progress: number, editable: boolean,
        private transport: EditingTransport, private notify: (state: EditorState) => void) {
        this.base = normalizeContent(content);
        this.state = { content: this.base, version, progress, dirty: false, saving: false, locked: !editable,
            error: '', updatedBy: null, updatedAt: null, conflict: false, blocks: {}, initialized: false, submitted: null,
            canSubmit: false, section: 'contexto', conflicts: [], review: null, submittedBy: null, preparing: [], issues: {} };
    }
    private dirtyKeys() {
        if (this.dirtyBase !== this.base || this.dirtyContent !== this.state.content) {
            this.dirtyBlocks = changedBlocks(this.base, this.state.content);
            this.dirtyBase = this.base; this.dirtyContent = this.state.content;
        }
        return this.dirtyBlocks;
    }
    private emit(leasesChanged = true) {
        this.state.dirty = this.dirtyKeys().length > 0;
        this.state.conflict = this.state.conflicts.length > 0;
        if (leasesChanged) {
            clearTimeout(this.expiration);
            const remaining = Object.values(this.state.blocks).map(b => Date.parse(b.reserva?.venceEn || '') - Date.now() - this.serverOffset).filter(n => n > 0);
            if (!this.disposed && remaining.length) this.expiration = setTimeout(() => {
                for (const key of this.dirtyKeys()) if (!this.owned(key)) this.lost(key);
                this.emit();
            }, Math.min(...remaining) + 10);
            this.publishedBlocks = { ...this.state.blocks };
        }
        if (!this.disposed) this.notify({ ...this.state, blocks: this.publishedBlocks });
    }
    private request(keys: string[], release = false): Mutation {
        return { tabId: this.tabId, operationId: crypto.randomUUID(), section: this.state.section, release,
            blocks: keys.map(key => ({ key, version: this.state.blocks[key]?.version ?? 0, leaseId: this.state.blocks[key]?.reserva?.id })) };
    }
    private active(lease?: Lease | null) { return !!lease && Date.parse(lease.venceEn) > Date.now() + this.serverOffset; }
    busy(key: string) { const lease = this.state.blocks[key]?.reserva; return this.active(lease) && !lease!.propia; }
    private owned(key: string) {
        const lease = this.state.blocks[key]?.reserva;
        return !!lease?.propia && this.active(lease) && this.confirmed.get(key) === lease.id;
    }
    canEdit(key: string) {
        return !this.disposed && !this.state.locked && this.state.initialized && !this.connectionIssue
            && !this.state.preparing.includes(key) && !this.releasing.has(key) && !this.state.issues[key] && this.owned(key);
    }
    private lost(key: string) {
        this.confirmed.delete(key);
        this.state.issues[key] ||= 'La edición se interrumpió. Tus cambios sin confirmar se conservan.';
        if (!this.state.conflicts.includes(key)) this.state.conflicts.push(key);
    }
    proposalKeys(key: string) { return this.groups.get(key) || [key]; }
    proposalBase() { return this.base; }
    async refresh() {
        const epoch = this.connectionEpoch;
        try {
            this.receive(await this.transport.read());
            // Un mensaje en vuelo anterior a la desconexión no rehabilita la captura.
            // Sólo una consulta iniciada después de ella vuelve a confirmar el estado vigente.
            if (epoch === this.connectionEpoch && this.connectionIssue) {
                this.connectionIssue = false; this.state.error = ''; this.emit();
            }
            // Volver a consultar permite revisar y reintentar un envío rechazado sin tocar respuestas.
            if (this.submissionIssue && !this.state.dirty && !this.state.conflict) {
                this.submissionIssue = false; this.state.error = ''; this.emit();
            }
            return true;
        }
        catch (error) { this.failure(error); return false; }
    }
    receive(data: LiveForm) {
        if (this.disposed || data.version < this.state.version || Date.parse(data.servidorEn) < this.serverTimestamp) return;
        const first = !this.state.initialized;
        this.latest = data;
        if (this.state.saving) return;
        const previousContent = this.state.content;
        const signature = () => JSON.stringify({ ...this.state, content: undefined, blocks: this.state.blocks });
        const previousState = signature();
        const dirty = this.dirtyKeys();
        // La copia compartida debe conservar sus versiones: la propuesta local puede retener otras.
        const incoming = normalizeContent(data.contenido);
        const updates = Object.fromEntries(changedBlocks(this.base, incoming).filter(key => !dirty.includes(key))
            .map(key => [key, blockValue(incoming, key) ?? null]));
        const blocks = Object.fromEntries(data.bloques.map(block => [block.key, sameBlock(this.state.blocks[block.key], block) ? this.state.blocks[block.key] : structuredClone(block)]));
        for (const key of dirty) {
            if ((blocks[key]?.version ?? 0) !== (this.state.blocks[key]?.version ?? 0)) this.lost(key);
            if (blocks[key]) blocks[key].version = this.state.blocks[key]?.version ?? 0;
        }
        this.base = applyBlocks(this.base, updates);
        this.state.content = applyBlocks(this.state.content, updates);
        this.state.blocks = blocks; this.state.version = data.version; this.state.progress = data.porcentaje;
        this.state.updatedAt = data.actualizadoEn; this.state.updatedBy = data.actualizadoPor;
        this.state.locked = !data.editable; this.state.canSubmit = data.puedeEnviar; this.state.submitted = data.enviadoEn;
        this.state.review = data.revisionEnvio ?? null; this.state.submittedBy = data.enviadoPor ?? null;
        this.serverOffset = Date.parse(data.servidorEn) - Date.now();
        this.serverTimestamp = Date.parse(data.servidorEn);
        for (const key of dirty) if (!this.owned(key)) this.lost(key);
        for (const key of this.confirmed.keys()) if (!this.owned(key)) this.confirmed.delete(key);
        if (!this.state.initialized && !this.sectionChosen) this.state.section = data.seccion || 'contexto';
        this.state.initialized = true;
        for (const key of Object.keys(this.state.issues)) if (!dirty.includes(key)) delete this.state.issues[key];
        // El latido sí revalida acceso y caducidad; un estado equivalente no repinta los controles.
        if (previousContent !== this.state.content || previousState !== signature()) this.emit();
        // Un campo puede recibir foco mientras llega la lectura inicial. Retomar esa intención,
        // sin habilitarlo antes de que el servidor confirme el acceso y la reserva.
        if (first) for (const key of this.focused) void this.focus(key);
    }
    connectionLost() {
        if (!this.disposed) {
            this.connectionEpoch++;
            this.connectionIssue = true;
            for (const key of this.dirtyKeys())
                this.state.issues[key] ||= 'Se perdió la conexión. Tu propuesta permanece en este registro.';
            this.state.error = 'Reconectando la captura… Los cambios sin confirmar se conservan.'; this.emit();
        }
    }
    private failure(error: unknown, keys: string[] = []) {
        const response = (error as { response?: { status: number; data?: { message?: string; errors?: Record<string, string[]> } } }).response;
        if ([401, 403, 419].includes(response?.status ?? 0)) this.state.locked = true;
        if (response?.status === 409) for (const key of keys) this.lost(key);
        const message = response?.data?.message || Object.values(response?.data?.errors || {}).flat()[0] || 'No se pudo guardar. Tu propuesta se conserva.';
        if (keys.length) for (const key of keys) this.state.issues[key] = message;
        else this.state.error = message;
        this.emit();
    }
    private async ensure(keys: string[]) {
        const releases = keys.flatMap(key => this.releasing.has(key) ? [this.releasing.get(key)!] : []);
        if (releases.length) await Promise.all(releases);
        if (this.acquiring) await this.acquiring;
        if (this.disposed) throw new Error('El editor terminó.');
        if (this.state.locked || this.connectionIssue) throw new Error('No se pudo confirmar el acceso.');
        const needed = keys.filter(key => !this.owned(key));
        if (!needed.length) return;
        // Nunca volver a reservar silenciosamente una propuesta cuyo titular o versión se perdió.
        if (needed.some(key => this.dirtyKeys().includes(key))) throw new Error('Revisa la propuesta pendiente.');
        this.state.preparing = [...new Set([...this.state.preparing, ...needed])]; this.emit();
        this.acquiring = (async () => {
            const result = await this.transport.reserve(this.request(needed));
            if (needed.some(key => !result.bloques.some(block => block.key === key && 'value' in block && block.reserva?.propia && block.reserva.id)))
                throw new Error('No se pudo confirmar el contenido del registro.');
            this.serverOffset = Date.parse(result.servidorEn) - Date.now();
            this.serverTimestamp = Math.max(this.serverTimestamp, Date.parse(result.servidorEn));
            for (const block of result.bloques) {
                if (block.version < (this.state.blocks[block.key]?.version ?? 0)) continue;
                this.state.blocks[block.key] = block;
                if ('value' in block) {
                    this.base = applyBlocks(this.base, { [block.key]: block.value ?? null });
                    this.state.content = applyBlocks(this.state.content, { [block.key]: block.value ?? null });
                }
                if (block.reserva?.propia && this.active(block.reserva) && block.reserva.id) this.confirmed.set(block.key, block.reserva.id);
                delete this.state.issues[block.key];
            }
            this.emit();
        })();
        try { await this.acquiring; } finally { this.acquiring = undefined; this.state.preparing = this.state.preparing.filter(key => !needed.includes(key)); this.emit(); }
    }
    async prepare(keys: string[]): Promise<boolean> {
        if (this.disposed || this.state.locked || !this.state.initialized || keys.some(key => this.state.issues[key] || this.busy(key))) return false;
        try { await this.ensure(keys); return keys.every(key => this.canEdit(key)); }
        catch (error) {
            // Una carrera normal por la reserva se presenta en la fila, no como conflicto de guardado.
            if ((error as { response?: { status: number } }).response?.status === 409) {
                if (await this.refresh() && !keys.some(key => this.busy(key) || this.state.issues[key])) {
                    try { await this.ensure(keys); return keys.every(key => this.canEdit(key)); } catch { /* Releer muestra al titular ganador. */ }
                    await this.refresh();
                }
            } else this.failure(error, keys);
            return false;
        }
    }
    /** Altas y cambios con relaciones reservan todos sus bloques antes de modificar la propuesta. */
    async edit(edit: (content: FormContent) => void, stillValid: () => boolean = () => true) {
        if (!stillValid()) return false;
        const next = structuredClone(this.state.content); edit(next);
        const keys = changedBlocks(this.state.content, next);
        if (!await this.prepare(keys)) return false;
        // Una confirmación destructiva no autoriza contenido que llegó mientras se esperaba la reserva.
        if (!stillValid()) return false;
        const current = structuredClone(this.state.content); edit(current);
        const actual = changedBlocks(this.state.content, current);
        if (actual.some(key => !keys.includes(key))) return false;
        const pendingKeys = this.dirtyKeys();
        const group = [...new Set([...actual, ...actual.flatMap(key => this.groups.get(key) || []).filter(key => pendingKeys.includes(key))])];
        for (const key of group) this.groups.set(key, group);
        return this.change(edit);
    }
    async focus(key: string) {
        this.focused.add(key);
        const epoch = (this.focusEpoch.get(key) || 0) + 1;
        this.focusEpoch.set(key, epoch);
        // Un cambio de foco durante el guardado no debe pedir una reserva con la versión anterior.
        if (this.running) await this.running;
        if (this.focusEpoch.get(key) !== epoch) return false;
        if (this.disposed || this.state.locked || !this.state.initialized || this.state.conflicts.includes(key) || this.busy(key)) return false;
        const ready = await this.prepare([key]);
        // Una intención de abrir un selector o marcar una casilla no sobrevive a salir del registro.
        return ready && this.focusEpoch.get(key) === epoch && this.focused.has(key);
    }
    change(edit: (content: FormContent) => void) {
        if (this.state.locked || !this.state.initialized || this.disposed) return false;
        const next = structuredClone(this.state.content); edit(next);
        const keys = changedBlocks(this.state.content, next);
        if (keys.some(key => !this.canEdit(key))) return false;
        return this.commitChange(applyBlocks(this.state.content, Object.fromEntries(keys.map(key => [key, blockValue(next, key) ?? null]))), keys);
    }
    /** La escritura de un campo copia sólo su bloque; las operaciones con relaciones usan edit. */
    changeBlock<T extends object>(key: string, edit: (value: T) => void) {
        if (!this.canEdit(key)) return false;
        const value = structuredClone(blockValue(this.state.content, key)) as T;
        if (!value) return false;
        edit(value);
        const next = applyBlocks(this.state.content, { [key]: value });
        if (next === this.state.content) return true;
        return this.commitChange(next, [key]);
    }
    private commitChange(next: FormContent, keys: string[]) {
        const dirty = new Set(this.dirtyKeys());
        for (const key of keys) {
            if (sameBlock(blockValue(this.base, key), blockValue(next, key))) dirty.delete(key);
            else dirty.add(key);
        }
        // Por tecla sólo se compara el bloque cambiado. Volver al valor original también limpia su pendiente.
        this.state.content = next;
        this.dirtyBase = this.base; this.dirtyContent = next; this.dirtyBlocks = [...dirty];
        this.emit(false);
        // Sólo una interacción que cambia respuestas renueva la reserva. No hay latido de pestaña abierta.
        for (const key of keys) {
            const lease = this.state.blocks[key]?.reserva;
            if (lease?.propia && this.active(lease) && Date.now() - (this.lastActivity.get(key) || 0) > 15000) {
                this.lastActivity.set(key, Date.now());
                const renewal = this.request([key]);
                void this.transport.renew(renewal).then(result => {
                    this.serverTimestamp = Math.max(this.serverTimestamp, Date.parse(result.servidorEn) || 0);
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
        if (!this.pending) this.timer = setTimeout(() => void this.flush(), 1000);
        return true;
    }
    setSection(section: string) { this.sectionChosen = true; this.state.section = section; this.emit(); }
    async endBlock(key: string) {
        this.focused.delete(key);
        const epoch = (this.focusEpoch.get(key) || 0) + 1;
        this.focusEpoch.set(key, epoch);
        if (this.state.locked) return;
        // La respuesta de adquisición puede llegar después de salir del campo: esperar antes de liberar.
        if (this.acquiring) await this.acquiring.catch(() => {});
        if (this.dirtyKeys().includes(key) && !await this.flush()) return;
        if (this.focusEpoch.get(key) !== epoch || this.dirtyKeys().includes(key)) return;
        const block = this.state.blocks[key];
        if (block?.reserva?.propia) {
            try {
                await this.releaseKeys([key]);
            }
            catch (error) { this.failure(error); }
        }
    }
    async flush(release = false): Promise<boolean> {
        if (this.running) {
            const result = await this.running;
            if (result && this.state.dirty) return this.flush(release);
            if (result && release) await this.releaseClean();
            return result;
        }
        if (this.state.locked || !this.state.initialized || this.disposed) return !this.state.dirty;
        clearTimeout(this.timer);
        this.running = (async () => {
            const saved = await this.save(release);
            if (saved && release) await this.releaseClean();
            return saved;
        })();
        try { return await this.running; } finally { this.running = undefined; }
    }
    async releaseClean(selected?: string[]) {
        if (this.acquiring) await this.acquiring.catch(() => {});
        const dirty = this.dirtyKeys();
        const keys = Object.values(this.state.blocks).filter(b => b.reserva?.propia && !dirty.includes(b.key)
            && (!selected || selected.includes(b.key))).map(b => b.key);
        if (!keys.length) return;
        try {
            await this.releaseKeys(keys);
        } catch { /* El vencimiento resuelve cierres o desconexiones; no descartar propuestas. */ }
    }
    private async releaseKeys(keys: string[]) {
        const releases = keys.flatMap(key => this.releasing.has(key) ? [this.releasing.get(key)!] : []);
        if (releases.length) await Promise.all(releases);
        const request = this.request(keys);
        const release = this.transport.release(request).then(() => {
            for (const block of request.blocks) if (this.state.blocks[block.key]?.reserva?.id === block.leaseId) {
                this.state.blocks[block.key].reserva = null; this.confirmed.delete(block.key);
            }
        });
        // Volver al registro durante la liberación espera su confirmación antes de reservar de nuevo.
        for (const key of keys) { this.releasing.set(key, release); this.confirmed.delete(key); }
        this.emit();
        try { await release; } finally { for (const key of keys) this.releasing.delete(key); this.emit(); }
    }
    private async save(release: boolean): Promise<boolean> {
        const section = this.state.section;
        const keys = this.dirtyKeys().filter(key => !this.state.conflicts.includes(key) && !this.state.issues[key]);
        if (!keys.length && !this.pending) return !this.state.dirty;
        this.state.saving = true; this.emit();
        try {
            if (!this.pending) {
                if (keys.some(key => !this.canEdit(key))) { for (const key of keys) if (!this.canEdit(key)) this.lost(key); return false; }
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
            for (const block of saved.bloques) {
                this.state.blocks[block.key] = block;
                delete this.state.issues[block.key];
                this.state.conflicts = this.state.conflicts.filter(key => key !== block.key);
                if (!block.reserva?.propia) this.confirmed.delete(block.key);
            }
            this.state.version = Math.max(this.state.version, saved.version); this.state.progress = saved.porcentaje;
            this.state.updatedAt = saved.actualizadoEn; this.state.updatedBy = saved.actualizadoPor;
            this.state.review = saved.revisionEnvio ?? null;
            this.pending = undefined; if (!this.state.conflicts.length) this.state.error = '';
            return true;
        } catch (error) {
            const status = (error as { response?: { status: number } }).response?.status;
            if (status && status < 500) this.pending = undefined;
            this.failure(error, keys); return false;
        } finally {
            this.state.saving = false; this.emit();
            const latest = this.latest;
            // Un recibo idempotente puede confirmar una operación anterior a la versión que ya vimos.
            // Reaplicar también la misma versión global evita volver a mostrar aquel contenido antiguo.
            if (latest && latest.version >= this.state.version) this.receive(latest);
            if (this.state.dirty && !this.state.error && !Object.keys(this.state.issues).length && !this.disposed) this.timer = setTimeout(() => void this.flush(), 300);
        }
    }
    /** Recuperación explícita: el usuario ya comparó su propuesta con el contenido compartido. */
    async recover(comparedVersion: number, selected = this.dirtyKeys()) {
        // Resolver primero un resultado incierto usando el mismo identificador, nunca otro guardado equivalente.
        if (this.pending) { if (!await this.flush()) throw new Error('No se pudo confirmar el guardado. Conserva la propuesta y vuelve a intentarlo.'); return; }
        const proposal = splitBlocks(this.state.content), keys = this.dirtyKeys().filter(key => selected.includes(key));
        const latest = await this.transport.read();
        if (latest.version !== comparedVersion) throw new Error('La versión compartida volvió a cambiar. Compara de nuevo.');
        if (!latest.editable) { this.receive(latest); return; }
        if (latest.bloques.some(block => keys.includes(block.key) && block.reserva && !block.reserva.propia && Date.parse(block.reserva.venceEn) > Date.parse(latest.servidorEn)))
            throw new Error('Otra sesión sigue editando este registro. Tu propuesta se conserva.');
        const previousBase = this.base, previousContent = this.state.content;
        const shared = splitBlocks(normalizeContent(latest.contenido));
        this.base = applyBlocks(this.base, Object.fromEntries(keys.map(key => [key, shared[key] ?? null])));
        this.state.content = applyBlocks(this.state.content, Object.fromEntries(keys.map(key => [key, shared[key] ?? null])));
        for (const key of keys) { this.confirmed.delete(key); delete this.state.issues[key]; }
        this.state.conflicts = this.state.conflicts.filter(key => !keys.includes(key)); this.receive(latest);
        try {
            if (!await this.prepare(keys)) throw new Error('No se pudo reservar el registro.');
            this.state.content = applyBlocks(this.state.content, Object.fromEntries(keys.map(key => [key, proposal[key] ?? null])));
        } catch (error) {
            this.base = previousBase; this.state.content = previousContent;
            for (const key of keys) this.lost(key);
            throw error;
        } finally { this.emit(); }
    }
    async discard(comparedVersion: number, keys: string[]) {
        if (this.pending) {
            if (!await this.flush()) throw new Error('No se pudo confirmar el resultado del guardado. Tu propuesta se conserva.');
            throw new Error('El guardado pendiente se confirmó. Revisa la respuesta actual antes de continuar.');
        }
        const latest = await this.transport.read();
        if (latest.version !== comparedVersion) throw new Error('La respuesta guardada cambió. Revisa la comparación actualizada.');
        const shared = splitBlocks(normalizeContent(latest.contenido));
        const values = Object.fromEntries(keys.map(key => [key, shared[key] ?? null]));
        // Descartar requiere una decisión explícita y afecta sólo a la propuesta revisada.
        this.base = applyBlocks(this.base, values); this.state.content = applyBlocks(this.state.content, values);
        for (const key of keys) delete this.state.issues[key];
        this.state.conflicts = this.state.conflicts.filter(key => !keys.includes(key));
        this.receive(latest);
        for (const key of keys) await this.endBlock(key);
        this.emit();
    }
    async submit(confirmedVersion?: number): Promise<boolean> {
        if (this.disposed || !this.state.canSubmit || !await this.flush(true) || this.disposed || this.state.dirty) return false;
        // Un diálogo abierto no autoriza enviar respuestas recibidas después de esa revisión.
        if (!this.submitting && confirmedVersion !== undefined && confirmedVersion !== this.state.version) {
            this.submissionIssue = true;
            this.state.error = 'Las respuestas cambiaron. Cierra la confirmación y actualiza el estado antes de enviar.'; this.emit(); return false;
        }
        this.submitting ||= { tabId: this.tabId, operationId: crypto.randomUUID(), version: this.state.version };
        try {
            const result = await this.transport.submit(this.submitting);
            this.state.submitted = result.enviadoEn; this.state.version = result.version; this.state.locked = true; this.state.canSubmit = false;
            this.state.submittedBy = result.enviadoPor; this.state.review = null;
            this.state.error = ''; this.emit(); return true;
        } catch (error) {
            if ((error as { response?: { status: number } }).response?.status! < 500) this.submitting = undefined;
            this.submissionIssue = true;
            this.failure(error); return false;
        }
    }
    dispose() { this.disposed = true; clearTimeout(this.timer); clearTimeout(this.expiration); void this.releaseClean(); }
}
