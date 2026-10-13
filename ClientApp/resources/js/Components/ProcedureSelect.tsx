import { Autocomplete, TextField } from '@mui/material';
import { useId, useMemo } from 'react';
import type { RecordAutocompleteEvents } from '@/lib/record-autocomplete';

type Option = { value: string; label: string };
export default function ProcedureSelect({ value, options, historicalLabel, label, error, events, onChange }: {
    value: string; options: Option[]; historicalLabel?: string; label: string; error?: string;
    events: RecordAutocompleteEvents;
    onChange: (value: string) => void;
}) {
    const selectedLabel = options.find(option => option.value === value)?.label || historicalLabel || value;
    // Una entrega de presencia no debe reiniciar el texto de búsqueda del Autocomplete.
    const selected = useMemo(() => value ? { value, label: selectedLabel } : null, [value, selectedLabel]);
    const selectedValueId = useId();
    return <Autocomplete fullWidth options={events.selectionDisabled ? [] : options} value={selected} getOptionLabel={option => option.label}
            className={`procedure-select multiline-record-select${selected && !events.open ? ' has-selected-value' : ''}`}
            getOptionKey={option => option.value} isOptionEqualToValue={(a, b) => a.value === b.value}
            // La apertura desde foco sigue a la reserva solicitada por la fila; mousedown puede precederla.
            openOnFocus readOnly={events.readOnly} open={events.open} onOpen={events.onOpen} onClose={events.onClose}
            onChange={(event, option, reason) => {
                // Vaciar la búsqueda no borra la asociación. El botón Quitar selección sí lo hace.
                if (!events.selectionDisabled && (reason !== 'clear' || event.type === 'click')) onChange(option?.value || '');
            }}
            openText="Buscar procedimiento" closeText="Cerrar opciones" clearText="Quitar selección"
            getOptionDisabled={() => events.selectionDisabled}
            loading={events.pending} loadingText={events.status}
            noOptionsText={events.status || 'No hay procedimientos disponibles para esta búsqueda'}
            sx={{ minWidth: 0 }}
            renderValue={option => <span id={selectedValueId} className="procedure-value record-selected-value">{option.label}</span>}
            slotProps={{ clearIndicator: { disabled: events.selectionDisabled }, paper: { sx: { fontSize: 13 } },
                status: { onMouseDown: event => event.preventDefault() },
                popper: { className: 'record-select-popup', sx: { width: 'min(560px, calc(100vw - 24px)) !important' },
                modifiers: [{ name: 'preventOverflow', options: { padding: 12, rootBoundary: 'viewport' } }] },
                listbox: { sx: { '& .MuiAutocomplete-option': { whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', alignItems: 'flex-start' } } } }}
            renderOption={(props, option) => <li {...props} key={option.value}>{option.label}</li>}
            renderInput={params => <TextField {...params} placeholder="Busca por código o nombre…" error={!!error} helperText={error}
                    slotProps={{ ...params.slotProps, htmlInput: { ...params.slotProps.htmlInput,
                        'aria-busy': events.pending,
                        'aria-describedby': [selected ? selectedValueId : null,
                            error ? `${params.id}-helper-text` : null].filter(Boolean).join(' ') || undefined,
                        'aria-label': label, 'data-field': 'proceso', 'data-catalog-search': true } }} />} />;
}
