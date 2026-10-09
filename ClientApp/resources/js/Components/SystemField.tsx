import { useMemo } from 'react';
import { Autocomplete, Box, MenuItem, TextField, Typography } from '@mui/material';
import { CheckCircleOutlined, WarningAmber, HighlightOff, RemoveCircleOutlined } from '@mui/icons-material';
import type { SystemRow } from '@/types/tdv2';

export const toolOptions = [
    ['sii_v2', 'SIIv2'], ['portal_cast', 'Portal del CAST de uso de técnicos'], ['excel', 'Hoja de cálculo (Excel)'],
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
export type SiiModule = { id: string; descripcion: string; label: string };
export type ModuleCatalog = { estado: 'cargando' | 'disponible' | 'pendiente' | 'error'; modulos: SiiModule[] };
type SelectEvents = { readOnly: boolean; open: boolean; onOpen: () => Promise<void>; onClose: () => void };

export default function SystemField({ row, field, index, readOnly, events, change, catalog, errors }: {
    row: SystemRow; field: 'sistema' | 'uso' | 'estado'; index: number; readOnly: boolean;
    events: (field: string) => SelectEvents; change: (field: keyof SystemRow, value: string) => void;
    catalog: ModuleCatalog; errors?: Record<string, string>;
}) {
    const options = field === 'sistema' ? toolOptions : field === 'uso' ? useOptions : stateOptions;
    const label = field === 'sistema' ? 'Sistema o herramienta' : field === 'uso' ? '¿Para qué se usa?' : '¿Cómo funciona?';
    const value = row[field] || '';
    const historical = !!value && !options.some(o => o[0] === value);
    const optionLabel = (key: string) => {
        if (field !== 'estado') return options.find(o => o[0] === key)?.[1] || key;
        const option = stateOptions.find(o => o[0] === key);
        if (!option) return key;
        const Icon = option[3];
        return <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', gap: .7 }}>
            <Icon aria-hidden="true" sx={{ fontSize: 19, color: option[2], flexShrink: 0 }} />{option[1]}
        </Box>;
    };
    const detail = (key: 'sistemaOtro' | 'usoOtro', title: string) => <TextField fullWidth label={title} value={row[key] || ''}
        multiline minRows={2} maxRows={4} onChange={e => change(key, e.target.value)} error={!!errors?.[key]} helperText={errors?.[key]}
        slotProps={{ input: { readOnly }, htmlInput: { 'data-field': key, 'aria-label': `${title} ${index + 1}` } }} />;
    const selected = useMemo(() => row.moduloSiiId ? {
        id: row.moduloSiiId, descripcion: row.moduloSiiDescripcion || '',
        label: row.moduloSiiDescripcion ? `${row.moduloSiiDescripcion} · ID ${row.moduloSiiId}`
            : catalog.modulos.find(m => m.id === row.moduloSiiId)?.label || `ID ${row.moduloSiiId}`,
    } : null, [row.moduloSiiId, row.moduloSiiDescripcion, catalog.modulos]);
    const moduleEvents = events('moduloSiiId');
    return <Box sx={{ display: 'grid', gap: 1.5, minWidth: field === 'sistema' ? 270 : 220 }}>
        <TextField fullWidth select value={value} onChange={e => change(field, e.target.value)}
            error={!!errors?.[field]} helperText={errors?.[field] || (historical ? 'Respuesta histórica' : undefined)}
            slotProps={{ select: { ...events(field), displayEmpty: true,
                renderValue: v => v ? optionLabel(String(v)) : <Typography component="span" color="text.secondary" sx={{ fontSize: 'inherit' }}>Selecciona…</Typography>,
                SelectDisplayProps: { 'aria-label': `${label} ${index + 1}`, ...{ 'data-field': field } },
                sx: { '& .MuiSelect-select': { whiteSpace: 'normal', display: 'flex', alignItems: 'center', minHeight: '1.5em' } },
            } }}>
            <MenuItem value="" disabled>Selecciona…</MenuItem>
            {options.map(o => <MenuItem key={o[0]} value={o[0]} sx={{ whiteSpace: 'normal', maxWidth: 420 }}>{optionLabel(o[0])}</MenuItem>)}
            {historical && <MenuItem value={value} disabled>{value} · Respuesta histórica</MenuItem>}
        </TextField>
        {field === 'sistema' && value === 'otra' && detail('sistemaOtro', 'Nombre de la herramienta')}
        {field === 'uso' && value === 'otro' && detail('usoOtro', 'Describe el uso')}
        {field === 'sistema' && value === 'sii_v2' && <>
            <Autocomplete options={catalog.modulos} value={selected} getOptionLabel={o => o.label} getOptionKey={o => o.id}
                isOptionEqualToValue={(a, b) => a.id === b.id} readOnly={moduleEvents.readOnly} open={moduleEvents.open}
                onOpen={moduleEvents.onOpen} onClose={moduleEvents.onClose}
                onChange={(_, module) => change('moduloSiiId', module?.id || '')}
                loading={catalog.estado === 'cargando'} loadingText="Cargando módulos…" noOptionsText="Sin módulos disponibles para esta búsqueda"
                renderInput={params => <TextField {...params} label="Módulo de SIIv2" placeholder="Busca un módulo…"
                    error={!!errors?.moduloSiiId} helperText={errors?.moduloSiiId}
                    slotProps={{ ...params.slotProps, htmlInput: { ...params.slotProps.htmlInput, 'data-catalog-search': true, 'data-field': 'moduloSiiId', 'aria-label': `Módulo de SIIv2 ${index + 1}` } }} />}
                renderOption={(props, module) => <li {...props} key={module.id}>{module.label}</li>} />
            {catalog.estado !== 'disponible' && <Typography variant="caption" role="status" color={catalog.estado === 'error' ? 'error.main' : 'text.secondary'}>
                {catalog.estado === 'cargando' ? 'Cargando catálogo local…' : catalog.estado === 'error' ? 'No fue posible consultar el catálogo local de módulos. Puedes continuar con el formulario.' : 'El catálogo de módulos de SIIv2 aún no está disponible'}
            </Typography>}
            {selected && catalog.estado === 'disponible' && !catalog.modulos.some(m => m.id === selected.id)
                && <Typography variant="caption">Módulo histórico · Ya no está disponible para nuevas selecciones.</Typography>}
        </>}
    </Box>;
}
