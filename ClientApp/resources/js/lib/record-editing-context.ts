const auxiliaries = '[data-field-help], [data-participant-avatar], [data-field-help-dialog], [role="listbox"]';
const fields = 'input, textarea, label, [role="combobox"]';

export function isScrollbarPointer(event: { target: EventTarget | null; clientX: number; clientY: number }) {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return false;
    const doc = target.ownerDocument;
    if (target === doc.documentElement || target === doc.body)
        return event.clientX >= doc.documentElement.clientWidth || event.clientY >= doc.documentElement.clientHeight;
    const rect = target.getBoundingClientRect();
    return target.scrollWidth > target.clientWidth && event.clientY >= rect.top + target.clientTop + target.clientHeight
        || target.scrollHeight > target.clientHeight && event.clientX >= rect.left + target.clientLeft + target.clientWidth;
}

/** El contexto del registro sobrevive al blur hacia el navegador y a los gestos de desplazamiento.
 * Sólo un destino de foco concreto o un clic completado fuera del contexto indica salida.
 * No enfoca controles, cancela eventos, renueva reservas ni conserva autoridad de escritura. */
export class RecordEditingContext {
    private current: string | null = null;
    private pointer?: { id: number; x: number; y: number; scrolling: boolean; moved: boolean };
    get scrolling() { return !!this.pointer?.scrolling; }
    constructor(private doc: Document, private leave: (key: string, stillOutside: () => boolean) => void,
        private modal: () => readonly string[]) {
        doc.addEventListener('focusin', this.focus, true);
        doc.addEventListener('pointerdown', this.down, true);
        doc.addEventListener('pointermove', this.move, true);
        doc.addEventListener('pointerup', this.up, true);
        doc.addEventListener('pointercancel', this.cancel, true);
        doc.addEventListener('scroll', this.scroll, true);
    }
    private visit(target: EventTarget | null, focus = false) {
        if (!(target instanceof Element)) return;
        if (focus && (target === this.doc.body || target === this.doc.documentElement)) return;
        if (target.closest(auxiliaries) || this.doc.querySelector('[data-field-help-dialog], [role="listbox"]')
            || this.current && this.modal().includes(this.current)) return;
        // El navegador puede enfocar el contenedor desplazable al utilizar su barra.
        if (focus && target.matches('[data-edit-scroll]')) return;
        const key = target.closest('[data-edit-block]')?.getAttribute('data-edit-block') || null;
        const previous = this.current;
        if (key === previous) return;
        this.current = key && target.closest(fields) ? key : null;
        if (previous) this.leave(previous, () => this.current !== previous);
    }
    private focus = (event: FocusEvent) => { if (!this.pointer?.scrolling) this.visit(event.target, true); };
    private down = (event: PointerEvent) => {
        this.pointer = { id: event.pointerId, x: event.clientX, y: event.clientY, scrolling: isScrollbarPointer(event), moved: false };
    };
    private move = (event: PointerEvent) => {
        if (this.pointer?.id === event.pointerId && Math.hypot(event.clientX - this.pointer.x, event.clientY - this.pointer.y) > 6)
            this.pointer.moved = true;
    };
    private scroll = () => { if (this.pointer) this.pointer.scrolling = true; };
    private cancel = () => { this.pointer = undefined; };
    private up = (event: PointerEvent) => {
        const pointer = this.pointer; this.pointer = undefined;
        if (pointer?.id === event.pointerId && !pointer.scrolling && !pointer.moved) this.visit(event.target);
    };
    dispose() {
        this.doc.removeEventListener('focusin', this.focus, true);
        this.doc.removeEventListener('pointerdown', this.down, true);
        this.doc.removeEventListener('pointermove', this.move, true);
        this.doc.removeEventListener('pointerup', this.up, true);
        this.doc.removeEventListener('pointercancel', this.cancel, true);
        this.doc.removeEventListener('scroll', this.scroll, true);
    }
}
