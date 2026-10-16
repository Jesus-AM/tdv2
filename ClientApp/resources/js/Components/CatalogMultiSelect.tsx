import { useEffect, useId, useMemo, useState } from 'react';
import { Autocomplete, Checkbox, TextField, Typography } from '@mui/material';
import type { RecordAutocompleteEvents } from '@/lib/record-autocomplete';
import type { SiiModuleSelection } from '@/types/tdv2';

export type CatalogState = 'cargando' | 'disponible' | 'pendiente' | 'error';

/** El mismo control conserva apertura, foco y resumen para todas las familias. */
export default function CatalogMultiSelect<T extends SiiModuleSelection>({ selected, options, state, label, field, noun, index, events, error, onChange }: {
    selected: T[]; options: T[]; state: CatalogState; label: string; field: string; noun: 'módulos' | 'aplicaciones';
    index: number; events: RecordAutocompleteEvents; error?: string; onChange: (values: T[]) => void;
}) {
    const selectedValueId = useId();
    const controlId = useId();
    const catalogLabel = label.charAt(0).toLowerCase() + label.slice(1);
    const [search, setSearch] = useState('');
    useEffect(() => { if (!events.open) setSearch(''); }, [events.open]);
    const available = useMemo(() => [...options, ...selected.filter(s => !options.some(o => o.id === s.id))], [options, selected]);
    return <>
        <Typography component="label" htmlFor={controlId} variant="body2">{label}</Typography>
        <Autocomplete id={controlId} multiple disableCloseOnSelect openOnFocus options={events.selectionDisabled ? [] : available} value={selected}
            getOptionLabel={o => o.descripcion} getOptionKey={o => o.id}
            className={`sii-module-select sii-modules-select multiline-record-select${selected.length && !events.open ? ' has-selected-value' : ''}`}
            inputValue={search} onInputChange={(_, text) => setSearch(text)}
            isOptionEqualToValue={(a, b) => a.id === b.id} readOnly={events.readOnly} open={events.open}
            onOpen={events.onOpen} onClose={events.onClose}
            onChange={(_, values) => { if (!events.selectionDisabled) onChange([...new Map(values.map(v => [v.id, v])).values()]); }}
            getOptionDisabled={() => events.selectionDisabled}
            loading={events.pending || state === 'cargando'} loadingText={events.pending ? events.status : `Cargando ${noun}…`}
            noOptionsText={events.status || `Sin ${noun} disponibles para esta búsqueda`}
            renderValue={values => <span id={selectedValueId} className="sii-module-value record-selected-value">
                {values.length === 1 ? values[0].descripcion : `${values.length} ${noun} seleccionad${noun === 'módulos' ? 'os' : 'as'}`}
            </span>}
            openText={`Buscar ${noun}`} closeText="Cerrar opciones" clearText="Quitar selecciones"
            slotProps={{ clearIndicator: { disabled: events.selectionDisabled }, paper: { sx: { fontSize: 13 } },
                popper: { className: 'record-select-popup' }, status: { onMouseDown: event => event.preventDefault() },
                listbox: { sx: { fontFamily: 'inherit', fontSize: 13, fontWeight: 400,
                    '& .MuiAutocomplete-option': { whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', alignItems: 'flex-start' } } } }}
            renderInput={params => <TextField {...params} placeholder={selected.length ? `Busca ${noun}…` : `Selecciona ${noun}`}
                error={!!error} helperText={error} slotProps={{ ...params.slotProps, inputLabel: { shrink: true }, htmlInput: { ...params.slotProps.htmlInput,
                    'aria-busy': events.pending, 'aria-describedby': [error ? `${params.id}-helper-text` : null,
                        selected.length ? selectedValueId : null].filter(Boolean).join(' ') || undefined,
                    'data-catalog-search': true, 'data-field': field, 'aria-label': `${label} ${index + 1}` } }} />}
            renderOption={(props, option, { selected: checked }) => <li {...props} key={option.id}>
                <Checkbox checked={checked} tabIndex={-1} disableRipple aria-hidden="true" sx={{ p: .5, mr: .5, pointerEvents: 'none' }} slotProps={{ input: { tabIndex: -1 } }} />
                <span>{option.descripcion}</span>
            </li>} />
        {state !== 'disponible' && <Typography variant="caption" role="status" color={state === 'error' ? 'error.main' : 'text.secondary'}>
            {state === 'cargando' ? 'Cargando catálogo local…' : state === 'error'
                ? `No fue posible consultar el catálogo local de ${catalogLabel}. Puedes continuar con el formulario.`
                : `El catálogo de ${catalogLabel} aún no está disponible`}
        </Typography>}
        {state === 'disponible' && selected.some(s => !options.some(o => o.id === s.id))
            && <Typography variant="caption">La selección incluye {noun} históric{noun === 'módulos' ? 'os' : 'as'} que ya no están disponibles para nuevas selecciones.</Typography>}
    </>;
}
