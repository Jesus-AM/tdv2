import type { Answer, Tool } from './types';

// Ayudas de captura: no alteran el instrumento ni los informes congelados.
export const rubricBlock = (id: string) => ['ciber', 'datos', 'sii'].includes(id);
export const descriptions: Record<string, string> = {
    m365: 'Herramientas de Microsoft 365 incluidas en tu cuenta institucional.',
    ciber: 'Prácticas para proteger tus cuentas, equipos e información.',
    datos: 'Calidad, protección, transparencia y uso de los datos institucionales.',
    sii: 'Módulos institucionales de planeación, adquisiciones, viajes, transporte, pagos y documentos.',
    ia: 'Principios de uso y las 22 herramientas del Manual práctico de inteligencias artificiales de la UACJ.',
};
export const instructions = (id: string) => rubricBlock(id)
    ? 'Marca lo que realizas en Básico, Intermedio y Avanzado. Alcanzas un nivel con 3 de sus 4 actividades y el nivel anterior cumplido. Si aún no realizas ninguna, elige Inicial; si no corresponde a tus funciones, No aplica.'
    : `Elige tu nivel: Básico, Intermedio o Avanzado. Cada nivel incluye los anteriores. Si aún no lo ${id === 'm365' ? 'usas' : 'haces'}, elige Inicial; si no corresponde a tus funciones, No aplica.`;

// Los objetos inicializados como pending no son respuestas. Una rúbrica parcial sí lo es.
export const unanswered = (answer?: Answer) => !answer || answer.state === 'pending' && answer.level == null && !answer.checks;
export const pendingTools = (tools: Tool[], answers: Record<string, Answer>) => tools.filter(t => unanswered(answers[t.id]));
export function markInitial(tools: Tool[], answers: Record<string, Answer>, block: string): Record<string, Answer> {
    const result = { ...answers };
    for (const t of pendingTools(tools, answers)) result[t.id] = rubricBlock(block) ? { state: 'level', checks: '000000000000' } : { state: 'level', level: 0 };
    return result;
}
