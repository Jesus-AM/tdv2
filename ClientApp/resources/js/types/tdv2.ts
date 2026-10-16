export interface Unit {
    id_ur: string;
    cve_ur: string;
    desc_ur: string;
    nivel_ur: number;
    tipo_ur?: string | null;
    id_ur_pertenece: string | null;
    id_ur_principal?: string | null;
    ejercicio: number;
}
export interface FormRow extends Unit {
    enviado_en?: string | null;
    propia?: boolean;
    editable: boolean;
    porcentaje: number;
    actualizado_en: string | null;
    actualizado_por: string | null;
    url: string;
}
export interface Preview {
    mode: 'escenario';
    label: string;
    roleLabel: string;
    unit: Unit | null;
    expiresAt: number;
    readOnly: boolean;
}
export interface ProcessRow {
    id: string;
    codigo: string;
    prioridad: string;
    fuente: string;
    area: string;
    tramite: string;
    // Los textos se conservan sólo para consultar instantáneas históricas; la captura nueva usa una colección.
    usuario: string[] | string;
    usuarioOtro?: string;
    resultado: string;
    responsable: string;
    validacion: string;
}
export interface SiiModuleSelection { id: string; descripcion: string }
export type ApplicationKind = 'moviles' | 'web';
export interface ApplicationSelection extends SiiModuleSelection { catalogo: ApplicationKind }
export interface SystemRow {
    id: string;
    proceso: string;
    sistema: string;
    uso: string;
    estado: string;
    fallas: string;
    sistemaOtro?: string;
    usoOtro?: string;
    moduloSiiId?: string;
    moduloSiiDescripcion?: string;
    modulosSii?: SiiModuleSelection[];
    aplicacionesMoviles?: ApplicationSelection[];
    aplicacionesWeb?: ApplicationSelection[];
    aplicacionesMicrosoft365?: string[];
    aplicacionesAdobe?: string[];
    procesos?: string[];
}
export interface DataRow {
    id: string;
    proceso: string;
    dato: string;
    fuente: string;
    origen: string;
    detalle: string;
}
export interface Evaluation {
    criterio: string;
    valor: string;
    obs: string;
}
export interface Question {
    pregunta: string;
    tipo: 'opcion' | 'abierta';
    opciones: string[] | null;
    marcada: boolean;
    respuesta: string;
}
export interface Agreement {
    id: string;
    acuerdo: string;
    responsable: string;
    fecha: string;
}
export interface FormContent {
    versionEstructura?: number;
    encabezado: { fecha: string; area: string; responsable: string };
    identificacion: ProcessRow[];
    sistemas: SystemRow[];
    datos: DataRow[];
    evaluaciones: Record<string, Evaluation[]>;
    preguntas: Question[];
    acuerdos: Agreement[];
}
export interface SaveResponse {
    entrega?: import('../Components/FormSubmissionReview').StageReview;
    procedimientosDisponibles?: { id: string; codigo: string; tramite: string }[];
    porcentajeEtapa?: number;
    revisionEtapa?: import('../Components/FormSubmissionReview').SubmissionReview;
    version: number;
    porcentaje: number;
    actualizadoEn: string;
    actualizadoPor: string;
}
export type CollaborationKind = 'local' | 'dependencias';
export interface Collaboration {
    key: string;
    id: number | null;
    nombre: string;
    email: string;
    tipo: CollaborationKind;
    ur_otorgante: string | null;
    id_ur_alcance: string;
    origen: 'central' | 'aplicacion';
    puedeRetirar: boolean;
    alcance: string;
    revocada: boolean;
    pendiente: boolean;
}
export interface EligiblePerson {
    email: string;
    nombre: string;
    id_ur: string;
    adscripcion: string;
    formato: string;
}
