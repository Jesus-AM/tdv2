import type { ProcessRow, SystemRow } from '../types/tdv2';

export const toolCatalog = [
    { id: 'sii_v2', label: 'Sistema Integral de Información', group: 'Sistemas institucionales' },
    { id: 'aplicaciones_web', label: 'Aplicaciones web', group: 'Sistemas institucionales' },
    { id: 'aplicaciones_moviles', label: 'Aplicaciones móviles', group: 'Sistemas institucionales' },
    { id: 'microsoft_365', label: 'Microsoft 365', group: 'Productividad y colaboración' },
    { id: 'limesurvey', label: 'LimeSurvey', group: 'Formularios y encuestas' },
    { id: 'power_bi', label: 'Power BI', group: 'Análisis y visualización de datos' },
    { id: 'tableau', label: 'Tableau', group: 'Análisis y visualización de datos' },
    { id: 'adobe_creative_cloud', label: 'Adobe Creative Cloud', group: 'Diseño y producción multimedia' },
    { id: 'otra', label: 'Otra (escríbela)', group: '' },
];
export const historicalTools: Record<string, string> = { portal_cast: 'Portal del CAST de uso de técnicos', excel: 'Hoja de cálculo (Excel)',
    correo: 'Correo electrónico institucional', formulario: 'Formulario en línea', papel: 'Formato impreso (papel)' };
export const suiteCatalog = {
    microsoft_365: { field: 'aplicacionesMicrosoft365', label: 'Aplicaciones de Microsoft 365', options: [
        ['excel', 'Excel'], ['word', 'Word'], ['powerpoint', 'PowerPoint'], ['outlook', 'Outlook — correo institucional'],
        ['teams', 'Teams'], ['forms', 'Forms — formularios'], ['onedrive', 'OneDrive'], ['sharepoint', 'SharePoint'],
    ] },
    adobe_creative_cloud: { field: 'aplicacionesAdobe', label: 'Aplicaciones de Adobe Creative Cloud', options: [
        ['acrobat_pro', 'Acrobat Pro'], ['acrobat_reader', 'Acrobat Reader'], ['adobe_express', 'Adobe Express'], ['adobe_scan', 'Adobe Scan'],
        ['after_effects', 'After Effects'], ['animate', 'Animate'], ['audition', 'Audition'], ['bridge', 'Bridge'],
        ['character_animator', 'Character Animator'], ['dreamweaver', 'Dreamweaver'], ['fresco', 'Fresco'], ['illustrator', 'Illustrator'],
        ['incopy', 'InCopy'], ['indesign', 'InDesign'], ['lightroom', 'Lightroom'], ['lightroom_classic', 'Lightroom Classic'],
        ['media_encoder', 'Media Encoder'], ['photoshop', 'Photoshop'], ['photoshop_express', 'Photoshop Express'], ['premiere', 'Premiere'],
    ] },
} as const;
export type SuiteField = typeof suiteCatalog[keyof typeof suiteCatalog]['field'];
export const normalizedOther = (name: string) => name.trim().replace(/\s+/gu, ' ').toUpperCase();
function key(row: SystemRow, procedures: ProcessRow[]): string | null {
    const procedure = row.proceso && procedures.find(p => p.codigo === row.proceso);
    if (!procedure || !row.sistema) return null;
    const other = row.sistema === 'otra' ? normalizedOther(row.sistemaOtro || '') : '';
    if (row.sistema === 'otra' && !other) return null;
    return JSON.stringify([procedure.id, row.sistema, other]);
}
export function toolUsed(row: SystemRow, tool: string, rows: SystemRow[], procedures: ProcessRow[]): boolean {
    if (tool === 'otra' || tool === row.sistema) return false;
    const target = key({ ...row, sistema: tool }, procedures);
    return !!target && rows.some(r => r.id !== row.id && key(r, procedures) === target);
}
export function reassignmentConflict(row: SystemRow, code: string, rows: SystemRow[], procedures: ProcessRow[]): boolean {
    if (row.proceso === code) return false;
    const target = key({ ...row, proceso: code }, procedures);
    return !!target && rows.some(r => r.id !== row.id && key(r, procedures) === target);
}
