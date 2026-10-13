import { useId, useMemo, type ReactNode } from 'react';
import { Autocomplete, Box, MenuItem, TextField, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { CheckCircleOutlined, WarningAmber, HighlightOff, RemoveCircleOutlined } from '@mui/icons-material';
import type { SystemRow } from '@/types/tdv2';
import type { RecordAutocompleteEvents } from '@/lib/record-autocomplete';

export const toolOptions = [
    ['sii_v2', 'SIIv2'], ['excel', 'Hoja de cálculo (Excel)'],
    ['correo', 'Correo electrónico institucional'], ['formulario', 'Formulario en línea'], ['papel', 'Formato impreso (papel)'], ['otra', 'Otra (escríbela)'],
] as const;
export const useOptions = [
    ['registrar', 'Registrar solicitudes'], ['agendar', 'Agendar o programar'], ['consultar', 'Consultar información'],
    ['seguimiento', 'Dar seguimiento'], ['avisos', 'Enviar avisos o notificaciones'], ['reportes', 'Generar reportes'],
    ['autorizar', 'Autorizar o aprobar'], ['otro', 'Otro (escríbelo)'],
] as const;
export const stateOptions = [
    ['bien', 'Funciona bien', 'success.main', CheckCircleOutlined], ['fallas', 'Funciona con fallas', 'warning.main', WarningAmber],
    ['no_funciona', 'No funciona', 'error.main', HighlightOff], ['sin_uso', 'Ya no se usa', 'text.secondary', RemoveCircleOutlined],
] as const;
export type SiiModule = { id: string; descripcion: string };
export type ModuleCatalog = { estado: 'cargando' | 'disponible' | 'pendiente' | 'error'; modulos: SiiModule[] };
type SelectEvents = { readOnly: boolean; open: boolean; onOpen: () => Promise<void>; onClose: () => void };

function DependentField({ label, children }: { label: string; children: ReactNode }) {
    return <Box className="system-dependent-field" role="group" aria-label={label}
        sx={{ display: 'grid', gap: 1, minWidth: 0, ml: { xs: .5, sm: .75 }, pl: { xs: 1, sm: 1.25 }, pt: 1.5,
            borderLeft: '2px solid', borderColor: theme => alpha(theme.palette.primary.main, .28) }}>
        {children}
    </Box>;
}

export default function SystemField({ row, field, index, readOnly, events, moduleEvents, change, catalog, errors }: {
    row: SystemRow; field: 'sistema' | 'uso' | 'estado'; index: number; readOnly: boolean;
    events: (field: string) => SelectEvents; change: (field: keyof SystemRow, value: string) => void;
    catalog: ModuleCatalog; errors?: Record<string, string>;
    moduleEvents: RecordAutocompleteEvents;
}) {
    const options = field === 'sistema' ? toolOptions : field === 'uso' ? useOptions : stateOptions;
    const label = field === 'sistema' ? 'Sistema o herramienta' : field === 'uso' ? '¿Para qué se usa?' : '¿Cómo funciona?';
    const value = row[field] || '';
    const historical = !!value && !options.some(o => o[0] === value);
    const optionLabel = (key: string) => {
        if (field !== 'estado') return options.find(o => o[0] === key)?.[1]
            || (field === 'sistema' && key === 'portal_cast' ? 'Portal del CAST de uso de técnicos' : key);
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
    const selected = useMemo(() => row.moduloSiiId ? {
        id: row.moduloSiiId,
        descripcion: row.moduloSiiDescripcion || catalog.modulos.find(m => m.id === row.moduloSiiId)?.descripcion || 'Descripción no disponible',
    } : null, [row.moduloSiiId, row.moduloSiiDescripcion, catalog.modulos]);
    const moduleValueId = useId();
    return <Box sx={{ display: 'grid', gap: 1, minWidth: 0 }}>
        <TextField fullWidth select value={value} onChange={e => change(field, e.target.value)}
            error={!!errors?.[field]} helperText={errors?.[field] || (historical ? 'Respuesta histórica' : undefined)}
            slotProps={{ select: { ...events(field), displayEmpty: true,
                renderValue: v => v ? optionLabel(String(v)) : <Typography component="span" color="text.secondary" sx={{ fontSize: 'inherit' }}>Selecciona…</Typography>,
                SelectDisplayProps: { 'aria-label': `${label} ${index + 1}`, ...{ 'data-field': field } },
                sx: { '& .MuiSelect-select': { whiteSpace: 'normal', display: 'flex', alignItems: 'center', minHeight: '1.5em' } },
            } }}>
            <MenuItem value="" disabled>Selecciona…</MenuItem>
            {options.map(o => <MenuItem key={o[0]} value={o[0]} sx={{ whiteSpace: 'normal', maxWidth: 420 }}>{optionLabel(o[0])}</MenuItem>)}
            {historical && <MenuItem value={value} disabled sx={field === 'sistema' && value === 'portal_cast' ? { display: 'none' } : undefined}>
                {optionLabel(value)} · Respuesta histórica
            </MenuItem>}
        </TextField>
        {field === 'sistema' && value === 'otra' && detail('sistemaOtro', 'Nombre de la herramienta')}
        {field === 'uso' && value === 'otro' && detail('usoOtro', 'Describe el uso')}
        {field === 'sistema' && value === 'sii_v2' && <DependentField label="Módulo de SIIv2">
            <Autocomplete options={moduleEvents.selectionDisabled ? [] : catalog.modulos} value={selected} getOptionLabel={o => o.descripcion} getOptionKey={o => o.id}
                className={`sii-module-select multiline-record-select${selected && !moduleEvents.open ? ' has-selected-value' : ''}`}
                isOptionEqualToValue={(a, b) => a.id === b.id} readOnly={moduleEvents.readOnly} open={moduleEvents.open}
                onOpen={moduleEvents.onOpen} onClose={moduleEvents.onClose}
                onChange={(_, module) => { if (!moduleEvents.selectionDisabled) change('moduloSiiId', module?.id || ''); }}
                getOptionDisabled={() => moduleEvents.selectionDisabled}
                loading={moduleEvents.pending || catalog.estado === 'cargando'}
                loadingText={moduleEvents.pending ? moduleEvents.status : 'Cargando módulos…'}
                noOptionsText={moduleEvents.status || 'Sin módulos disponibles para esta búsqueda'}
                renderValue={(module, _getItemProps, ownerState) => ownerState.popupOpen ? null
                    : <span id={moduleValueId} className="sii-module-value record-selected-value">{module.descripcion}</span>}
                slotProps={{ clearIndicator: { disabled: moduleEvents.selectionDisabled }, paper: { sx: { fontSize: 13 } },
                    popper: { className: 'record-select-popup' },
                    status: { onMouseDown: event => event.preventDefault() },
                    listbox: { sx: { fontFamily: 'inherit', fontSize: 13, fontWeight: 400,
                    '& .MuiAutocomplete-option': { whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', alignItems: 'flex-start' } } } }}
                renderInput={params => <TextField {...params} label="Módulo de SIIv2" placeholder="Busca un módulo…"
                    error={!!errors?.moduloSiiId} helperText={errors?.moduloSiiId}
                    slotProps={{ ...params.slotProps, htmlInput: { ...params.slotProps.htmlInput,
                        'aria-busy': moduleEvents.pending,
                        'aria-describedby': [selected && !moduleEvents.open ? moduleValueId : null,
                            errors?.moduloSiiId ? `${params.id}-helper-text` : null].filter(Boolean).join(' ') || undefined,
                        'data-catalog-search': true, 'data-field': 'moduloSiiId', 'aria-label': `Módulo de SIIv2 ${index + 1}` } }} />}
                renderOption={(props, module) => <li {...props} key={module.id}>{module.descripcion}</li>} />
            {catalog.estado !== 'disponible' && <Typography variant="caption" role="status" color={catalog.estado === 'error' ? 'error.main' : 'text.secondary'}>
                {catalog.estado === 'cargando' ? 'Cargando catálogo local…' : catalog.estado === 'error' ? 'No fue posible consultar el catálogo local de módulos. Puedes continuar con el formulario.' : 'El catálogo de módulos de SIIv2 aún no está disponible'}
            </Typography>}
            {selected && catalog.estado === 'disponible' && !catalog.modulos.some(m => m.id === selected.id)
                && <Typography variant="caption">Módulo histórico · Ya no está disponible para nuevas selecciones.</Typography>}
        </DependentField>}
    </Box>;
}
