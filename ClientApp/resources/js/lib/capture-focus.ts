/** Revela el destino respetando el encabezado y la presencia fijos de la tabla. */
export function focusCaptureField(target?: HTMLElement | null) {
    if (!target) return;
    target.focus({ preventScroll: true });
    // El foco puede abrir Autocomplete y añadir su buscador. Medir tras ese render,
    // sin volver a enfocar si mientras tanto el usuario cambió de destino.
    requestAnimationFrame(() => {
        if (!target.isConnected || document.activeElement !== target) return;
        const control = target.closest<HTMLElement>('.MuiFormControl-root') || target;
        control.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'instant' });
    });
}
