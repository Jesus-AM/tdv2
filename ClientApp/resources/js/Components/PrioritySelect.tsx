import { Box, MenuItem, Select, Typography } from '@mui/material';

export const priorities = [
    ['5', 'Muy alta', '#B91C1C'], ['4', 'Alta', '#C2410C'], ['3', 'Media', '#A16207'],
    ['2', 'Baja', '#1E4FA8'], ['1', 'Puede esperar', '#475569'],
] as const;
export function PriorityValue({ option }: { option: readonly [string, string, string] }) {
    return <Box component="span" sx={{ display: 'inline-flex', gap: 1, alignItems: 'center' }}>
        <Box component="span" sx={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
            bgcolor: option[2], color: '#fff', borderRadius: '50%', width: 26, height: 26, flexShrink: 0, fontWeight: 600 }}>{option[0]}</Box>
        <Box component="span" sx={{ fontWeight: 400 }}>{option[1]}</Box>
    </Box>;
}
export default function PrioritySelect({ value, label, readOnly, onChange, open, onOpen, onClose }: {
    value: string; label: string; readOnly: boolean; onChange: (value: string) => void;
    open: boolean; onOpen: () => void; onClose: () => void;
}) {
    const historical = !!value && !priorities.some(([number]) => number === value);
    return <Box sx={{ minWidth: 240 }}>
        <Select fullWidth displayEmpty value={value || ''} readOnly={readOnly} open={open} onOpen={onOpen} onClose={onClose}
            sx={{ '& .MuiSelect-select.MuiSelect-select': { whiteSpace: 'normal', textOverflow: 'clip' } }}
            inputProps={{ 'aria-label': label, 'data-field': 'prioridad' }}
            renderValue={selected => {
                if (!selected) return <Box component="span" sx={{ color: 'text.secondary' }}>Selecciona una prioridad</Box>;
                const option = priorities.find(([number]) => number === selected);
                return option ? <PriorityValue option={option} /> : `Valor histórico: ${selected}`;
            }}
            onChange={event => onChange(event.target.value)}>
            {historical && <MenuItem value={value} disabled>Valor histórico: {value}</MenuItem>}
            {priorities.map(option => <MenuItem key={option[0]} value={option[0]} aria-label={`${option[0]} · ${option[1]}`}>
                <PriorityValue option={option} />
            </MenuItem>)}
        </Select>
        {historical && <Typography variant="caption">Se conserva el valor histórico. Elige una prioridad de 1 a 5 antes de enviar.</Typography>}
    </Box>;
}
