import { useId, useMemo } from 'react';
import { Autocomplete, Box, TextField } from '@mui/material';
import type { RecordAutocompleteEvents } from '@/lib/record-autocomplete';
import { historicalTools, toolCatalog } from '@/lib/system-catalog';

export default function ToolSelect({ value, index, events, unavailable, error, onChange }: {
    value: string; index: number; events: RecordAutocompleteEvents; unavailable: string[]; error?: string; onChange: (value: string) => void;
}) {
    const groupId = useId();
    const selected = useMemo(() => toolCatalog.find(o => o.id === value) || (value
        ? { id: value, label: historicalTools[value] || value, group: '' } : null), [value]);
    const historical = !!value && !toolCatalog.some(o => o.id === value);
    return <Autocomplete options={events.selectionDisabled ? [] : toolCatalog} value={selected} fullWidth openOnFocus
        className={`tool-select multiline-record-select${selected && !events.open ? ' has-selected-value' : ''}`}
        getOptionKey={o => o.id} getOptionLabel={o => o.label} isOptionEqualToValue={(a, b) => a.id === b.id}
        groupBy={o => o.group} renderGroup={params => <li key={params.key} role="presentation">
            {params.group && <Box id={`${groupId}-${params.key}`} className="tool-catalog-group-title">{params.group}</Box>}
            <Box component="ul" role="group" aria-labelledby={params.group ? `${groupId}-${params.key}` : undefined}
                aria-label={params.group ? undefined : 'Otra herramienta'} sx={{ p: 0 }}>{params.children}</Box>
        </li>}
        readOnly={events.readOnly} open={events.open} onOpen={events.onOpen} onClose={events.onClose}
        getOptionDisabled={o => events.selectionDisabled || unavailable.includes(o.id)}
        onChange={(event, option, reason) => {
            if (!events.selectionDisabled && (reason !== 'clear' || event.type === 'click')) onChange(option?.id || '');
        }}
        loading={events.pending} loadingText={events.status} noOptionsText={events.status || 'Sin herramientas para esta búsqueda'}
        openText="Buscar sistema o herramienta" closeText="Cerrar opciones" clearText="Quitar selección"
        renderValue={option => <span className="record-selected-value">{option.label}</span>}
        renderOption={(props, option) => <li {...props} key={option.id}>
            <span>{option.label}{unavailable.includes(option.id) && <Box component="span" sx={{ display: 'block' }}>Ya agregado a este procedimiento</Box>}</span>
        </li>}
        slotProps={{ clearIndicator: { disabled: events.selectionDisabled }, popper: { className: 'record-select-popup' },
            paper: { sx: { fontSize: 13 } }, status: { onMouseDown: event => event.preventDefault() },
            listbox: { sx: { '& .MuiAutocomplete-option': { whiteSpace: 'normal', overflowWrap: 'anywhere' } } } }}
        renderInput={params => <TextField {...params} placeholder="Selecciona o busca…" error={!!error}
            helperText={error || (historical ? 'Respuesta histórica' : undefined)}
            slotProps={{ ...params.slotProps, htmlInput: { ...params.slotProps.htmlInput, 'data-field': 'sistema',
                'data-catalog-search': true, 'aria-label': `Sistema o herramienta ${index + 1}`, 'aria-busy': events.pending } }} />} />;
}
