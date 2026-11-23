import type { Answer, Tool } from './types';

// Ayudas de captura: no alteran el instrumento ni los informes congelados.
interface SectionInstructions {
    title: string;
    subtitle?: string;
    paragraphs: string[];
    required?: string;
    options?: string[];
    consecutiveLevels?: boolean;
}
export const optionLabels = ['Básico, Intermedio o Avanzado.', 'Inicial.', 'No aplica.', 'No aplica a mis funciones, pero sé hacerlo.', 'Sin respuesta.'];
export const sectionInstructions: Record<string, SectionInstructions> = {
    perfil: {
        title: 'Perfil',
        subtitle: 'Comencemos por conocerte',
        paragraphs: ['Completa tu perfil antes de iniciar el diagnóstico. Esta información nos permite asociar tus resultados con tu dependencia e identificar qué capacitación puede ayudarte según las funciones de tu puesto.'],
        required: 'Los campos marcados con un asterisco (*) son obligatorios.',
    },
    m365: {
        title: 'Microsoft 365',
        subtitle: 'Herramientas de Microsoft 365',
        paragraphs: ['En esta sección encontrarás las herramientas de Microsoft 365. Para cada una, selecciona la opción que mejor describa lo que puedes hacer actualmente en tu trabajo.'],
        options: [
            'Elige el nivel más alto cuyas actividades puedas realizar, incluidas las de los niveles anteriores.',
            'Selecciónalo si la herramienta corresponde a tus funciones y todavía no la utilizas.',
            'Selecciónalo si la herramienta no corresponde a tus funciones. Esta opción queda fuera del puntaje.',
            'Elige esta opción si sabes utilizar la herramienta, aunque tu puesto no la requiera. Selecciona tu nivel: Básico, Intermedio o Avanzado. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
            'Si dejas una herramienta sin seleccionar, quedará pendiente.',
        ],
    },
    ciber: {
        title: 'Ciberseguridad',
        paragraphs: ['En esta sección encontrarás prácticas para proteger tus cuentas, equipos e información en el trabajo. En cada tema, marca todas las actividades que ya realizas, aunque sean de distintos niveles.'],
        options: [
            'Alcanzas un nivel cuando realizas al menos 3 de sus 4 actividades.',
            'Selecciónalo si las prácticas del tema corresponden a tus funciones y todavía no realizas ninguna.',
            'Selecciónalo si las prácticas del tema no corresponden a tus funciones. Esta opción queda fuera del puntaje.',
            'Si sabes realizar prácticas de este tema que tu puesto no requiere, marca las actividades que sabes hacer para determinar tu nivel. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
            'Si dejas un tema sin responder, quedará pendiente.',
        ],
        consecutiveLevels: true,
    },
    datos: {
        title: 'Gobernanza de datos',
        paragraphs: ['En esta sección encontrarás prácticas relacionadas con la calidad, la protección, la transparencia y el uso de los datos institucionales. En cada tema, marca todas las actividades que ya realizas, aunque sean de distintos niveles.'],
        options: [
            'Alcanzas un nivel cuando realizas al menos 3 de sus 4 actividades.',
            'Selecciónalo si las actividades del tema corresponden a tus funciones y todavía no realizas ninguna.',
            'Selecciónalo si las actividades del tema no corresponden a tus funciones. Esta opción queda fuera del puntaje.',
            'Si sabes realizar actividades de este tema que tu puesto no requiere, marca las que sabes hacer para determinar tu nivel. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
            'Si dejas un tema sin responder, quedará pendiente.',
        ],
        consecutiveLevels: true,
    },
    sii: {
        title: 'Sistema Integral de Información',
        paragraphs: [
            'En esta sección encontrarás actividades de los módulos institucionales generales como POA adquisiciones, egresos, gestión documental, boletos de avión y transporte.',
            'En cada módulo, marca todas las actividades que ya realizas, aunque sean de distintos niveles.',
        ],
        options: [
            'Alcanzas un nivel cuando realizas al menos 3 de sus 4 actividades.',
            'Selecciónalo si el módulo corresponde a tus funciones y todavía no realizas ninguna de las actividades indicadas.',
            'Selecciónalo si el módulo no corresponde a tus funciones. Esta opción queda fuera del puntaje.',
            'Si sabes realizar actividades de este módulo que tu puesto no requiere, marca las que sabes hacer para determinar tu nivel. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
            'Si dejas un módulo sin responder, quedará pendiente.',
        ],
        consecutiveLevels: true,
    },
    ia: {
        title: 'Inteligencia artificial',
        paragraphs: [
            'En esta sección encontrarás principios para usar la inteligencia artificial (IA) de manera responsable en tu trabajo. Las actividades se centran en lo que puedes hacer con las versiones básicas y gratuitas de estas herramientas, como redactar una solicitud clara, revisar la respuesta antes de utilizarla y proteger la información institucional que manejas.',
            'Para cada tema, selecciona la opción que mejor describa lo que puedes hacer actualmente.',
        ],
        options: [
            'Elige el nivel más alto cuyas actividades puedas realizar, incluidas las de los niveles anteriores. Los niveles describen tus habilidades, independientemente del plan de la herramienta que utilices.',
            'Selecciónalo si las actividades del tema corresponden a tus funciones y todavía no realizas ninguna.',
            'Selecciónalo si las actividades del tema no corresponden a tus funciones. Esta opción queda fuera del puntaje.',
            'Elige esta opción si sabes realizar las actividades del tema, aunque tu puesto no las requiera. Selecciona tu nivel: Básico, Intermedio o Avanzado. Este nivel cuenta para tus conocimientos digitales generales y queda fuera del puntaje de tu puesto.',
            'Si dejas un tema sin responder, quedará pendiente.',
        ],
    },
};

export const rubricBlock = (id: string) => ['ciber', 'datos', 'sii'].includes(id);

// Los objetos inicializados como pending no son respuestas. Una rúbrica parcial sí lo es.
export const unanswered = (answer?: Answer) => !answer || answer.state === 'pending' && answer.level == null && !answer.checks;
export const pendingTools = (tools: Tool[], answers: Record<string, Answer>) => tools.filter(t => unanswered(answers[t.id]));
export function markInitial(tools: Tool[], answers: Record<string, Answer>, block: string): Record<string, Answer> {
    const result = { ...answers };
    for (const t of pendingTools(tools, answers)) result[t.id] = rubricBlock(block) ? { state: 'level', checks: '000000000000' } : { state: 'level', level: 0 };
    return result;
}
