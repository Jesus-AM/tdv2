import { useLayoutEffect, useRef, type ReactNode } from 'react';

/** Una región estable por tabla. Sólo se mide el encabezado, nunca se reinicia su desplazamiento. */
export default function CaptureTableScroll({ label, bounded = true, children }: {
    label: string; bounded?: boolean; children: ReactNode;
}) {
    const container = useRef<HTMLDivElement>(null);
    useLayoutEffect(() => {
        const element = container.current, head = element?.querySelector('thead');
        if (!element || !head || !bounded) return;
        const measure = () => element.style.setProperty('--capture-head-height', `${head.getBoundingClientRect().height}px`);
        measure();
        const observer = new ResizeObserver(measure); observer.observe(head);
        return () => observer.disconnect();
    }, [bounded]);
    return <div ref={container} className={`capture-scroll${bounded ? ' capture-row-scroll' : ''}`}
        data-edit-scroll role={bounded ? 'region' : undefined} aria-label={bounded ? `Tabla de ${label}` : undefined}
        tabIndex={bounded ? 0 : undefined}>{children}</div>;
}
