// Textos de la plantilla histórica: sólo cambia su presentación. No transformar respuestas libres.
const questions: Record<string, string> = {
    '¿Los procesos del área están documentados (existe un procedimiento escrito)?': '¿Los procedimientos del área están documentados por escrito?',
    '¿Con qué frecuencia se revisan o actualizan los procesos?': '¿Con qué frecuencia se revisan o actualizan los procedimientos?',
    '¿Qué tanto depende el proceso del conocimiento de una sola persona?': '¿Qué tanto depende el procedimiento del conocimiento de una sola persona?',
    '¿Cómo se realiza principalmente el proceso hoy?': '¿Cómo se realiza principalmente el procedimiento hoy?',
    '¿Existen indicadores para medir el desempeño del proceso?': '¿Existen indicadores para medir el desempeño del procedimiento?',
    '¿Cuáles son los principales cuellos de botella, demoras o pasos que generan fricción en el proceso?': '¿Cuáles son los principales cuellos de botella, demoras o pasos que generan fricción en el procedimiento?',
    '¿Qué parte del proceso sería prioritario automatizar o digitalizar y por qué?': '¿Qué parte del procedimiento conviene automatizar o digitalizar primero y por qué?',
    '¿Qué riesgos o puntos de control hacen falta en el proceso actual?': '¿Qué riesgos o puntos de control hacen falta en el procedimiento actual?',
    '¿Qué pasos considera innecesarios, repetidos o que podrían eliminarse o simplificarse?': '¿Qué pasos consideras innecesarios, repetidos o que podrían eliminarse o simplificarse?',
    '¿Qué mejora tendría el mayor impacto para el usuario que se atiende?': '¿Qué mejora tendría el mayor impacto para las personas que atiende tu área?',
};
const questionOptions: Record<string, string> = {
    'No o muy pocos': 'Ninguno o muy pocos',
    'El conocimiento está compartido': 'El conocimiento se comparte',
    'Mezcla de sistema y trabajo manual': 'Con un sistema y trabajo manual',
};
const origins: Record<string, string> = {
    'Se origina en este proceso': 'Se origina en este procedimiento',
    'Se origina en otro proceso': 'Se origina en otro procedimiento',
};
export const questionLabel = (text: string) => questions[text] ?? text;
export const questionOptionLabel = (text: string) => questionOptions[text] ?? text;
export const originLabel = (text: string) => origins[text] ?? text;
export const countLabel = (count: number, singular: string, plural = singular + 's') => `${count} ${count === 1 ? singular : plural}`;
