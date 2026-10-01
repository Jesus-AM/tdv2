import type { FormRow, Unit } from '@/types/tdv2';

export interface AreaNode {
    unit: Unit;
    form: FormRow | null;
    children: AreaNode[];
    areaCount: number;
    forms: FormRow[];
    contextOnly?: boolean;
}

export type AreaFilter = 'todos' | 'edicion' | 'sin_iniciar' | 'en_proceso' | 'completos';

export function normalizeAreaText(value: string): string {
    return value
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLocaleLowerCase('es')
        .trim();
}

export function matchesForm(form: FormRow | null, filter: AreaFilter): boolean {
    if (filter === 'todos') {
        return true;
    }

    if (!form) {
        return false;
    }

    const progress = Number(form.porcentaje);

    return filter === 'edicion'
        ? form.editable
        : filter === 'sin_iniciar'
          ? progress === 0
          : filter === 'en_proceso'
            ? progress > 0 && progress < 100
            : progress === 100;
}

// The institutional parent identifier defines the hierarchy; codes are labels only.
export function buildAreaDirectory(units: Unit[], forms: FormRow[]): { roots: AreaNode[]; others: AreaNode[] } {
    // Only these levels have their own format in the administrator directory.
    const ordered = units
        .filter((unit) => [2, 3].includes(Number(unit.nivel_ur)))
        .sort((a, b) => a.cve_ur.localeCompare(b.cve_ur, 'es', { numeric: true }) || a.id_ur.localeCompare(b.id_ur));
    const byId = new Map(ordered.map((unit) => [unit.id_ur, unit]));
    const byForm = new Map(forms.map((form) => [form.id_ur, form]));
    const parents = new Map(
        ordered.map((unit) => [
            unit.id_ur,
            Number(unit.nivel_ur) !== 2 && unit.id_ur_pertenece !== unit.id_ur && byId.has(unit.id_ur_pertenece || '')
                ? unit.id_ur_pertenece
                : null,
        ]),
    );

    // A malformed catalog must not hide areas or cause infinite recursion.
    for (const unit of ordered) {
        const seen = new Set<string>();
        let id: string | null = unit.id_ur;

        while (id) {
            if (seen.has(id)) {
                parents.set(id, null);
                break;
            }

            seen.add(id);
            id = parents.get(id) || null;
        }
    }

    const children = new Map<string, Unit[]>();

    for (const unit of ordered) {
        const parent = parents.get(unit.id_ur);

        if (parent) {
            children.set(parent, [...(children.get(parent) || []), unit]);
        }
    }

    function build(unit: Unit): AreaNode {
        const form = byForm.get(unit.id_ur) || null;
        const branches = (children.get(unit.id_ur) || []).map((child) => build(child));

        return {
            unit,
            form,
            children: branches,
            areaCount: 1 + branches.reduce((sum, child) => sum + child.areaCount, 0),
            forms: [...(form ? [form] : []), ...branches.flatMap((child) => child.forms)],
        };
    }
    const roots = ordered.filter((unit) => Number(unit.nivel_ur) === 2).map((unit) => build(unit));
    const others = ordered
        .filter((unit) => Number(unit.nivel_ur) !== 2 && !parents.get(unit.id_ur))
        .map((unit) => build(unit));

    return { roots, others };
}

// Keep ancestors as context; a matching parent includes its matching descendants.
export function filterAreaTree(nodes: AreaNode[], query: string, filter: AreaFilter): AreaNode[] {
    const term = normalizeAreaText(query);
    function visit(node: AreaNode, parentMatched: boolean): AreaNode | null {
        const textMatch =
            !term || parentMatched || normalizeAreaText(`${node.unit.cve_ur} ${node.unit.desc_ur}`).includes(term);
        const children = node.children
            .map((child) => visit(child, textMatch))
            .filter((child): child is AreaNode => child !== null);
        const ownMatch = textMatch && matchesForm(node.form, filter);

        return ownMatch || children.length ? { ...node, children, contextOnly: !ownMatch } : null;
    }

    return nodes.map((node) => visit(node, false)).filter((node): node is AreaNode => node !== null);
}

export function expandableAreaIds(nodes: AreaNode[]): string[] {
    return nodes.flatMap((node) => [node.unit.id_ur, ...expandableAreaIds(node.children)]);
}
