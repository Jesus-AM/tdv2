// Instrumentación sólo del bundle de pruebas: nunca se añade al código fuente ni al bundle de entrega.
import { build } from 'vite';
import { fileURLToPath } from 'node:url';
await build({ root: fileURLToPath(new URL('../../', import.meta.url)), plugins: [{
    name: 'synthetic-capture-renders', enforce: 'pre',
    transform(code, id) {
        if (!id.replaceAll('\\', '/').endsWith('/pages/FormatoUR.tsx')) return;
        if (!code.includes('return render();') || !code.includes('function Editor(props: Props) {')) throw new Error('Actualizar la sonda de renderizados antes de medir.');
        return code.replace('return render();', `const node = render();
            const counts = ((window as any).__captureRenders ||= { editor: 0, rows: {} });
            const key = (node as any)?.props?.['data-edit-block'] || 'otro';
            counts.rows[key] = (counts.rows[key] || 0) + 1; return node;`)
            .replace('function Editor(props: Props) {', `function Editor(props: Props) {
                ((window as any).__captureRenders ||= { editor: 0, rows: {} }).editor++;`);
    },
}] });
