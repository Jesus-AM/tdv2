export interface Auth {
    user: { id: number; name: string; email: string } | null;
    roles: { id: number; key: string; name: string }[];
    modules: { id: number; key: string; name: string; route: string; icon: string; parent?: string | null }[];
    canPreview: boolean;
    canRepresent: boolean;
}
