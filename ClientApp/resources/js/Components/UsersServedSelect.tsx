import { Box, Checkbox, ListItemText, MenuItem, Select, Typography } from '@mui/material';

export const usersServedOptions = ['Comunidad universitaria', 'Docentes', 'Estudiantes', 'Personal administrativo', 'Otro'] as const;

export default function UsersServedSelect({ value, label, readOnly, onChange, open, onOpen, onClose }: {
    value: string[] | string; label: string; readOnly: boolean; onChange: (value: string[]) => void;
    open: boolean; onOpen: () => void; onClose: () => void;
}) {
    // No convertir historia durante una lectura: los enviados pueden conservar el texto original.
    if (!Array.isArray(value)) return <Typography variant="body2" aria-label={label} sx={{ minWidth: 240, whiteSpace: 'pre-wrap' }}>{value || 'Sin selección'}</Typography>;
    return <Select multiple fullWidth displayEmpty value={value} readOnly={readOnly} open={open} onOpen={onOpen} onClose={onClose}
        sx={{ minWidth: 240, maxWidth: 300, '& .MuiSelect-select': { whiteSpace: 'normal', lineHeight: 1.5 } }}
        inputProps={{ 'aria-label': label, 'data-field': 'usuario' }}
        MenuProps={{ slotProps: { paper: { sx: { maxWidth: 'calc(100vw - 24px)', maxHeight: 320 } } } }}
        renderValue={selected => selected.length ? selected.join(', ') : <Box component="span" sx={{ color: 'text.secondary' }}>Selecciona los usuarios</Box>}
        onChange={event => {
            if (readOnly) return;
            const selected = event.target.value;
            if (Array.isArray(selected)) onChange(usersServedOptions.filter(option => selected.includes(option)));
        }}>
        {usersServedOptions.map(option => <MenuItem key={option} value={option} sx={{ whiteSpace: 'normal', minHeight: 44 }}>
            <Checkbox checked={value.includes(option)} tabIndex={-1} disableRipple
                slotProps={{ input: { 'aria-hidden': true } }} sx={{ pointerEvents: 'none' }} />
            <ListItemText primary={option} slotProps={{ primary: { sx: { fontSize: 14 } } }} />
        </MenuItem>)}
    </Select>;
}
