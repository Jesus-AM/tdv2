import type { Auth } from '../types/auth';
export type NavigationModule = Auth['modules'][number];
export function moduleGroups(modules: NavigationModule[], origin: string) {
    const order: Record<string, number> = { procesos_operativos: 0, configuracion: 1, sincronizaciones: 2, pruebas_acceso: 3 };
    const seen = new Set<string>();
    const safe = modules.filter(module => {
        try {
            if (!module.route || new URL(module.route, origin).origin !== origin || seen.has(module.key)) return false;
            seen.add(module.key); return true;
        } catch { return false; }
    });
    // Sólo se agrupan relaciones publicadas; un submódulo huérfano nunca se convierte en módulo principal.
    safe.sort((a, b) => (order[a.key] ?? 99) - (order[b.key] ?? 99));
    return safe.filter(module => !module.parent).map(module => ({ module, children: safe.filter(child => child.parent === module.key) }));
}
export function activeModule(modules: NavigationModule[], current: string, origin: string) {
    const path = new URL(current, origin).pathname;
    return [...modules].sort((a, b) => b.route.length - a.route.length).find(module => {
        const route = new URL(module.route, origin).pathname;
        return path === route || route !== '/' && path.startsWith(route + '/');
    }) || (/^\/(formatos|colaboradores|vista-prueba|actuar-como-usuario)(\/|$)/.test(path)
        ? modules.find(module => module.key === 'procesos_operativos') : undefined);
}
