import { Box, MenuItem, Select, Typography } from '@mui/material';

const priorities = [
    ['5', 'Extremadamente prioritario', '#b71c1c'], ['4', 'Muy prioritario', '#a63c00'],
    ['3', 'Moderadamente prioritario', '#7a5700'], ['2', 'Poco prioritario', '#1256a0'], ['1', 'Nada prioritario', '#216731'],
];
export default function PrioritySelect({ value, label, readOnly, onChange, open, onOpen, onClose }: {
    value: string; label: string; readOnly: boolean; onChange: (value: string) => void;
    open: boolean; onOpen: () => void; onClose: () => void;
}) {
    const historical = !!value && !priorities.some(([number]) => number === value);
    return <Box sx={{ minWidth: 240 }}>
        <Select fullWidth displayEmpty value={value || ''} readOnly={readOnly} open={open} onOpen={onOpen} onClose={onClose}
            inputProps={{ 'aria-label': label, 'data-field': 'prioridad' }}
            renderValue={selected => {
                if (!selected) return <Box component="span" sx={{ color: 'text.secondary' }}>Selecciona una prioridad</Box>;
                const option = priorities.find(([number]) => number === selected);
                return option ? <Box component="span" sx={{ color: option[2], fontWeight: 500 }}>{option[0]}: {option[1]}</Box> : `Valor histórico: ${selected}`;
            }}
            onChange={event => onChange(event.target.value)}>
            {historical && <MenuItem value={value} disabled>Valor histórico: {value}</MenuItem>}
            {priorities.map(([number, text, color]) => <MenuItem key={number} value={number}>
                <Box component="span" sx={{ color, fontWeight: 500 }}>{number}: {text}</Box>
            </MenuItem>)}
        </Select>
        {historical && <Typography variant="caption">Se conserva el valor histórico. Elige una prioridad de 1 a 5 antes de enviar.</Typography>}
    </Box>;
}
