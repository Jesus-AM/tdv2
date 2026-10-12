import { Autocomplete, Box, TextField, Tooltip } from '@mui/material';
import FieldHelp from './FieldHelp';
import { useMemo } from 'react';

type Option = { value: string; label: string };
export default function ProcedureSelect({ value, options, historicalLabel, label, error, events, onChange }: {
    value: string; options: Option[]; historicalLabel?: string; label: string; error?: string;
    events: { readOnly: boolean; open: boolean; onOpen: () => Promise<void>; onClose: () => void };
    onChange: (value: string) => void;
}) {
    const selectedLabel = options.find(option => option.value === value)?.label || historicalLabel || value;
    // Una entrega de presencia no debe reiniciar el texto de búsqueda del Autocomplete.
    const selected = useMemo(() => value ? { value, label: selectedLabel } : null, [value, selectedLabel]);
    return <Box sx={{ display: 'flex', alignItems: 'flex-start', minWidth: 0 }}>
        <Autocomplete options={options} value={selected} getOptionLabel={option => option.label}
            getOptionKey={option => option.value} isOptionEqualToValue={(a, b) => a.value === b.value}
            // La apertura desde foco sigue a la reserva solicitada por la fila; mousedown puede precederla.
            openOnFocus readOnly={events.readOnly} open={events.open} onOpen={events.onOpen} onClose={events.onClose}
            onChange={(event, option, reason) => {
                // Vaciar la búsqueda no borra la asociación. El botón Quitar selección sí lo hace.
                if (reason !== 'clear' || event.type === 'click') onChange(option?.value || '');
            }}
            openText="Buscar procedimiento" closeText="Cerrar opciones" clearText="Quitar selección"
            noOptionsText="No hay procedimientos disponibles para esta búsqueda"
            sx={{ flex: 1, minWidth: 0, '& input': { textOverflow: 'ellipsis' } }}
            slotProps={{ popper: { sx: { width: 'min(560px, calc(100vw - 24px)) !important' },
                modifiers: [{ name: 'preventOverflow', options: { padding: 12, rootBoundary: 'viewport' } }] },
                listbox: { sx: { '& .MuiAutocomplete-option': { whiteSpace: 'normal', overflowWrap: 'anywhere', alignItems: 'flex-start' } } } }}
            renderOption={(props, option) => <li {...props} key={option.value}>{option.label}</li>}
            renderInput={params => <Tooltip title={events.open ? '' : selected?.label || ''} describeChild>
                <TextField {...params} placeholder="Busca por código o nombre…" error={!!error} helperText={error}
                    slotProps={{ ...params.slotProps, htmlInput: { ...params.slotProps.htmlInput,
                        'aria-label': label, 'data-field': 'proceso', 'data-catalog-search': true } }} />
            </Tooltip>} />
        {selected && <FieldHelp title={`Procedimiento seleccionado · ${value}`} explanation={selected.label} />}
    </Box>;
}
