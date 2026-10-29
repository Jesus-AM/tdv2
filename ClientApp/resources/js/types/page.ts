import type { Auth } from './auth';
import type { Preview } from './tdv2';
export interface ApplicationOption { name: string; path: string }
export interface SharedProps {
    name: string;
    auth: Auth;
    simulacion: Preview | null;
    representacion: {
        application?: 'tdv2' | 'competencias-digitales';
        id: string; email: string; nombre: string; escritura: boolean; expira_en: string; id_ur: string | null;
    } | null;
    contextoEdicion: string;
    photoContext?: string | null;
    photoApplication?: string;
    csrfToken: string;
    session: { lifetime_ms: number };
    routes: { inicio: string; logout: string; home: string; connect: string };
    access?: { message: string; status: number; options?: ApplicationOption[] };
    available?: { options: ApplicationOption[]; unavailable: boolean };
    [key: string]: unknown;
}
export interface Page { component: string; props: SharedProps; url: string }
