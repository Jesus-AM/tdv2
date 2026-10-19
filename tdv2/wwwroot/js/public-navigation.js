// Sólo presentación del portal: las anclas y el historial siguen siendo nativos.
(() => {
    const header = document.querySelector('.topbar');
    if (!header) return;
    const updateOffset = () => {
        const height = header.getBoundingClientRect().height;
        document.documentElement.style.setProperty('--alto-cabecera', `${height}px`);
    };
    updateOffset();
    new ResizeObserver(updateOffset).observe(header);
})();
