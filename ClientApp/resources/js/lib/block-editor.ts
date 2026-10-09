import type { FormContent, SaveResponse } from '../types/tdv2';
import { normalizeContent } from './form-editor';
import { applyBlocks, blockValue, changedBlocks, sameBlock } from './form-blocks';
import type { SubmissionReview } from '../Components/FormSubmissionReview';
import type { RowTarget } from './form-deletion';
export type Lease = { id: string | null; propia: boolean; titular: string; venceEn: string; otraPestana?: boolean; color?: number; foto?: string | null };
export type Block = { key: string; version: number; reserva: Lease | null; value?: unknown };
export type LiveForm = SaveResponse & { contenido: FormContent; bloques: Block[]; editable: boolean; puedeEnviar: boolean; enviadoEn: string | null; enviadoPor: string | null; revisionEnvio: SubmissionReview | null; seccion: string; servidorEn: string; seccionesPosteriores?: boolean };
export type Mutation = { tabId: string; operationId: string; blocks: { key: string; version: number; leaseId?: string | null; value?: unknown }[]; section: string; release: boolean; removal?: RowTarget };
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
    procedures: NonNullable<LiveForm['procedimientosDisponibles']>;
    stageProgress: number; stageReview: SubmissionReview | null;
    laterSections?: boolean;
    preparing: string[]; issues: Record<string, string>; validation: Record<string, Record<string, string>>;
    unaccepted: Record<string, Record<string, string>>;
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
    private renewing = new Map<string, Promise<void>>();
    private pending = new Map<string, Mutation>();
    private removals = new Map<string, RowTarget>();
    private retries = new Map<string, { attempts: number; due: number }>();
    private retryTimer?: ReturnType<typeof setTimeout>;
    private recoveryAttempts = 0;
    private kinds = new Map<string, 'validation' | 'connection' | 'expired' | 'incompatible' | 'permission'>();
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
            canSubmit: false, section: 'contexto', conflicts: [], review: null, procedures: [], stageProgress: 0, stageReview: null, submittedBy: null, preparing: [], issues: {}, validation: {}, unaccepted: {} };
    }
    private dirtyKeys() {
        if (this.dirtyBase !== this.base || this.dirtyContent !== this.state.content) {
            this.dirtyBlocks = changedBlocks(this.base, this.state.content);
            this.dirtyBase = this.base; this.dirtyContent = this.state.content;
        }
        return this.dirtyBlocks;
    }
    private emit(leasesChanged = true) {
        this.state.dirty = this.dirtyKeys().length > 0 || Object.keys(this.state.unaccepted).length > 0 || this.pending.size > 0;
        this.state.conflict = this.state.conflicts.length > 0;
        if (leasesChanged) {
            clearTimeout(this.expiration);
            const remaining = Object.values(this.state.blocks).map(b => Date.parse(b.reserva?.venceEn || '') - Date.now() - this.serverOffset).filter(n => n > 0);
            if (!this.disposed && remaining.length) this.expiration = setTimeout(() => {
                for (const key of this.dirtyKeys()) if (!this.owned(key)) this.lost(key);
                if (this.state.conflicts.length) this.scheduleRetry();
                this.emit();
            }, Math.min(...remaining) + 10);
            this.publishedBlocks = { ...this.state.blocks };
        }
        if (!this.disposed) this.notify({ ...this.state, blocks: this.publishedBlocks });
    }
    private request(keys: string[], release = false): Mutation {
        return { tabId: this.tabId, operationId: crypto.randomUUID(), section: this.state.section, release,
            removal: keys.map(key => this.removals.get(key)).find(Boolean),
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
            && !this.state.preparing.includes(key) && !this.releasing.has(key)
            && (!this.kinds.has(key) || this.kinds.get(key) === 'validation') && this.owned(key);
    }
    private lost(key: string) {
        this.confirmed.delete(key);
        if (this.kinds.get(key) === 'incompatible') return;
        this.kinds.set(key, 'expired');
        this.state.issues[key] ||= 'Cambios sin guardar. Comprobando la conexión y el registro…';
        if (!this.state.conflicts.includes(key)) this.state.conflicts.push(key);
    }
    proposalKeys(key: string) { return this.groups.get(key) || [key]; }
    proposalBase() { return this.base; }
    keepUnacceptedInput(key: string, field: string, text: string) {
        // Es texto capturado antes del permiso, no una modificación autorizada ni un guardado pendiente.
        this.state.unaccepted[key] = { ...this.state.unaccepted[key], [field]: text };
        this.state.issues[key] = 'No se pudo iniciar la edición. Este texto no se guardó:';
        this.emit(false);
    }
    async refresh() {
        const epoch = this.connectionEpoch;
        try {
            this.receive(await this.transport.read());
            // Un mensaje en vuelo anterior a la desconexión no rehabilita la captura.
            // Sólo una consulta iniciada después de ella vuelve a confirmar el estado vigente.
            if (epoch === this.connectionEpoch && this.connectionIssue) {
                this.connectionIssue = false; this.state.error = ''; this.rearmRetries(); this.emit();
            }
            // Volver a consultar permite revisar y reintentar un envío rechazado sin tocar respuestas.
            if (this.submissionIssue && !this.state.dirty && !this.state.conflict) {
                this.submissionIssue = false; this.state.error = ''; this.emit();
            }
            for (const key of this.dirtyKeys()) {
                if (this.owned(key) && this.kinds.get(key) === 'connection' && !this.pendingFor(key)) this.clearIssue(key);
            }
            if (!this.state.locked && (this.pending.size || [...this.kinds.values()].some(k => k === 'connection' || k === 'expired'))) this.scheduleRetry();
            this.emit(); return true;
        }
        catch (error) { this.failure(error); return false; }
    }
    receive(data: LiveForm) {
        if (this.disposed || data.version < this.state.version || Date.parse(data.servidorEn) < this.serverTimestamp) return;
        const first = !this.state.initialized;
        if (this.state.saving) { this.latest = data; return; }
        // Una lectura ya aplicada no debe reaplicarse tras un 422 y borrar reservas adquiridas
        // después. Sí retenemos el estado compartido de bloques con propuesta/recibo pendiente.
        this.latest = undefined;
        const previousContent = this.state.content;
        const signature = () => JSON.stringify({ ...this.state, content: undefined, blocks: this.state.blocks });
        const previousState = signature();
        const dirty = this.dirtyKeys();
        if (dirty.length || this.pending.size) this.latest = data;
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
        for (const key of Object.keys(updates)) if (updates[key] === null && !dirty.includes(key)) this.retire(key);
        this.state.stageProgress = data.porcentajeEtapa ?? this.state.stageProgress;
        this.state.stageReview = data.revisionEtapa ?? this.state.stageReview; this.state.procedures = data.procedimientosDisponibles ?? this.state.procedures;
        this.state.laterSections = data.seccionesPosteriores;
        this.state.updatedAt = data.actualizadoEn; this.state.updatedBy = data.actualizadoPor;
        this.state.locked = !data.editable; this.state.canSubmit = data.puedeEnviar; this.state.submitted = data.enviadoEn;
        this.state.review = data.revisionEnvio ?? null; this.state.submittedBy = data.enviadoPor ?? null;
        this.serverOffset = Date.parse(data.servidorEn) - Date.now();
        this.serverTimestamp = Date.parse(data.servidorEn);
        for (const key of dirty) if (!this.owned(key)) this.lost(key);
        for (const key of this.confirmed.keys()) if (!this.owned(key)) this.confirmed.delete(key);
        if (!this.state.initialized && !this.sectionChosen) this.state.section = data.seccion || 'contexto';
        this.state.initialized = true;
        for (const key of Object.keys(this.state.issues)) if (!dirty.includes(key) && !this.pendingFor(key) && !this.state.unaccepted[key]) this.clearIssue(key);
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
            for (const key of this.dirtyKeys()) {
                this.kinds.set(key, 'connection'); this.state.issues[key] ||= 'Sin conexión. Lo escrito se conserva aquí, todavía sin guardar.';
            }
            this.state.error = 'Reconectando… Los cambios sin confirmar se conservan.'; this.scheduleRetry(); this.emit();
        }
    }
    private clearIssue(key: string) {
        delete this.state.issues[key]; delete this.state.validation[key]; this.kinds.delete(key);
        this.state.conflicts = this.state.conflicts.filter(k => k !== key);
    }
    private pendingFor(key: string) { return [...this.pending.values()].some(p => p.blocks.some(b => b.key === key)); }
    private rearmRetries() {
        this.recoveryAttempts = 0;
        for (const retry of this.retries.values()) { retry.attempts = 0; retry.due = 0; }
    }
    private failure(error: unknown, keys: string[] = []) {
        const response = (error as { response?: { status: number; data?: { message?: string; errors?: Record<string, string[]>;
            validation?: { block: string; field: string; message: string }[] } } }).response;
        const status = response?.status ?? 0;
        if ([401, 403, 419].includes(status)) this.state.locked = true;
        const message = response?.data?.message || Object.values(response?.data?.errors || {}).flat()[0]
            || 'Sin confirmar el guardado. Lo escrito se conserva aquí.';
        const validation = response?.data?.validation || [];
        if (status === 422) {
            // Sólo el campo inválido se señala; su reserva sigue habilitando correcciones.
            for (const key of keys) if (!validation.length || validation.some(v => v.block === key)) {
                this.kinds.set(key, 'validation'); this.state.issues[key] = message;
                this.state.validation[key] = Object.fromEntries(validation.filter(v => v.block === key).map(v => [v.field, v.message]));
            }
            if (validation.length && !validation.some(v => keys.includes(v.block)) && keys.length) {
                this.kinds.set(keys[0], 'validation'); this.state.issues[keys[0]] = message;
            }
        } else if (keys.length) for (const key of keys) {
            if (status === 409) this.lost(key);
            else this.kinds.set(key, this.state.locked ? 'permission' : 'connection');
            this.state.issues[key] = this.state.locked ? 'El acceso cambió. Lo escrito no se ha guardado.' : message;
        } else this.state.error = message;
        this.emit();
    }
    // Tres reintentos espaciados, nunca por tecla ni por latido de SignalR.
    private scheduleRetry() {
        if (this.disposed || this.state.locked || this.retryTimer || this.recoveryAttempts >= 3) return;
        const wait = [1000, 3000, 8000][this.recoveryAttempts++];
        this.retryTimer = setTimeout(() => {
            this.retryTimer = undefined;
            void (async () => { if (await this.refresh()) await this.flush();
                if (this.connectionIssue || [...this.kinds.values()].some(k => k === 'connection' || k === 'expired')) this.scheduleRetry(); })();
        }, wait);
    }
    private async recheck(keys: string[]) {
        const live = await this.transport.read(); this.receive(live);
        if (!live.editable || this.state.locked || this.connectionIssue) return false;
        for (const key of keys) {
            if (this.busy(key)) return false;
            if (!sameBlock(blockValue(this.base, key), blockValue(live.contenido, key))) {
                this.kinds.set(key, 'incompatible');
                this.state.issues[key] = 'Este registro cambió durante la interrupción. Lo escrito permanece aquí, sin guardar.';
                this.emit(); return false;
            }
        }
        for (const key of keys) {
            const current = live.bloques.find(b => b.key === key);
            this.state.blocks[key] = current ? structuredClone(current) : { key, version: 0, reserva: null };
            this.clearIssue(key);
        }
        return true;
    }
    private async ensure(keys: string[]) {
        const releases = keys.flatMap(key => this.releasing.has(key) ? [this.releasing.get(key)!] : []);
        if (releases.length) await Promise.all(releases);
        if (this.acquiring) await this.acquiring;
        if (this.disposed) throw new Error('El editor terminó.');
        if (this.state.locked || this.connectionIssue) throw new Error('No se pudo confirmar el acceso.');
        const needed = keys.filter(key => !this.owned(key));
        if (!needed.length) return;
        const dirty = needed.filter(key => this.dirtyKeys().includes(key));
        // Reanudar sólo si la respuesta compartida sigue siendo la base de lo escrito.
        if (dirty.length && !await this.recheck(dirty)) throw new Error('El registro no está disponible.');
        this.state.preparing = [...new Set([...this.state.preparing, ...needed])]; this.emit();
        this.acquiring = (async () => {
            const result = await this.transport.reserve(this.request(needed));
            if (needed.some(key => !result.bloques.some(block => block.key === key && 'value' in block && block.reserva?.propia && block.reserva.id)))
                throw new Error('No se pudo confirmar el contenido del registro.');
            this.serverOffset = Date.parse(result.servidorEn) - Date.now();
            this.serverTimestamp = Math.max(this.serverTimestamp, Date.parse(result.servidorEn));
            for (const block of result.bloques) {
                if (block.version < (this.state.blocks[block.key]?.version ?? 0)) continue;
                if (dirty.includes(block.key) && !sameBlock(blockValue(this.base, block.key), block.value)) {
                    this.kinds.set(block.key, 'incompatible');
                    this.state.issues[block.key] = 'El registro cambió. Lo escrito se conserva sin guardar.';
                    void this.transport.release({ ...this.request([]), blocks: [{ key: block.key, version: block.version, leaseId: block.reserva?.id }] }).catch(() => {});
                    continue;
                }
                this.state.blocks[block.key] = block;
                if ('value' in block && !dirty.includes(block.key)) {
                    this.base = applyBlocks(this.base, { [block.key]: block.value ?? null });
                    this.state.content = applyBlocks(this.state.content, { [block.key]: block.value ?? null });
                }
                if (block.reserva?.propia && this.active(block.reserva) && block.reserva.id) this.confirmed.set(block.key, block.reserva.id);
                this.clearIssue(block.key);
            }
            this.emit();
        })();
        try { await this.acquiring; } finally { this.acquiring = undefined; this.state.preparing = this.state.preparing.filter(key => !needed.includes(key)); this.emit(); }
    }
    async prepare(keys: string[]): Promise<boolean> {
        if (this.disposed || this.state.locked || !this.state.initialized || keys.some(key => this.kinds.get(key) === 'incompatible' || this.kinds.get(key) === 'permission' || this.pendingFor(key) || this.busy(key))) return false;
        try { await this.ensure(keys); return keys.every(key => this.canEdit(key)); }
        catch (error) {
            if ((error as { response?: { data?: { code?: string } } }).response?.data?.code === 'registro_eliminado') {
                await this.refresh(); return false;
            }
            // Una carrera normal por la reserva se presenta en la fila, no como conflicto de guardado.
            if ((error as { response?: { status: number } }).response?.status === 409) {
                if (await this.refresh() && !keys.some(key => this.busy(key) || this.kinds.get(key) === 'incompatible')) {
                    try { await this.ensure(keys); return keys.every(key => this.canEdit(key)); } catch { /* Releer muestra al titular ganador. */ }
                    await this.refresh();
                }
            } else if (!keys.some(key => this.kinds.get(key) === 'incompatible' || this.busy(key))) this.failure(error, keys);
            return false;
        }
    }
    async prepareRemoval(keys: string[], target: RowTarget) {
        // La actividad anterior termina antes de iniciar el retiro; nunca renovamos una eliminación.
        await Promise.all(keys.flatMap(key => this.renewing.has(key) ? [this.renewing.get(key)!] : []));
        // El servidor concede sólo las reservas auxiliares de esta cascada; no habilita captura de esas secciones.
        for (const key of keys) this.removals.set(key, target);
        return this.prepare(keys);
    }
    finishRemoval(keys: string[]) {
        for (const key of keys) if (!this.dirtyKeys().includes(key) && !this.pendingFor(key)) this.removals.delete(key);
    }
    private retire(key: string) {
        this.confirmed.delete(key); this.focused.delete(key); this.focusEpoch.delete(key);
        this.lastActivity.delete(key); this.removals.delete(key); this.groups.delete(key); this.clearIssue(key);
        this.state.preparing = this.state.preparing.filter(k => k !== key);
        if (this.state.blocks[key]) this.state.blocks[key].reserva = null;
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
        if (this.disposed || this.state.locked || !this.state.initialized || this.kinds.get(key) === 'incompatible' || this.busy(key)) return false;
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
        this.recoveryAttempts = 0;
        for (const key of keys) {
            if (this.kinds.get(key) === 'validation') this.clearIssue(key);
            const held = this.state.unaccepted[key];
            if (held) {
                const before = blockValue(this.state.content, key) as Record<string, unknown> | undefined;
                const after = blockValue(next, key) as Record<string, unknown> | undefined;
                for (const field of Object.keys(held)) if (!sameBlock(before?.[field], after?.[field])) delete held[field];
                if (!Object.keys(held).length) { delete this.state.unaccepted[key]; this.clearIssue(key); }
            }
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
            if (!this.removals.has(key) && !this.renewing.has(key) && blockValue(next, key) != null
                && lease?.propia && this.active(lease) && Date.now() - (this.lastActivity.get(key) || 0) > 15000) {
                this.lastActivity.set(key, Date.now());
                const renewal = this.request([key]);
                const activity = this.transport.renew(renewal).then(result => {
                    for (const block of result.bloques) {
                        const current = this.state.blocks[block.key];
                        if (!this.disposed && current?.reserva?.id === block.reserva?.id && current.version === block.version) {
                            current.reserva = block.reserva;
                            this.serverTimestamp = Math.max(this.serverTimestamp, Date.parse(result.servidorEn) || 0);
                        }
                    }
                    this.emit();
                }).catch(error => {
                    const current = this.state.blocks[key];
                    // Una renovación anterior al guardado no puede convertir su confirmación en conflicto.
                    if (current?.version === renewal.blocks[0].version && current.reserva?.id === renewal.blocks[0].leaseId)
                        this.failure(error, [key]);
                }).finally(() => { if (this.renewing.get(key) === activity) this.renewing.delete(key); });
                this.renewing.set(key, activity);
            }
        }
        clearTimeout(this.timer);
        this.timer = setTimeout(() => void this.flush(), 1000);
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
        // Volver al valor original no anula una petición que ya viaja al servidor.
        if ((this.dirtyKeys().includes(key) || this.pendingFor(key)) && !await this.flush(false, this.proposalKeys(key))) return;
        if (this.focusEpoch.get(key) !== epoch || this.dirtyKeys().includes(key) || this.pendingFor(key)) return;
        const block = this.state.blocks[key];
        if (block?.reserva?.propia) {
            try {
                await this.releaseKeys([key]);
            }
            catch (error) { this.failure(error); }
        }
    }
    async flush(release = false, selected?: string[]): Promise<boolean> {
        if (this.running) { await this.running; return this.flush(release, selected); }
        // Guardar borrador y terminar explícitamente el apartado pueden reintentar un recibo agotado.
        // El temporizador no reinicia este presupuesto, ni inventa un nuevo identificador de operación.
        if (release) this.rearmRetries();
        if (this.state.locked || !this.state.initialized || this.disposed) return !this.state.dirty;
        clearTimeout(this.timer);
        this.running = (async () => {
            const considered = new Set<string>();
            // Una fila o un cambio explícito con relaciones constituyen una operación atómica.
            for (const key of this.dirtyKeys()) {
                if (considered.has(key) || selected && !selected.includes(key)) continue;
                const group = this.proposalKeys(key).filter(k => this.dirtyKeys().includes(k));
                for (const k of group) considered.add(k);
                await this.save(group);
            }
            for (const [key, operation] of this.pending) if (!considered.has(key) && (!selected || operation.blocks.some(b => selected.includes(b.key))))
                await this.save(operation.blocks.map(b => b.key));
            if (release) await this.releaseClean(selected);
            else await this.releaseClean([...considered].filter(k => !this.focused.has(k)));
            return !Object.keys(this.state.unaccepted).some(k => !selected || selected.includes(k))
                && !this.dirtyKeys().some(k => !selected || selected.includes(k))
                && ![...this.pending.values()].some(p => !selected || p.blocks.some(b => selected.includes(b.key)));
        })();
        try { return await this.running; } finally { this.running = undefined; }
    }
    async releaseClean(selected?: string[]) {
        if (this.acquiring) await this.acquiring.catch(() => {});
        const dirty = this.dirtyKeys();
        const keys = Object.values(this.state.blocks).filter(b => b.reserva?.propia && !dirty.includes(b.key)
            && !this.pendingFor(b.key) && (!selected || selected.includes(b.key))).map(b => b.key);
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
    private async save(keys: string[]): Promise<boolean> {
        const existing = [...this.pending.entries()].find(([, p]) => p.blocks.some(b => keys.includes(b.key)));
        const operationKey = existing?.[0] || keys[0];
        let sent = existing?.[1];
        if (!sent && keys.some(k => this.kinds.get(k) === 'validation' || this.kinds.get(k) === 'incompatible')) return false;
        if (this.connectionIssue) { this.scheduleRetry(); return false; }
        if (sent) {
            const retry = this.retries.get(sent.operationId);
            if (retry && (retry.attempts >= 3 || retry.due > Date.now())) { this.scheduleRetry(); return false; }
        } else {
            if (!await this.prepare(keys)) return false;
            if (keys.some(k => !this.canEdit(k)) || this.disposed) return false;
            sent = this.request(keys); // Liberar después del ACK y de agotar escrituras posteriores.
            sent.blocks = sent.blocks.map(block => ({ ...block, value: structuredClone(blockValue(this.state.content, block.key) ?? null) }));
            this.pending.set(operationKey, sent);
        }
        this.state.saving = true; this.emit();
        try {
            const saved = await this.transport.save(sent);
            const updates: Record<string, unknown> = {};
            for (const block of saved.bloques) {
                const current = blockValue(this.state.content, block.key), original = sent.blocks.find(b => b.key === block.key);
                if (!original || sameBlock(current, original.value)) updates[block.key] = block.value ?? null;
                else if (current && original.value && block.value && typeof current === 'object' && !Array.isArray(current)) {
                    // Incorporar códigos confirmados sin borrar texto escrito durante la petición.
                    const merged = { ...current as Record<string, unknown> };
                    for (const [field, value] of Object.entries(block.value as Record<string, unknown>))
                        if (sameBlock(merged[field], (original.value as Record<string, unknown>)[field])) merged[field] = value;
                    updates[block.key] = merged;
                }
                this.state.blocks[block.key] = block; this.clearIssue(block.key);
                if (!block.reserva?.propia) this.confirmed.delete(block.key);
            }
            this.base = applyBlocks(this.base, Object.fromEntries(saved.bloques.map(b => [b.key, b.value ?? null])));
            this.state.content = applyBlocks(this.state.content, updates);
            // Sólo la confirmación del servidor retira el estado de edición, nunca la propuesta optimista.
            for (const block of saved.bloques) if (block.value === null) this.retire(block.key);
            const dirty = this.dirtyKeys();
            for (const block of saved.bloques) {
                const group = this.groups.get(block.key);
                // Una cascada confirmada no une para siempre registros que ya son independientes.
                if (group && !group.some(key => dirty.includes(key))) for (const key of group) this.groups.delete(key);
            }
            this.state.version = Math.max(this.state.version, saved.version); this.state.progress = saved.porcentaje;
            this.state.stageProgress = saved.porcentajeEtapa ?? this.state.stageProgress;
            this.state.stageReview = saved.revisionEtapa ?? this.state.stageReview; this.state.procedures = saved.procedimientosDisponibles ?? this.state.procedures;
            this.state.updatedAt = saved.actualizadoEn; this.state.updatedBy = saved.actualizadoPor;
            this.state.review = saved.revisionEnvio ?? null;
            this.pending.delete(operationKey); this.retries.delete(sent.operationId);
            this.finishRemoval(sent.blocks.map(b => b.key));
            if (!this.state.conflicts.length) this.state.error = '';
            return true;
        } catch (error) {
            const status = (error as { response?: { status: number } }).response?.status;
            if (status && status < 500 && status !== 408 && status !== 429) {
                this.pending.delete(operationKey); this.retries.delete(sent.operationId);
            } else {
                const attempts = (this.retries.get(sent.operationId)?.attempts || 0) + 1;
                this.retries.set(sent.operationId, { attempts, due: Date.now() + [1000, 3000, 8000][Math.min(attempts - 1, 2)] });
            }
            this.failure(error, sent.blocks.map(b => b.key));
            if (status !== 422 && !this.state.locked) this.scheduleRetry();
            return false;
        } finally {
            this.state.saving = false; this.emit();
            const latest = this.latest;
            if (latest && latest.version >= this.state.version) this.receive(latest);
            if (!this.disposed && this.dirtyKeys().some(k => !this.kinds.has(k) && !this.pendingFor(k)))
                this.timer = setTimeout(() => void this.flush(), 300);
        }
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
    dispose() { this.disposed = true; clearTimeout(this.timer); clearTimeout(this.expiration); clearTimeout(this.retryTimer); void this.releaseClean(); }
}
