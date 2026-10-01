import type { FormContent, SaveResponse } from '../types/tdv2';
export interface EditorState {
    content: FormContent;
    version: number;
    progress: number;
    dirty: boolean;
    saving: boolean;
    locked: boolean;
    error: string;
    updatedBy: string | null;
    updatedAt: string | null;
    conflict: boolean;
}
// Laravel returns nullable fields as null after validation; keep controlled inputs stable.
export function normalizeContent(input: FormContent): FormContent {
    const c = structuredClone(input);
    const text = <T extends object>(row: T): T =>
        Object.fromEntries(Object.entries(row).map(([key, value]) => [key, value === null ? '' : value])) as T;
    c.encabezado = text(c.encabezado);
    c.identificacion = c.identificacion.map(text);
    c.sistemas = c.sistemas.map((row) => text({ ...row, proceso: row.proceso ?? row.procesos?.[0] ?? '' }));
    c.datos = c.datos.map(text);
    c.acuerdos = c.acuerdos.map(text);
    c.medioOtro ||= '';
    c.evaluaciones = Object.fromEntries(
        Object.entries(c.evaluaciones || {}).map(([code, rows]) => [
            code,
            rows.filter((row) => row.criterio !== 'Tiempo de atención').map(text),
        ]),
    );
    c.preguntas = c.preguntas.map((q) => ({ ...q, respuesta: q.respuesta || '' }));
    return c;
}
type Timer = ReturnType<typeof setTimeout>;
export class FormEditor {
    state: EditorState;
    private timer: Timer | undefined;
    private disposed = false;
    constructor(
        content: FormContent,
        version: number,
        progress: number,
        private editable: boolean,
        private put: (body: { version: number; contenido: FormContent }) => Promise<SaveResponse>,
        private notify: (state: EditorState) => void,
        private later = (callback: () => void, ms: number) => setTimeout(callback, ms),
        private cancel = (timer: Timer | undefined) => clearTimeout(timer),
    ) {
        this.state = {
            content: normalizeContent(content),
            version,
            progress,
            dirty: false,
            saving: false,
            locked: !editable,
            error: '',
            updatedBy: null,
            updatedAt: null,
            conflict: false,
        };
        this.state.content.evaluaciones = { ...this.state.content.evaluaciones };
    }
    private emit() {
        if (!this.disposed) this.notify({ ...this.state });
    }
    change(edit: (content: FormContent) => void) {
        if (this.state.locked || this.disposed) return;
        const next = structuredClone(this.state.content);
        edit(next);
        this.state.content = next;
        this.state.dirty = true;
        this.emit();
        this.cancel(this.timer);
        this.timer = this.later(() => void this.flush(), 900);
    }
    async flush(force = false) {
        if (this.disposed || this.state.locked || !this.editable || this.state.saving || (!this.state.dirty && !force))
            return;
        this.cancel(this.timer);
        const body = { version: this.state.version, contenido: structuredClone(this.state.content) };
        this.state.saving = true;
        this.state.dirty = false;
        this.state.error = '';
        this.emit();
        try {
            const saved = await this.put(body);
            this.state.version = saved.version;
            this.state.progress = saved.porcentaje;
            this.state.updatedBy = saved.actualizadoPor;
            this.state.updatedAt = saved.actualizadoEn;
        } catch (error) {
            this.state.dirty = true;
            const response = (error as { response?: { status: number; data?: { errors?: Record<string, string[]> } } })
                .response;
            const status = response?.status;
            this.state.conflict = status === 409;
            this.state.locked = this.state.conflict || [401, 403, 419].includes(status || 0);
            this.state.error = this.state.conflict
                ? 'Otra persona guardó una versión más reciente. Descarga tus cambios y recarga para revisar la versión compartida.'
                : this.state.locked
                  ? 'Tu sesión o permiso cambió. Conserva tus cambios antes de salir.'
                  : Object.values(response?.data?.errors || {}).flat()[0] ||
                    'No se pudo guardar. Conserva esta página e inténtalo de nuevo.';
        } finally {
            this.state.saving = false;
            this.emit();
            if (!this.disposed && this.state.dirty && !this.state.error && !this.state.locked) {
                this.cancel(this.timer);
                this.timer = this.later(() => void this.flush(), 300);
            }
        }
    }
    dispose() {
        this.disposed = true;
        this.cancel(this.timer);
    }
}
