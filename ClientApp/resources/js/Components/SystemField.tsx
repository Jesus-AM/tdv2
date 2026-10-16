import { useMemo, type ReactNode } from 'react';
import { Box, MenuItem, TextField, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { CheckCircleOutlined, WarningAmber, HighlightOff, RemoveCircleOutlined } from '@mui/icons-material';
import type { ApplicationKind, ApplicationSelection, SiiModuleSelection, SystemRow } from '@/types/tdv2';
import type { RecordAutocompleteEvents } from '@/lib/record-autocomplete';
import CatalogMultiSelect, { type CatalogState } from './CatalogMultiSelect';
import ToolSelect from './ToolSelect';
import { historicalTools, suiteCatalog, toolCatalog, type SuiteField } from '@/lib/system-catalog';

export const toolOptions = toolCatalog.map(o => [o.id, o.label] as const);
export const useOptions = [
    ['registrar', 'Registrar solicitudes'], ['agendar', 'Agendar o programar'], ['consultar', 'Consultar información'],
    ['seguimiento', 'Dar seguimiento'], ['avisos', 'Enviar avisos o notificaciones'], ['reportes', 'Generar reportes'],
    ['autorizar', 'Autorizar o aprobar'], ['otro', 'Otro (escríbelo)'],
] as const;
export const stateOptions = [
    ['bien', 'Funciona bien', 'success.main', CheckCircleOutlined], ['fallas', 'Funciona con fallas', 'warning.main', WarningAmber],
    ['no_funciona', 'No funciona', 'error.main', HighlightOff], ['sin_uso', 'Ya no se usa', 'text.secondary', RemoveCircleOutlined],
] as const;
export type SiiModule = SiiModuleSelection;
export type ModuleCatalog = { estado: 'cargando' | 'disponible' | 'pendiente' | 'error'; modulos: SiiModule[] };
export type ApplicationCatalog = { estado: CatalogState; aplicaciones: ApplicationSelection[] };
export const applicationFields = { moviles: 'aplicacionesMoviles', web: 'aplicacionesWeb' } as const;
type SelectEvents = { readOnly: boolean; open: boolean; onOpen: () => Promise<void>; onClose: () => void };

function DependentField({ label, children }: { label: string; children: ReactNode }) {
    return <Box className="system-dependent-field" role="group" aria-label={label}
        sx={{ display: 'grid', gap: 1, minWidth: 0, ml: { xs: .5, sm: .75 }, pl: { xs: 1, sm: 1.25 }, pt: 1.5,
            borderLeft: '2px solid', borderColor: theme => alpha(theme.palette.primary.main, .28) }}>
        {children}
    </Box>;
}

export default function SystemField({ row, field, index, readOnly, events, catalogEvents, change, changeModules, changeApplications, changeSuite, unavailable, catalog, applications, errors }: {
    row: SystemRow; field: 'sistema' | 'uso' | 'estado'; index: number; readOnly: boolean;
    events: (field: string) => SelectEvents;
    change: (field: 'sistema' | 'uso' | 'estado' | 'sistemaOtro' | 'usoOtro', value: string) => void;
    changeModules: (modules: SiiModule[]) => void;
    changeApplications: (kind: ApplicationKind, values: ApplicationSelection[]) => void;
    changeSuite: (field: SuiteField, values: string[]) => void; unavailable: string[];
    catalog: ModuleCatalog; errors?: Record<string, string>;
    applications: Record<ApplicationKind, ApplicationCatalog>;
    catalogEvents: (field: string) => RecordAutocompleteEvents;
}) {
    const options = field === 'sistema' ? toolOptions : field === 'uso' ? useOptions : stateOptions;
    const label = field === 'sistema' ? 'Sistema o herramienta' : field === 'uso' ? '¿Para qué se usa?' : '¿Cómo funciona?';
    const value = row[field] || '';
    const historical = !!value && !options.some(o => o[0] === value);
    const optionLabel = (key: string) => {
        if (field !== 'estado') return options.find(o => o[0] === key)?.[1]
            || (field === 'sistema' ? historicalTools[key] || 'Respuesta histórica' : key);
        const option = stateOptions.find(o => o[0] === key);
        if (!option) return key;
        const Icon = option[3];
        return <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: .7 }}>
            <Icon aria-hidden="true" sx={{ fontSize: 19, color: option[2], flexShrink: 0 }} />{option[1]}
        </Box>;
    };
    const detail = (key: 'sistemaOtro' | 'usoOtro', title: string) => <DependentField label={title}><TextField fullWidth label={title} value={row[key] || ''}
        multiline minRows={2} maxRows={4} onChange={e => change(key, e.target.value)} error={!!errors?.[key]} helperText={errors?.[key]}
        slotProps={{ input: { readOnly }, htmlInput: { 'data-field': key, 'aria-label': `${title} ${index + 1}` } }} /></DependentField>;
    const selected = useMemo(() => row.modulosSii ?? (row.moduloSiiId ? [{
        id: row.moduloSiiId,
        descripcion: row.moduloSiiDescripcion || catalog.modulos.find(m => m.id === row.moduloSiiId)?.descripcion || 'Descripción no disponible',
    }] : []), [row.modulosSii, row.moduloSiiId, row.moduloSiiDescripcion, catalog.modulos]);
    const moduleError = errors?.modulosSii || errors?.moduloSiiId;
    const applicationKind = value === 'aplicaciones_moviles' ? 'moviles' : value === 'aplicaciones_web' ? 'web' : null;
    const suite = suiteCatalog[value as keyof typeof suiteCatalog];
    const suiteOptions = useMemo(() => suite?.options.map(([id, descripcion]) => ({ id, descripcion })) || [], [suite]);
    const suiteSelected = useMemo(() => suite ? (row[suite.field] || []).map(id => suiteOptions.find(o => o.id === id)
        || { id, descripcion: 'Respuesta histórica' }) : [], [suite, suiteOptions, row]);
    return <Box sx={{ display: 'grid', gap: 1, minWidth: 0 }}>
        {field === 'sistema' ? <ToolSelect value={value} index={index} events={catalogEvents('sistema')} unavailable={unavailable}
            error={errors?.sistema} onChange={value => change('sistema', value)} /> : <TextField fullWidth select value={value} onChange={e => change(field, e.target.value)}
            error={!!errors?.[field]} helperText={errors?.[field] || (historical ? 'Respuesta histórica' : undefined)}
            slotProps={{ select: { ...events(field), displayEmpty: true,
                renderValue: v => v ? optionLabel(String(v)) : <Typography component="span" color="text.secondary" sx={{ fontSize: 'inherit' }}>Selecciona…</Typography>,
                SelectDisplayProps: { 'aria-label': `${label} ${index + 1}`, ...{ 'data-field': field } },
                sx: { '& .MuiSelect-select': { whiteSpace: 'normal', display: 'flex', alignItems: 'center', minHeight: '1.5em' } },
            } }}>
            <MenuItem value="" disabled>Selecciona…</MenuItem>
            {options.map(o => <MenuItem key={o[0]} value={o[0]} sx={{ whiteSpace: 'normal', maxWidth: 420 }}>{optionLabel(o[0])}</MenuItem>)}
            {historical && <MenuItem value={value} disabled>
                {optionLabel(value)} · Respuesta histórica
            </MenuItem>}
        </TextField>}
        {field === 'sistema' && value === 'otra' && detail('sistemaOtro', 'Nombre de la herramienta')}
        {field === 'uso' && value === 'otro' && detail('usoOtro', 'Describe el uso')}
        {field === 'sistema' && value === 'sii_v2' && <DependentField label="Módulos del Sistema Integral de Información">
            <CatalogMultiSelect selected={selected} options={catalog.modulos} state={catalog.estado} label="Módulos del Sistema Integral de Información"
                field="modulosSii" noun="módulos" index={index} events={catalogEvents('modulosSii')} error={moduleError} onChange={changeModules} />
        </DependentField>}
        {field === 'sistema' && suite && <DependentField label={suite.label}>
            <CatalogMultiSelect key={suite.field} selected={suiteSelected} options={suiteOptions} state="disponible" label={suite.label}
                field={suite.field} noun="aplicaciones" index={index} events={catalogEvents(suite.field)} error={errors?.[suite.field]}
                onChange={values => changeSuite(suite.field, values.map(v => v.id))} />
        </DependentField>}
        {field === 'sistema' && applicationKind && <DependentField label={applicationKind === 'moviles' ? 'Aplicaciones móviles' : 'Aplicaciones web'}>
            <CatalogMultiSelect key={applicationKind} selected={row[applicationFields[applicationKind]] || []}
                options={applications[applicationKind].aplicaciones} state={applications[applicationKind].estado}
                label={applicationKind === 'moviles' ? 'Aplicaciones móviles' : 'Aplicaciones web'} field={applicationFields[applicationKind]}
                noun="aplicaciones" index={index} events={catalogEvents(applicationFields[applicationKind])}
                error={errors?.[applicationFields[applicationKind]]} onChange={values => changeApplications(applicationKind, values)} />
        </DependentField>}
    </Box>;
}
