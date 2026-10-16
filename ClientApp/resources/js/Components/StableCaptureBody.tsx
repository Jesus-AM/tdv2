import { Component, createRef, type ReactNode } from 'react';
import { TableBody } from '@mui/material';

type Snapshot = {
    scroll: HTMLElement; left: number; top: number; anchor: HTMLElement | null; anchorTop: number;
    focus: HTMLElement | null; selection?: [number, number, 'forward' | 'backward' | 'none'];
};

/** Todas las filas son hermanas con claves propias: moverlas de grupo no desmonta controles.
 * La instantánea previa al commit conserva el punto de lectura cuando cambian grupos o nombres. */
export default class StableCaptureBody extends Component<{ children: ReactNode }, object, Snapshot | null> {
    private body = createRef<HTMLTableSectionElement>();
    getSnapshotBeforeUpdate(): Snapshot | null {
        const body = this.body.current, scroll = body?.closest<HTMLElement>('[data-edit-scroll]');
        if (!body || !scroll) return null;
        const active = body.ownerDocument.activeElement as HTMLElement | null;
        const focus = active && body.contains(active) ? active : null;
        const visibleTop = scroll.getBoundingClientRect().top + (scroll.querySelector('thead')?.getBoundingClientRect().height || 0);
        const anchor = focus?.closest<HTMLElement>('tr') || Array.from(body.querySelectorAll('tr'))
            .find(row => row.getBoundingClientRect().bottom > visibleTop) || null;
        const snapshot: Snapshot = { scroll, left: scroll.scrollLeft, top: scroll.scrollTop, anchor,
            anchorTop: anchor?.getBoundingClientRect().top || 0, focus };
        if ((focus instanceof HTMLInputElement || focus instanceof HTMLTextAreaElement) && focus.selectionStart !== null)
            snapshot.selection = [focus.selectionStart, focus.selectionEnd!, focus.selectionDirection || 'none'];
        return snapshot;
    }
    componentDidUpdate(_props: Readonly<{ children: ReactNode }>, _state: object, snapshot: Snapshot | null) {
        if (!snapshot) return;
        const { scroll, anchor, focus, selection } = snapshot;
        // Sólo recuperar el mismo control si el movimiento del DOM perdió el foco; no robarlo a otro destino.
        if (focus?.isConnected && focus.ownerDocument.activeElement === focus.ownerDocument.body) {
            focus.focus({ preventScroll: true });
            if (selection && (focus instanceof HTMLInputElement || focus instanceof HTMLTextAreaElement)) focus.setSelectionRange(...selection);
        }
        scroll.scrollLeft = snapshot.left;
        if (anchor?.isConnected) scroll.scrollTop += anchor.getBoundingClientRect().top - snapshot.anchorTop;
        else scroll.scrollTop = snapshot.top;
    }
    render() { return <TableBody ref={this.body}>{this.props.children}</TableBody>; }
}
